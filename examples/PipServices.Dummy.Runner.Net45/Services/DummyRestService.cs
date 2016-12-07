using System;
using PipServices.Commons.Refer;
using PipServices.Dummy.Runner.Persistance;
using PipServices.Net.Rest;

namespace PipServices.Dummy.Runner.Services
{
    public sealed class DummyRestService : RestService<DummyWebApiController, IDummyRepository>, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "service", "rest", "default", "1.0");

        public DummyRestService()
        {
        }

        public DummyRestService(IDummyRepository logic)
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
                    references.GetOneRequired<IDummyRepository>(new Descriptor("pip-services-dummies", "repository", "*", "*", "*"));

            base.SetReferences(references);
        }
    }
}
