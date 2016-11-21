using System;
using System.Threading;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Net.Test.Rest;

namespace PipServices.Net.Test
{
    class Program
    {
        private static readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);

        private static readonly ConfigParams RestConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            "connection.host", "localhost",
            "connection.port", 3000
            );

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                _exitEvent.Set();

                Environment.Exit(1);
            };

            var ctrl = new DummyController();

            var service = new DummyRestService();
            service.Configure(RestConfig);

            var references = ReferenceSet.FromList(ctrl, service);

            service.SetReferences(references);

            var task = service.OpenAsync(null);
            task.Wait();

            // Wait and close
            _exitEvent.WaitOne();
        }
    }
}
