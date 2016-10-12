using PipServices.Data.Interfaces;
using PipServices.Net.Test.Models;

namespace PipServices.Net.Test.Persistance
{
    public interface IDummyRepository : IWriter<DummyObject, string>, IGetter<DummyObject, string>, IQuerableReader<DummyObject>
    {
    }
}
