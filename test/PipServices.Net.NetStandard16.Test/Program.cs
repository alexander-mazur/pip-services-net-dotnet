using PipServices.Commons.Config;
using PipServices.Net.Rest;

namespace PipServices.Net.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var service = new DummyRestService();
            service.Configure(new ConfigParams());
            var task = service.OpenAsync(null);
            task.Wait();
        }
    }
}
