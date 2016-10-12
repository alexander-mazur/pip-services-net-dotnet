using PipServices.Data.Memory;
using PipServices.Net.Test.Models;

namespace PipServices.Net.Test.Persistance
{
    public class DummyRepository : MemoryPersistence<DummyObject,string>, IDummyRepository
    {
    }
}
