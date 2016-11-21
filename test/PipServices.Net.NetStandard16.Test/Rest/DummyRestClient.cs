using PipServices.Net.Rest;
using PipServices.Commons.Refer;
using PipServices.Commons.Data;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Net.Test.Rest
{
    public sealed class DummyRestClient : RestClient, IDummyService, IDescriptable
    {
        public static Descriptor Descriptor { get; } = new Descriptor("pip-services-dummies", "client", "rest", "1.0");

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }

        private string PrepareQueryString(string path, FilterParams filter)
        {
            var param = filter.ToString();
            return path + (string.IsNullOrWhiteSpace(param) ? "" : "&" + param);
        }

        public Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging, CancellationToken token)
        {
            filter = filter ?? new FilterParams();
            paging = paging ?? new PagingParams();

            using (var timing = Instrument(correlationId, "dummy.get_page_by_filter"))
            {

                return ExecuteAsync<DataPage<Dummy>>(
                    correlationId,
                    HttpMethod.Get,
                    PrepareQueryString($"dummies?correlation_id={correlationId}", filter),
                    token
                    );
            }
        }

        public Task<Dummy> GetOneByIdAsync(string correlationId, string id, CancellationToken token)
        {
            using (var timing = Instrument(correlationId, "dummy.get_one_by_id"))
            {

                return ExecuteAsync<Dummy>(
                    correlationId,
                    HttpMethod.Get,
                    $"dummies/{id}?correlation_id={correlationId}",
                    token
                    );
            }
        }

        public Task<Dummy> CreateAsync(string correlationId, Dummy entity, CancellationToken token)
        {
            using (var timing = Instrument(correlationId, "dummy.create"))
            {
                return ExecuteAsync<Dummy>(
                    correlationId,
                    HttpMethod.Post,
                    $"dummies?correlation_id={correlationId}",
                    entity,
                    token
                    );
            }
        }

        public Task<Dummy> UpdateAsync(string correlationId, Dummy entity, CancellationToken token)
        {
            using (var timing = Instrument(correlationId, "dummy.update"))
            {
                return ExecuteAsync<Dummy>(
                    correlationId,
                    HttpMethod.Put,
                    $"dummies?correlation_id={correlationId}",
                    entity,
                    token
                    );
            }
        }

        public Task<Dummy> DeleteByIdAsync(string correlationId, string id, CancellationToken token)
        {
            using (var timing = Instrument(correlationId, "dummy.delete_by_id"))
            {
                return ExecuteAsync<Dummy>(
                    correlationId,
                    HttpMethod.Delete,
                    $"dummies/{id}?correlation_id={correlationId}",
                    token
                    );
            }
        }
    }
}
