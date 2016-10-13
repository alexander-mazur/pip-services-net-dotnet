using PipServices.Data.Interfaces;
using PipServices.Dummy.Runner.Models;

namespace PipServices.Dummy.Runner.Persistance
{
    public interface IDummyRepository : IWriter<DummyObject, string>, IGetter<DummyObject, string>, IQuerableReader<DummyObject>
    {
    }
}
