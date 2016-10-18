using System.Collections.Generic;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;

namespace PipServices.Net.Auth
{
    public sealed class CredentialResolver : IConfigurable, IReferenceable
    {
        private readonly IList<CredentialParams> _credentials = new List<CredentialParams>();
        private IReferences _references;

        public CredentialResolver() { }

        public CredentialResolver(ConfigParams config)
        {
            Configure(config);
        }

        public CredentialResolver(ConfigParams config, IReferences references)
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
            // Try to get multiple credentials first
            var credentials = config.GetSection("credentials");

            if (credentials.Count > 0)
            {
                var sectionsNames = credentials.GetSectionNames();

                foreach (var section in sectionsNames)
                {
                    var credential = credentials.GetSection(section);

                    _credentials.Add(new CredentialParams(credential));
                }
            }
            // Then try to get a single connection
            else
            {
                var credential = config.GetSection("credential");

                _credentials.Add(new CredentialParams(credential));
            }
        }

        public IEnumerable<CredentialParams> GetAll()
        {
            return _credentials;
        }

        public void Add(CredentialParams connection)
        {
            _credentials.Add(connection);
        }

        private CredentialParams LookupInStores(string correlationId, CredentialParams credential)
        {
            if (credential.UseCredentialStore() == false)
                return null;

            var key = credential.StoreKey;

            var components = _references.GetOptional(new Descriptor("*", "credential_store", "*", "*"));

            if (components.Count == 0)
                throw new ReferenceNotFoundException(correlationId, "Credential store wasn't found to make lookup");

            foreach (var component in components)
            {
                var store = component as ICredentialStore;

                var resolvedCredential = store?.Lookup(correlationId, key);

                if (resolvedCredential != null)
                    return resolvedCredential;
            }

            return null;
        }

        public CredentialParams Lookup(string correlationId)
        {
            if (_credentials.Count == 0) return null;

            // Return connection that doesn't require discovery
            foreach (var credential in _credentials)
            {
                if (!credential.UseCredentialStore())
                    return credential;
            }

            // Return connection that require discovery
            foreach (var credential in _credentials)
            {
                if (!credential.UseCredentialStore())
                    continue;

                var resolvedConnection = LookupInStores(correlationId, credential);

                if (resolvedConnection != null)
                    return resolvedConnection;
            }

            return null;
        }
    }
}
