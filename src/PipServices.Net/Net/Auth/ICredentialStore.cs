namespace PipServices.Net.Net.Auth
{
    public interface ICredentialStore
    {
        /**
             * Stores credential in the store
             * @param correlationId a unique transaction id to trace calls across components
             * @param key the key to lookup credential
             * @param credential a credential parameters
             */
        void Store(string correlationId, string key, CredentialParams credential);

        /**
         * Looks up credential from the store
         * @param correlationId a unique transaction id to trace calls across components
         * @param key a key to lookup credential
         * @return found credential parameters or <code>null</code> if nothing was found
         */
        CredentialParams Lookup(string correlationId, string key);
    }
}
