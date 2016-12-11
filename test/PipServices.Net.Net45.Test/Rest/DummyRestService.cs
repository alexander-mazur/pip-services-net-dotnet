using System;
using PipServices.Commons.Refer;

namespace PipServices.Net.Rest
{
    public sealed class DummyRestService : RestService<DummyWebApiController, IDummyController>, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "service", "rest", "default", "1.0");

        public DummyRestService()
        {
        }

        public DummyRestService(IDummyController logic)
        {
            if (logic == null)
                throw new ArgumentNullException(nameof(logic));

            _logic = logic;
        }

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }

        public override void SetReferences(IReferences references)
        {
            _logic =
                    references.GetOneRequired<IDummyController>(new Descriptor("pip-services-dummies", "controller", "*", "*", "*"));

            base.SetReferences(references);
        }
    }
}
