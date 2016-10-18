using PipServices.Net.Auth;
using PipServices.Commons.Config;
using Xunit;

namespace PipServices.Net.Test.Auth
{
    public sealed class CredentialParamsTest
    {
        [Fact]
        public void TestStoreKey()
        {
            var сredential = new CredentialParams();
            сredential.StoreKey = null;

            Assert.Null(сredential.StoreKey);

            сredential.StoreKey = "Store key";

            Assert.Equal(сredential.StoreKey, "Store key");

            Assert.True(сredential.UseCredentialStore);
        }

        [Fact]
        public void TestUsername()
        {
            var сredential = new CredentialParams();
            сredential.Username = null;

            Assert.Null(сredential.Username);

            сredential.Username = "Kate Negrienko";

            Assert.Equal(сredential.Username, "Kate Negrienko");
        }

        [Fact]
        public void TestPassword()
        {
            CredentialParams сredential = new CredentialParams();
            сredential.Password = null;

            Assert.Null(сredential.Password);

            сredential.Password = "qwerty";

            Assert.Equal(сredential.Password, "qwerty");
        }

        [Fact]
        public void TestAccessKey()
        {
            var сredential = new CredentialParams();
            сredential.AccessKey = null;

            Assert.Null(сredential.AccessKey);

            сredential.AccessKey = "key";

            Assert.Equal(сredential.AccessKey, "key");
        }
    }
}
