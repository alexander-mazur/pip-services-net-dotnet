using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using PipServices.Commons.Config;
using PipServices.Commons.Connect;
using PipServices.Commons.Count;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;

namespace PipServices.Net.Rest
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
                    .UseKestrel(options =>
                    {
                        // options.ThreadCount = 4;
                        options.NoDelay = true;
                        //options.UseHttps("testCert.pfx", "testPassword");
                        options.UseConnectionLogging();
                    })
                    //.UseWebListener()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseUrls(address)
                    //.UseIISIntegration()
                    //.UseConfiguration()
                    .UseStartup<TStartup>();

                Server = builder.Build();

                Logger.Info(correlationId, "Opened REST service at %s", Url);

                Server.Run(token);
            }
            catch (Exception ex)
            {
                Server.Dispose();

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
