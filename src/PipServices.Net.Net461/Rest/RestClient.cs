using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Commons.Count;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Commons.Connect;

namespace PipServices.Net.Rest
{
    public class RestClient : IOpenable, IClosable, IConfigurable, IReferenceable
    {
        private static readonly ConfigParams DefaultConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            //"connection.host", "localhost",
            //"connection.port", 3000,
            "connection.request_max_size", 1024*1024,
            "connection.connect_timeout", 60000,
            "connection.debug", true
            );

        private static readonly JsonSerializerSettings TransportSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        protected ConnectionResolver Resolver = new ConnectionResolver();
        protected ILogger Logger = new NullLogger();
        protected ICounters Counters = new NullCounters();
        protected string Url;
        protected HttpClient Client { get; set; }

        protected RestClient()
        {
        }

        public void SetReferences(IReferences references)
        {
            Resolver.SetReferences(references);

            var logger = (ILogger) references.GetOneOptional(new Descriptor("*", "logger", "*", "*"));
            Logger = logger ?? Logger;

            var counters = (ICounters) references.GetOneOptional(new Descriptor("*", "counters", "*", "*"));
            Counters = counters ?? Counters;
        }

        public void Configure(ConfigParams config)
        {
            config = config.SetDefaults(DefaultConfig);
            Resolver.Configure(config);
        }

        /**
	     * Does instrumentation of performed business method by counting elapsed time.
	     * @param correlationId the unique id to identify distributed transaction
	     * @param name the name of called business method
	     * @return ITiming instance to be called at the end of execution of the method.
	     */

        protected Timing Instrument(string correlationId, string name)
        {
            Logger.Trace(correlationId, "Calling %s method", name);
            return Counters.BeginTiming(name + ".call_time");
        }

        protected async Task<ConnectionParams> GetConnectionAsync(string correlationId, CancellationToken token)
        {
            var connection = await Resolver.ResolveAsync(correlationId, token);

            // Check for connection
            if (connection == null)
            {
                throw new ConfigException(
                    correlationId, "NO_CONNECTION", "Connection for REST client is not defined");
            }

            // Check for type
            var protocol = connection.GetProtocol("http");
            if (!"http".Equals(protocol))
            {
                throw new ConfigException(
                    correlationId, "WRONG_PROTOCOL", "Protocol is not supported by REST connection")
                    .WithDetails("protocol", protocol);
            }

            // Check for host
            if (connection.Host == null)
            {
                throw new ConfigException(
                    correlationId, "NO_HOST", "No host is configured in REST connection");
            }

            // Check for port
            if (connection.Port == 0)
            {
                throw new ConfigException(
                    correlationId, "NO_PORT", "No port is configured in REST connection");
            }

            return connection;
        }

        public async Task OpenAsync(string correlationId, CancellationToken token)
        {
            var connection = await GetConnectionAsync(correlationId, token);

            var protocol = connection.GetProtocol("http");
            var host = connection.Host;
            var port = connection.Port;

            // Check for type
            if (!"http".Equals(protocol.ToLower()))
                throw new ConfigException(correlationId, "SupportedProtocol", "Protocol type is not supported by REST transport")
                    .WithDetails(protocol, protocol);

            // Check for host
            if (string.IsNullOrWhiteSpace(host))
                throw new ConfigException(correlationId, "NoHost", "No host is configured in REST transport");

            // Check for port
            if (port == 0)
                throw new ConfigException(correlationId, "NoPort", "No port is configured in REST transport");

            Url = protocol + "://" + host + ":" + port;

//            if (!string.IsNullOrWhiteSpace(Route))
                //Url += "/" + Route;

            Client?.Dispose();

            Client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = true,
                UseCookies = true
            });

            Client.DefaultRequestHeaders.ConnectionClose = true;

            Logger.Debug(correlationId, "Connected via REST to %s", Url);
        }

        public Task CloseAsync(string correlationId, CancellationToken token)
        {
            Client?.Dispose();
            Client = null;

            Url = null;

            Logger.Debug(correlationId, "Disconnected from %s", Url);

            return Task.CompletedTask;
        }

        private static HttpContent CreateEntityContent(object value)
        {
            var content = JsonConvert.SerializeObject(value, TransportSettings);
            var result = new StringContent(content, Encoding.UTF8, "application/json");

            return result;
        }

        private Uri CreateRequestUri(string route)
        {
            var builder = new StringBuilder(Url);
            builder.Append("/");
            builder.Append(route);

            var uri = builder.ToString();

            var result = new Uri(uri, UriKind.Absolute);

            return result;
        }

        private async Task<HttpResponseMessage> ExecuteRequestAsync(string correlationId, HttpMethod method, Uri uri, CancellationToken token,
            HttpContent content = null)
        {
            if (Client == null)
                throw new InvalidOperationException("REST client is not configured");

            HttpResponseMessage result;

            try
            {
                if (method == HttpMethod.Get)
                    result = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
                else if (method == HttpMethod.Post)
                    result = await Client.PostAsync(uri, content, token);
                else if (method == HttpMethod.Put)
                    result = await Client.PutAsync(uri, content, token);
                else if (method == HttpMethod.Delete)
                    result = await Client.DeleteAsync(uri, token);
                else
                    throw new InvalidOperationException("Invalid request type");
            }
            catch (HttpRequestException ex)
            {
                throw new ConnectionException(correlationId, null, "Unknown communication problem on REST client", ex);
            }

            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NoContent:
                    break;
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.ServiceUnavailable:
                    {
                        var responseContent = await result.Content.ReadAsStringAsync();

                        ErrorDescription errorObject;
                        try
                        {
                            errorObject = JsonConvert.DeserializeObject<ErrorDescription>(responseContent);
                        }
                        catch (Exception ex)
                        {
                            errorObject = ErrorDescriptionFactory.Create(ex);
                        }

                        var appEx = ApplicationExceptionFactory.Create(errorObject);

                        throw appEx;
                    }
                default:
                    {
                        var responseContent = await result.Content.ReadAsStringAsync();

                        throw new BadRequestException(correlationId, null, responseContent);
                    }
            }

            return result;
        }

        protected async Task ExecuteAsync(string correlationId, HttpMethod method, string route, CancellationToken token)
        {
            var uri = CreateRequestUri(route);

            using (await ExecuteRequestAsync(correlationId, method, uri, token))
            {
            }
        }

        protected async Task ExecuteAsync(string correlationId, HttpMethod method, string route, object requestEntity, CancellationToken token)
        {
            var uri = CreateRequestUri(route);

            using (var requestContent = CreateEntityContent(requestEntity))
            {
                using (await ExecuteRequestAsync(correlationId, method, uri, token, requestContent))
                {
                }
            }
        }

        private static async Task<T> ExtractContentEntityAsync<T>(string correlationId, HttpContent content)
        {
            var value = await content.ReadAsStringAsync();

            try
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (JsonReaderException ex)
            {
                throw new BadRequestException(correlationId, null, "Unexpected protocol format", ex);
            }
        }

        protected async Task<T> ExecuteAsync<T>(string correlationId, HttpMethod method, string route, CancellationToken token)
            where T : class
        {
            var uri = CreateRequestUri(route);

            using (var response = await ExecuteRequestAsync(correlationId, method, uri, token))
            {
                return await ExtractContentEntityAsync<T>(correlationId, response.Content);
            }
        }

        private static async Task<string> ExtractContentEntityAsync(string correlationId, HttpContent content)
        {
            var value = await content.ReadAsStringAsync();

            try
            {
                value = JsonConvert.DeserializeObject<string>(value);
                return value;
            }
            catch (JsonReaderException ex)
            {
                throw new BadRequestException(correlationId, null, "Unexpected protocol format", ex);
            }
        }

        protected async Task<string> ExecuteStringAsync(string correlationId, HttpMethod method, string route, CancellationToken token)
        {
            var uri = CreateRequestUri(route);

            using (var response = await ExecuteRequestAsync(correlationId, method, uri, token))
            {
                return await ExtractContentEntityAsync(correlationId, response.Content);
            }
        }

        protected async Task<T> ExecuteAsync<T>(string correlationId, HttpMethod method, string route, object requestEntity,
            CancellationToken token)
            where T : class
        {
            var uri = CreateRequestUri(route);

            using (var requestContent = CreateEntityContent(requestEntity))
            {
                using (var response = await ExecuteRequestAsync(correlationId, method, uri, token, requestContent))
                {
                    return await ExtractContentEntityAsync<T>(correlationId, response.Content);
                }
            }
        }
    }
}
