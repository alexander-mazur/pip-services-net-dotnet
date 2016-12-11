using PipServices.Commons.Config;
using PipServices.Commons.Connect;
using PipServices.Commons.Count;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.SelfHost;

namespace PipServices.Net.Rest
{
    public abstract class RestService<TC, TL> : IOpenable, IClosable, IConfigurable, IReferenceable
        where TC : class, IHttpLogicController<TL>, new()
        where TL : class
    {
        private static readonly ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            "connection.host", "0.0.0.0",
            "connection.port", 3000,

            "options.request_max_size", 1024*1024,
            "options.connect_timeout", 60000,
            "options.debug", true
        );

        protected CompositeLogger _logger = new CompositeLogger();
        protected CompositeCounters _counters = new CompositeCounters();
        protected ConnectionResolver _connectionResolver = new ConnectionResolver();
        protected ConfigParams _options = new ConfigParams();

        protected TL _logic;
        protected HttpSelfHostServer _server;
        protected string _address;

        public virtual void SetReferences(IReferences references)
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

        protected Timing Instrument(string correlationId, string name)
        {
            _logger.Trace(correlationId, "Executing %s method", name);
            return _counters.BeginTiming(name + ".exec_time");
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
            if (string.IsNullOrWhiteSpace(connection.Host))
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

        public async Task OpenAsync(string correlationId)
        {
            var connection = await GetConnectionAsync(correlationId);

            var protocol = connection.GetProtocol("http");
            var host = connection.Host;
            var port = connection.Port;
            _address = protocol + "://" + host + ":" + port;

            var config = new HttpSelfHostConfiguration(_address);

            // Use routes
            config.MapHttpAttributeRoutes();

            // Override dependency resolver to inject this service as a controller
            config.DependencyResolver = new WebApiControllerResolver<TC, TL>(config.DependencyResolver, _logic);

            config.Services.Replace(typeof(IExceptionHandler), new RestExceptionHandler());

            try
            {
                _server = new HttpSelfHostServer(config);

                _logger.Info(correlationId, "Opened REST service at {0}", _address);

                await _server.OpenAsync();
            }
            catch (Exception ex)
            {
                _server.Dispose();

                _server = null;

                throw new ConnectionException(correlationId, "CANNOT_CONNECT", "Opening REST service failed")
                    .WithCause(ex).WithDetails("url", _address);
            }
        }

        public Task CloseAsync(string correlationId)
        {
            if (_server != null)
            {
                // Eat exceptions
                try
                {
                    _server.Dispose();
                    _logger.Info(correlationId, "Closed REST service at {0}", _address);
                }
                catch (Exception ex)
                {
                    _logger.Warn(correlationId, "Failed while closing REST service: {0}", ex);
                }

                _server = null;
                _address = null;
            }

            return Task.Delay(0);
        }
    }
}
