using PipServices.Commons.Refer;
using PipServices.Net.Test;

namespace PipServices.Net.Rest
{
    public sealed class DummyRestService : RestService<Startup>
    {
        private IDummyController _controller;

        public override void SetReferences(IReferences references)
        {
            _controller =
                references.GetOneRequired<IDummyController>(new Descriptor("pip-services-dummies", "controller", "*", "*", "*"));

            base.SetReferences(references);
        }
    }
}
