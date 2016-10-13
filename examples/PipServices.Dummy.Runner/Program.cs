using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Dummy.Runner.Services;

namespace PipServices.Dummy.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var service = new DummyRestService();
            service.Configure(new ConfigParams());
            var task = service.OpenAsync(null, CancellationToken.None);
            task.Wait();
        }
    }
}
