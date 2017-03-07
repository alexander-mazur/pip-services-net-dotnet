using PipServices.Commons.Config;
using PipServices.Commons.Connect;
using PipServices.Commons.Convert;
using PipServices.Commons.Count;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PipServices.Net.Rest
{
    public class RestClient : IOpenable, IConfigurable, IReferenceable
    {
        private static readonly ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            //"connection.host", "localhost",
            //"connection.port", 3000,

            "options.request_max_size", 1024*1024,
            "options.connect_timeout", 60000,
            "options.debug", true
        );

        //private static readonly JsonSerializerSettings TransportSettings = new JsonSerializerSettings
        //{
        //    DefaultValueHandling = DefaultValueHandling.Ignore
        //};

        protected ConnectionResolver _connectionResolver = new ConnectionResolver();
        protected CompositeLogger _logger = new CompositeLogger();
        protected CompositeCounters _counters = new CompositeCounters();
        protected ConfigParams _options = new ConfigParams();

        protected HttpClient _client { get; set; }
        protected string _address;

        public void SetReferences(IReferences references)
        {
            _connectionResolver.SetReferences(references);
            _logger.SetReferences(references);
            _counters.SetReferences(references);
        }

        public void Configure(ConfigParams config)
        {
            config = config.SetDefaults(_defaultConfig);
            _connectionResolver.Configure(config);
            _options = _options.Override(config.GetSection("options"));
        }

        protected Timing Instrument(string correlationId, [CallerMemberName]string methodName = null)
        {
            var typeName = GetType().Name;
            _logger.Trace(correlationId, "Calling {0} method of {1}", methodName, typeName);
            return _counters.BeginTiming(typeName + "." + methodName + ".call_time");
        }

        protected async Task<ConnectionParams> GetConnectionAsync(string correlationId)
        {
            var connection = await _connectionResolver.ResolveAsync(correlationId);

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

        public bool IsOpened()
        {
            return _client != null;
        }

        public async Task OpenAsync(string correlationId)
        {
            var connection = await GetConnectionAsync(correlationId);

            var protocol = connection.GetProtocol("http");
            var host = connection.Host;
            var port = connection.Port;

            _address = protocol + "://" + host + ":" + port;

//            if (!string.IsNullOrWhiteSpace(Route))
                //Url += "/" + Route;

            _client?.Dispose();

            _client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = true,
                UseCookies = true
            });

            _client.DefaultRequestHeaders.ConnectionClose = true;

            _logger.Debug(correlationId, "Connected via REST to {0}", _address);
        }

        public Task CloseAsync(string correlationId)
        {
            _client?.Dispose();
            _client = null;

            _address = null;

            _logger.Debug(correlationId, "Disconnected from {0}", _address);

            return Task.Delay(0);
        }

        private static HttpContent CreateEntityContent(object value)
        {
            var content = JsonConverter.ToJson(value);
            var result = new StringContent(content, Encoding.UTF8, "application/json");
            return result;
        }

        private Uri CreateRequestUri(string route)
        {
            var builder = new StringBuilder(_address);
            builder.Append("/");
            builder.Append(route);

            var uri = builder.ToString();

            var result = new Uri(uri, UriKind.Absolute);

            return result;
        }

        private async Task<HttpResponseMessage> ExecuteRequestAsync(
            string correlationId, HttpMethod method, Uri uri, HttpContent content = null)
        {
            if (_client == null)
                throw new InvalidOperationException("REST client is not configured");

            HttpResponseMessage result;

            try
            {
                if (method == HttpMethod.Get)
                    result = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                else if (method == HttpMethod.Post)
                    result = await _client.PostAsync(uri, content);
                else if (method == HttpMethod.Put)
                    result = await _client.PutAsync(uri, content);
                else if (method == HttpMethod.Delete)
                    result = await _client.DeleteAsync(uri);
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
                            errorObject = JsonConverter.FromJson<ErrorDescription>(responseContent);
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

        protected async Task ExecuteAsync(string correlationId, HttpMethod method, string route)
        {
            var uri = CreateRequestUri(route);

            using (await ExecuteRequestAsync(correlationId, method, uri))
            {
            }
        }

        protected async Task ExecuteAsync(
            string correlationId, HttpMethod method, string route, object requestEntity)
        {
            var uri = CreateRequestUri(route);

            using (var requestContent = CreateEntityContent(requestEntity))
            {
                using (await ExecuteRequestAsync(correlationId, method, uri, requestContent))
                {
                }
            }
        }

        private static async Task<T> ExtractContentEntityAsync<T>(string correlationId, HttpContent content)
        {
            var value = await content.ReadAsStringAsync();

            try
            {
                return JsonConverter.FromJson<T>(value);
            }
            catch (Exception ex)
            {
                throw new BadRequestException(correlationId, null, "Unexpected protocol format", ex);
            }
        }

        protected async Task<T> ExecuteAsync<T>(string correlationId, HttpMethod method, string route)
            where T : class
        {
            var uri = CreateRequestUri(route);

            using (var response = await ExecuteRequestAsync(correlationId, method, uri))
            {
                return await ExtractContentEntityAsync<T>(correlationId, response.Content);
            }
        }

        private static async Task<string> ExtractContentEntityAsync(string correlationId, HttpContent content)
        {
            var value = await content.ReadAsStringAsync();

            try
            {
                value = JsonConverter.FromJson<string>(value);
                return value;
            }
            catch (Exception ex)
            {
                throw new BadRequestException(correlationId, null, "Unexpected protocol format", ex);
            }
        }

        protected async Task<string> ExecuteStringAsync(string correlationId, HttpMethod method, string route)
        {
            var uri = CreateRequestUri(route);

            using (var response = await ExecuteRequestAsync(correlationId, method, uri))
            {
                return await ExtractContentEntityAsync(correlationId, response.Content);
            }
        }

        protected async Task<T> ExecuteAsync<T>(
            string correlationId, HttpMethod method, string route, object requestEntity)
            where T : class
        {
            var uri = CreateRequestUri(route);

            using (var requestContent = CreateEntityContent(requestEntity))
            {
                using (var response = await ExecuteRequestAsync(correlationId, method, uri, requestContent))
                {
                    return await ExtractContentEntityAsync<T>(correlationId, response.Content);
                }
            }
        }
    }
}
