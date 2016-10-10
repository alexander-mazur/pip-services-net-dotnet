using System;
using PipServices.Commons.Config;
using PipServices.Commons.Count;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Net.Net.Connect;

namespace PipServices.Net.Net.Rest
{
    public abstract class RestService : IOpenable, IClosable, IConfigurable, IReferenceable
    {
        private static readonly ConfigParams DefaultConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            "connection.host", "0.0.0.0",
            //"connection.port", 3000,
            "connection.request_max_size", 1024*1024,
            "connection.connect_timeout", 60000,
            "connection.debug", true
            );

        protected HttpSelfHostServer _server;
        protected ConnectionResolver Resolver = new ConnectionResolver();
        protected ILogger Logger = new NullLogger();
        protected ICounters Counters = new NullCounters();
        protected string Url;

        protected RestService()
        {
        }

        public void SetReferences(IReferences references)
        {
            Resolver.SetReferences(references);

            var logger = references.GetOneOptional(new Descriptor("*", "logger", "*", "*")) as ILogger;
            Logger = logger ?? Logger;

            var counters = references.GetOneOptional(new Descriptor("*", "counters", "*", "*")) as ICounters;
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
            Logger.Trace(correlationId, "Executing %s method", name);
            return Counters.BeginTiming(name + ".exec_time");
        }

        protected ConnectionParams GetConnection(string correlationId)
        {
            var connection = Resolver.ResolveAsync(correlationId);

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

        public void Open(string correlationId)
        {
            var connection = GetConnection(correlationId);

            var protocol = connection.GetProtocol("http");
            var host = connection.Host;
            var port = connection.Port;
            var address = protocol + "://" + host + ":" + port;

            var config = new HttpSelfHostConfiguration(address);

            // Use routes
            config.MapHttpAttributeRoutes();

            // Override dependency resolver to inject this service as a controller
            config.DependencyResolver = new WebApiControllerResolver<T, I>(config.DependencyResolver, Logic);

            config.Services.Replace(typeof(IExceptionHandler), new MicroserviceExceptionHandler());

            try
            {
                _host = new HttpSelfHostServer(config);
                _host.OpenAsync().Wait();
            // RegisterAsync the service URI
            Resolver.RegisterAsync(correlationId, connection);

                Logger.Info(correlationId, "Opened REST service at %s", Url);
            }
            catch (Exception ex)
            {
                _server = null;
                throw new ConnectionException(correlationId, "CANNOT_CONNECT", "Opening REST service failed")
                    .WithCause(ex.Message).WithDetails("url", Url);
            }
        }

        public void Close(string correlationId)
        {
            if (_server != null)
            {
                // Eat exceptions
                try
                {
                    _server.stop(0);
                    Logger.Info(correlationId, "Closed REST service at %s", Url);
                }
                catch (Exception ex)
                {
                    Logger.Warn(correlationId, "Failed while closing REST service: %s", ex);
                }

                _server = null;
                Url = null;
            }
        }
    }
}
