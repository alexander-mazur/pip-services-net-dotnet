using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using PipServices.Commons.Config;
using PipServices.Commons.Count;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Net.Net.Connect;
using AuthenticationSchemes = Microsoft.Net.Http.Server.AuthenticationSchemes;

namespace PipServices.Net.Net.Rest
{
    public abstract class RestService<TStartup> : IOpenable, IClosable, IConfigurable, IReferenceable
        where TStartup : class
    {
        private static readonly ConfigParams DefaultConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            "connection.host", "0.0.0.0",
            "connection.port", 3000,
            "connection.request_max_size", 1024*1024,
            "connection.connect_timeout", 60000,
            "connection.debug", true
            );

        protected IWebHost Server { get; private set; }
        protected ConnectionResolver Resolver = new ConnectionResolver();
        protected ILogger Logger = new NullLogger();
        protected ICounters Counters = new NullCounters();
        protected string Url;

        public virtual void SetReferences(IReferences references)
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

        public async Task OpenAsync(string correlationId, CancellationToken token)
        {
            var connection = await GetConnectionAsync(correlationId, token);

            var protocol = connection.GetProtocol("http");
            var host = connection.Host;
            var port = connection.Port;
            var address = protocol + "://" + host + ":" + port;

            try
            {
                //.UseWebListener(options =>
                //{
                //    // AllowAnonymous is the default WebListner configuration
                //    options.Listener.AuthenticationManager.AuthenticationSchemes =
                //        AuthenticationSchemes.AllowAnonymous;
                //})
                var builder = new WebHostBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseUrls(address)
                    .UseIISIntegration()
                    //.UseConfiguration()
                    .UseStartup<TStartup>();

                // The default listening address is http://localhost:5000 if none is specified.
                // Replace "localhost" with "*" to listen to external requests.
                // You can use the --urls flag to change the listening address. Ex:
                // > dotnet run --urls http://*:8080;http://*:8081

                // Uncomment the following to configure URLs programmatically.
                // Since this is after UseConfiguraiton(config), this will clobber command line configuration.
                //builder.UseUrls("http://*:8080", "http://*:8081");

                // If this app isn't hosted by IIS, UseIISIntegration() no-ops.
                // It isn't possible to both listen to requests directly and from IIS using the same WebHost,
                // since this will clobber your UseUrls() configuration when hosted by IIS.
                // If UseIISIntegration() is called before UseUrls(), IIS hosting will fail.
                //builder.UseIISIntegration();

                Server = builder.Build();
                Server.Run(token);

                Logger.Info(correlationId, "Opened REST service at %s", Url);
            }
            catch (Exception ex)
            {
                Server = null;
                throw new ConnectionException(correlationId, "CANNOT_CONNECT", "Opening REST service failed")
                    .WithCause(ex).WithDetails("url", Url);
            }
        }

        public Task CloseAsync(string correlationId, CancellationToken token)
        {
            if (Server != null)
            {
                // Eat exceptions
                try
                {
                    Server.Dispose();
                    Logger.Info(correlationId, "Closed REST service at %s", Url);
                }
                catch (Exception ex)
                {
                    Logger.Warn(correlationId, "Failed while closing REST service: %s", ex);
                }

                Server = null;
                Url = null;
            }

            return Task.CompletedTask;
        }
    }
}
