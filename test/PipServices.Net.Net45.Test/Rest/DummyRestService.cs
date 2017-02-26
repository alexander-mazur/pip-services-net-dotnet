using System;
using PipServices.Commons.Refer;

namespace PipServices.Net.Rest
{
    public sealed class DummyRestService : RestService<DummyWebApiController, IDummyController>
    {
        public DummyRestService()
        {
        }

        public DummyRestService(IDummyController logic)
        {
            if (logic == null)
                throw new ArgumentNullException(nameof(logic));

            _logic = logic;
        }

        public override void SetReferences(IReferences references)
        {
            _logic =
                    references.GetOneRequired<IDummyController>(new Descriptor("pip-services-dummies", "controller", "*", "*", "*"));

            base.SetReferences(references);
        }
    }
}
