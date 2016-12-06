using System;
using System.Diagnostics;
using PipServices.Commons.Config;
using PipServices.Dummy.Runner.Services;

namespace PipServices.Dummy.Runner
{
    public class Program
    {
        private static DummyRestService _service;

        public static void Main(string[] args)
        {
            _service = new DummyRestService();

            Process.GetCurrentProcess().Exited += OnExited;

            _service.Configure(new ConfigParams());

            var task = _service.OpenAsync(null);
            task.Wait();
        }

        private static void OnExited(object sender, EventArgs e)
        {
            var task = _service.CloseAsync(null);
            task.Wait();
        }
    }
}
