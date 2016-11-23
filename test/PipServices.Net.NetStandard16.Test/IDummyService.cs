using System.Threading;
using PipServices.Commons.Data;
using System.Threading.Tasks;

namespace PipServices.Net
{
    public interface IDummyService
    {
        Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging, CancellationToken token);
        Task<Dummy> GetOneByIdAsync(string correlationId, string id, CancellationToken token);
        Task<Dummy> CreateAsync(string correlationId, Dummy entity, CancellationToken token);
        Task<Dummy> UpdateAsync(string correlationId, Dummy entity, CancellationToken token);
        Task<Dummy> DeleteByIdAsync(string correlationId, string id, CancellationToken token);
    }
}
