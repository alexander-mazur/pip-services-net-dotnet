using System;
using PipServices.Net.Auth;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Commons.Errors;
using Xunit;
using System.Linq;

namespace PipServices.Net.Test.Auth
{
    public sealed class CredentialResolverTest
    {
        private static readonly ConfigParams RestConfig = ConfigParams.FromTuples(
            "credential.username", "Negrienko",
            "credential.password", "qwerty",
            "credential.access_key", "key",
            "credential.store_key", "store key"
            );

        [Fact]
        public void TestConfigure()
        {
            var credentialResolver = new CredentialResolver(RestConfig);
            var config = credentialResolver.GetAll().FirstOrDefault();

            Assert.Equal(config["username"], "Negrienko");
            Assert.Equal(config["password"], "qwerty");
            Assert.Equal(config["access_key"], "key");
            Assert.Equal(config["store_key"], "store key");
        }

        [Fact]
        public void TestLookup()
        {
            var credentialResolver = new CredentialResolver();
            var credential = credentialResolver.Lookup("correlationId");

            Assert.Null(credential);

            var restConfigWithoutStoreKey = ConfigParams.FromTuples(
                "credential.username", "Negrienko",
                "credential.password", "qwerty",
                "credential.access_key", "key"
                );
            credentialResolver = new CredentialResolver(restConfigWithoutStoreKey);
            credential = credentialResolver.Lookup("correlationId");

            Assert.Equal(credential.Get("username"), "Negrienko");

            Assert.Equal(credential.Get("password"), "qwerty");

            Assert.Equal(credential.Get("access_key"), "key");

            Assert.Null(credential.Get("store_key"));

            credentialResolver = new CredentialResolver(RestConfig);
            credentialResolver.SetReferences(new ReferenceSet());

            try
            {
                credential = credentialResolver.Lookup("correlationId");
            }
            catch (Exception ex)
            {
                Assert.IsType<ReferenceNotFoundException>(ex);
            }
        }
    }
}
