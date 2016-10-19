using PipServices.Net.Messaging;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Commons.Errors;
using Xunit;

namespace PipServices.Net.Test.Rest
{
    public sealed class DummyClientFixture
    {
        private readonly Dummy _dummy1 = new Dummy(null, "Key 1", "Content 1");
        private readonly Dummy _dummy2 = new Dummy(null, "Key 2", "Content 2");

        private readonly IDummyService _client;

        public DummyClientFixture(IDummyService client)
        {
            Assert.NotNull(client);
            _client = client;
        }

        public void TestCrudOperations()
        {
            // Create one dummy
            var dummy1 = _client.Create(null, _dummy1);

            Assert.NotNull(dummy1);
            Assert.NotNull(dummy1.Id);
            Assert.Equal(_dummy1.Key, dummy1.Key);
            Assert.Equal(_dummy1.Content, dummy1.Content);

            // Create another dummy
            var dummy2 = _client.Create(null, _dummy2);

            Assert.NotNull(dummy2);
            Assert.NotNull(dummy2.Id);
            Assert.Equal(_dummy2.Key, dummy2.Key);
            Assert.Equal(_dummy2.Content, dummy2.Content);

            // Get all dummies
            var dummies = _client.GetPageByFilter(null, null, null);
            Assert.NotNull(dummies);
            Assert.Equal(2, dummies.Data.Count);

            // Update the dummy
            dummy1.Content = "Updated Content 1";
            var dummy = _client.Update(null, dummy1);

            Assert.NotNull(dummy);
            Assert.Equal(dummy1.Id, dummy.Id);
            Assert.Equal(dummy1.Key, dummy.Key);
            Assert.Equal("Updated Content 1", dummy.Content);

            // Delete the dummy
            _client.DeleteById(null, dummy1.Id);

            // Try to get deleted dummy
            dummy = _client.GetOneById(null, dummy1.Id);
            Assert.Null(dummy);
        }
    }
}
