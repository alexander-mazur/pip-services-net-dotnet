using System;
using System.Collections.Generic;
using PipServices.Commons.Config;
using PipServices.Commons.Errors;
using PipServices.Commons.Refer;

namespace PipServices.Net.Net.Connect
{
    public sealed class ConnectionResolver
    {
        private readonly IList<ConnectionParams> _connections = new List<ConnectionParams>();
        private IReferences _references;

        public ConnectionResolver()
        {
        }

        public ConnectionResolver(ConfigParams config)
        {
            Configure(config);
        }

        public ConnectionResolver(ConfigParams config, IReferences references)
        {
            Configure(config);
            SetReferences(references);
        }

        public void SetReferences(IReferences references)
        {
            _references = references;
        }

        public void Configure(ConfigParams config)
        {
            // Try to get multiple connections first
            var connections = config.GetSection("connections");

            if (connections.Count > 0)
            {
                var connectionSections = connections.GetSectionNames();

                foreach (var section in connectionSections)
                {
                    var connection = connections.GetSection(section);

                    _connections.Add(new ConnectionParams(connection));
                }
            }
            // Then try to get a single connection
            else
            {
                var connection = config.GetSection("connection");

                _connections.Add(new ConnectionParams(connection));
            }
        }

        public IEnumerable<ConnectionParams> GetAll()
        {
            return _connections;
        }

        public void Add(ConnectionParams connection)
        {
            _connections.Add(connection);
        }

        private void RegisterInDiscovery(string correlationId, ConnectionParams connection)
        {

            if (connection.UseDiscovery == false) return;

            var key = connection.DiscoveryKey;

            var components = _references.GetOptional(new Descriptor("*", "discovery", "*", "*"));

            foreach (var component in components)
            {
                var discovery = component as IDiscovery;

                discovery?.Register(correlationId, key, connection);
            }
        }

        public void Register(string correlationId, ConnectionParams connection)
        {
            RegisterInDiscovery(correlationId, connection);

            _connections.Add(connection);
        }

        private ConnectionParams ResolveInDiscovery(string correlationId, ConnectionParams connection)
        {

            if (connection.UseDiscovery == false)
                return null;

            var key = connection.DiscoveryKey;

            var components = _references.GetOptional(new Descriptor("*", "discovery", "*", "*"));
            if (components.Count == 0)
                throw new ConfigException(correlationId, "CANNOT_RESOLVE", "Discovery wasn't found to make resolution");

            foreach (var component in components)
            {
                var discovery = component as IDiscovery;

                var resolvedConnection = discovery?.ResolveOne(correlationId, key);

                if (resolvedConnection != null)
                    return resolvedConnection;
            }

            return null;
        }

        public ConnectionParams Resolve(string correlationId)
        {
            if (_connections.Count == 0)
                return null;

            // Return connection that doesn't require discovery
            foreach (var connection in _connections)
            {
                if (!connection.UseDiscovery)
                    return connection;
            }

            // Return connection that require discovery
            foreach (var connection in _connections)
            {
                if (!connection.UseDiscovery)
                    continue;

                var resolvedConnection = ResolveInDiscovery(correlationId, connection);

                if (resolvedConnection == null)
                    continue;

                // Merge configured and new parameters
                resolvedConnection =
                    new ConnectionParams(ConfigParams.MergeConfigs(connection, resolvedConnection));

                return resolvedConnection;
            }

            return null;
        }

        private IEnumerable<ConnectionParams> ResolveAllInDiscovery(string correlationId, ConnectionParams connection)
        {
            var result = new List<ConnectionParams>();

            if (connection.UseDiscovery == false)
                return result;

            var key = connection.DiscoveryKey;

            var components = _references.GetOptional(new Descriptor("*", "discovery", "*", "*"));

            if (components.Count == 0)
                throw new ConfigException(correlationId, "CANNOT_RESOLVE", "Discovery wasn't found to make resolution");

            foreach (var component in components)
            {
                var discovery = component as IDiscovery;

                var resolvedConnections = discovery?.ResolveAll(correlationId, key);

                if (resolvedConnections != null)
                    result.AddRange(resolvedConnections);
            }

            return result;
        }

        public IEnumerable<ConnectionParams> ResolveAllstring(string correlationId)
        {
            var resolved = new List<ConnectionParams>();
            var toResolve = new List<ConnectionParams>();

            // Sort connections
            foreach (var connection in _connections)
            {
                if (connection.UseDiscovery)
                    toResolve.Add(connection);
                else
                    resolved.Add(connection);
            }

            // Resolve addresses that require that
            if (toResolve.Count <= 0)
                return resolved;

            foreach (var connection in toResolve)
            {
                var resolvedConnections = ResolveAllInDiscovery(correlationId, connection);

                foreach (var resolvedConnection in resolvedConnections)
                {
                    // Merge configured and new parameters
                    var localResolvedConnection = new ConnectionParams(ConfigParams.MergeConfigs(connection, resolvedConnection));
                    resolved.Add(localResolvedConnection);
                }
            }

            return resolved;
        }
    }
}
