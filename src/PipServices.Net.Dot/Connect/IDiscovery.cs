using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Net.Connect
{
    public interface IDiscovery
    {
        /**
         * Registers connection where API service binds to.
         * @param correlationId a unique transaction id to trace calls across components
         * @param key a key to identify the connection
         * @param connection the connection to be registered.
         * @throws ApplicationException when registration fails for whatever reasons
         */
        Task RegisterAsync(string correlationId, string key, ConnectionParams connection, CancellationToken token);

        /**
         * Resolves one connection from the list of service connections.
         * @param correlationId a unique transaction id to trace calls across components
         * @param key a key locate a connection
         * @return a resolved connection.
         * @throws ApplicationException when resolution failed for whatever reasons.
         */
        Task<ConnectionParams> ResolveOneAsync(string correlationId, string key, CancellationToken token);

        /**
         * Resolves a list of connections from to be called by a client.
         * @param correlationId a unique transaction id to trace calls across components
         * @param key a key locate connections
         * @return a list with resolved connections.
         * @throws ApplicationException when resolution failed for whatever reasons.
         */
        Task<IList<ConnectionParams>> ResolveAllAsync(string correlationId, string key, CancellationToken token);
    }
}
