using System.Threading;
using PipServices.Net.Test.Rest;
using PipServices.Commons.Config;
using PipServices.Commons.Data;

namespace PipServices.Net.Test
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
