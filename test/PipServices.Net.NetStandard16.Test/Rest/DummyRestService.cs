using PipServices.Commons.Refer;
using PipServices.Net.Test;

namespace PipServices.Net.Rest
{
    public sealed class DummyRestService : RestService<Startup>, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "service", "rest", "default", "1.0");

        private IDummyController _controller;

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }

        public override void SetReferences(IReferences references)
        {
            _controller =
                references.GetOneRequired<IDummyController>(new Descriptor("pip-services-dummies", "controller", "*",
                    "*", "*"));

            base.SetReferences(references);
        }
    }
}
