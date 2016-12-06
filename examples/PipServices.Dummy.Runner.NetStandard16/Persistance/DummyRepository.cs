using PipServices.Data.Memory;
using PipServices.Dummy.Runner.Models;

namespace PipServices.Dummy.Runner.Persistance
{
    public class DummyRepository : MemoryPersistence<DummyObject,string>, IDummyRepository
    {
    }
}
