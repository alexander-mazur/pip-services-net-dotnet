using PipServices.Commons.Refer;
using PipServices.Data.Memory;
using PipServices.Dummy.Runner.Models;

namespace PipServices.Dummy.Runner.Persistance
{
    public class DummyRepository : MemoryPersistence<DummyObject,string>, IDummyRepository, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "repository", "default", "default", "1.0");

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }
    }
}
