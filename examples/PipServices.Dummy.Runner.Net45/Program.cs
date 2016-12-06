using System;
using System.Threading;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Dummy.Runner.Persistance;
using PipServices.Dummy.Runner.Services;

namespace PipServices.Dummy.Runner
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

            var ctrl = new DummyRepository();

            var service = new DummyRestService();
            service.Configure(RestConfig);

            var references = References.FromList(ctrl, service);

            service.SetReferences(references);

            var task = service.OpenAsync(null);
            task.Wait();

            // Wait and close
            _exitEvent.WaitOne();
        }
    }
}
