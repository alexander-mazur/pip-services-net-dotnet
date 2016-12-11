using Microsoft.AspNetCore.Hosting;
using PipServices.Commons.Config;
using PipServices.Commons.Connect;
using PipServices.Commons.Count;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PipServices.Net.Rest
{
    public abstract class RestService<TStartup> : IOpenable, IClosable, IConfigurable, IReferenceable
        where TStartup : class
    {
        private static readonly ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            "connection.host", "0.0.0.0",
            "connection.port", 3000,

            "options.request_max_size", 1024*1024,
            "options.connect_timeout", 60000,
            "options.debug", true
        );

        protected ConnectionResolver _connectionResolver = new ConnectionResolver();
        protected CompositeLogger _logger = new CompositeLogger();
        protected CompositeCounters _counters = new CompositeCounters();
        protected ConfigParams _options = new ConfigParams();

        protected IWebHost _server;
        protected string _address;

        public virtual void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _counters.SetReferences(references);
            _connectionResolver.SetReferences(references);
        }

        public void Configure(ConfigParams config)
        {
            config = config.SetDefaults(_defaultConfig);
            _connectionResolver.Configure(config);
            _options = _options.Override(config.GetSection("options"));
        }

        protected Timing Instrument(string correlationId, string name)
        {
            _logger.Trace(correlationId, "Executing {0} method", name);
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

            try
            {
                var builder = new WebHostBuilder()
                    .UseKestrel(options =>
                    {
                        // options.ThreadCount = 4;
                        options.NoDelay = true;
                        //options.UseHttps("testCert.pfx", "testPassword");
                        options.UseConnectionLogging();
                    })
                    //.UseWebListener()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseUrls(_address)
                    //.UseIISIntegration()
                    //.UseConfiguration()
                    .UseStartup<TStartup>();

                _server = builder.Build();

                _logger.Info(correlationId, "Opened REST service at {0}", _address);

                _server.Run();
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
