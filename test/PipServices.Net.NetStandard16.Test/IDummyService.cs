using System.Threading;
using PipServices.Commons.Data;
using System.Threading.Tasks;

namespace PipServices.Net
{
    public interface IDummyService
    {
        Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging);
        Task<Dummy> GetOneByIdAsync(string correlationId, string id);
        Task<Dummy> CreateAsync(string correlationId, Dummy entity);
        Task<Dummy> UpdateAsync(string correlationId, Dummy entity);
        Task<Dummy> DeleteByIdAsync(string correlationId, string id);
    }
}
