using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Refer;
using PipServices.Net.Rest;
using Xunit;

namespace PipServices.Net.Direct
{
    public class DummyDirectClientTest
    {
        private readonly DummyController _ctrl;
        private readonly DummyDirectClient _client;
        private readonly DummyClientFixture _fixture;

        public DummyDirectClientTest()
        {
            _ctrl = new DummyController();
            _client = new DummyDirectClient();

            var references = References.FromTuples(
                new Descriptor("pip-services-dummies", "controller", "default", "default", "1.0"), _ctrl
            );
            _client.SetReferences(references);

            _fixture = new DummyClientFixture(_client);

            var clientTask = _client.OpenAsync(null);
            clientTask.Wait();
        }

        [Fact]
        public void TestCrudOperations()
        {
            var task = _fixture.TestCrudOperations();
            task.Wait();
        }

        public void Dispose()
        {
            var task = _client.CloseAsync(null);
            task.Wait();
        }
    }
}
