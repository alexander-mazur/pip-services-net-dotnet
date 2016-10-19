using System.Threading;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using Xunit;

namespace PipServices.Net.Test.Rest
{
    public sealed class DummyRestClientTest
    {
        private static readonly ConfigParams RestConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            "connection.host", "localhost",
            "connection.port", 3000
            );

        private readonly DummyController _ctrl;
        private readonly DummyRestService _service;
        private readonly DummyRestClient _client;
        private readonly ReferenceSet _references;
        private readonly DummyClientFixture _fixture;

        public DummyRestClientTest()
        {
            _ctrl = new DummyController();

            _service = new DummyRestService();
            _service.Configure(RestConfig);

            _client = new DummyRestClient();
            _client.Configure(RestConfig);

            _references = ReferenceSet.From(_ctrl, _client, _service);
            _client.SetReferences(_references);
            _service.SetReferences(_references);

            _fixture = new DummyClientFixture(_client);

            var serviceTask = _service.OpenAsync(null, CancellationToken.None);
            serviceTask.Wait();
            var clientTask = _client.OpenAsync(null, CancellationToken.None);
            clientTask.Wait();
        }

        [Fact]
        public void TestCrudOperations()
        {
            _fixture.TestCrudOperations();
        }
    }
}
