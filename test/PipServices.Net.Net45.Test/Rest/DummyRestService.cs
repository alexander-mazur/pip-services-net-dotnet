using System;
using PipServices.Commons.Refer;
using PipServices.Net.Test;

namespace PipServices.Net.Rest
{
    public sealed class DummyRestService : RestService<DummyWebApiController, IDummyController>, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "service", "rest", "1.0");

        public DummyRestService()
        {
        }

        public DummyRestService(IDummyController logic)
        {
            if (logic == null)
                throw new ArgumentNullException(nameof(logic));

            Logic = logic;
        }

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }

        public override void SetReferences(IReferences references)
        {
            Logic =
                (IDummyController)
                    references.GetOneBefore(this, new Descriptor("pip-services-dummies", "controller", "*", "*"));

            base.SetReferences(references);
        }
    }
}
