using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Count;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Net.Net.Connect;

namespace PipServices.Net.Net.Rest
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

        protected ConnectionResolver Resolver = new ConnectionResolver();
        protected ILogger Logger = new NullLogger();
        protected ICounters Counters = new NullCounters();
        protected string Route;
        protected string Url;
        protected HttpClient Client { get; set; }

        protected RestClient()
            : this(null)
        {
        }

        protected RestClient(string route)
        {
            Route = route;
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

        protected ConnectionParams GetConnection(string correlationId)
        {
            var connection = Resolver.Resolve(correlationId);

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

        public Task OpenAsync(string correlationId, CancellationToken token)
        {
            var connection = GetConnection(correlationId);

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

            if (!string.IsNullOrWhiteSpace(Route))
                Url += "/" + Route;

            Client?.Dispose();

            Client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = true,
                UseCookies = true
            });

            Client.DefaultRequestHeaders.ConnectionClose = true;

            Logger.Debug(correlationId, "Connected via REST to %s", Url);

            return Task.CompletedTask;
        }

        public Task CloseAsync(string correlationId, CancellationToken token)
        {
            Client?.Dispose();
            Client = null;

            Url = null;

            Logger.Debug(correlationId, "Disconnected from %s", Url);

            return Task.CompletedTask;
        }
    }
}
