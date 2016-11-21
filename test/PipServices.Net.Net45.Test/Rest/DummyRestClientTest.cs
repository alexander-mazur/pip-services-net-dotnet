using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using System;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly DummyClientFixture _fixture;
        private readonly CancellationTokenSource _source;
        private readonly ReferenceSet _references;

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

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await _service.OpenAsync(null, CancellationToken.None));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            _source = new CancellationTokenSource();

            var clientTask = _client.OpenAsync(null, CancellationToken.None);
            clientTask.Wait();
        }

        [Fact]
        public void TestCrudOperations()
        {
            var task = _fixture.TestCrudOperations();
            task.Wait();

            task = _client.CloseAsync(null, CancellationToken.None);
            task.Wait();

            task = _service.CloseAsync(null, CancellationToken.None);
            task.Wait();
        }
    }
}
