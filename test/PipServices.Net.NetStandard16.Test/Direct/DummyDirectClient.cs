using System.Threading.Tasks;
using PipServices.Commons.Data;
using PipServices.Commons.Refer;

namespace PipServices.Net.Direct
{
    public class DummyDirectClient : DirectClient, IDummyClient
    {
        private IDummyController _logic;

        public override void SetReferences(IReferences references)
        {
            base.SetReferences(references);

            _logic = references.GetOneRequired<IDummyController>(new Descriptor("pip-services-dummies", "controller", "*", "*", "*"));
        }

        public override bool IsOpened()
        {
            return _logic != null;
        }

        public Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging)
        {
            filter = filter ?? new FilterParams();
            paging = paging ?? new PagingParams();

            using (var timing = Instrument(correlationId, "dummy.get_page_by_filter"))
            {
                return Task.FromResult(_logic.GetPageByFilter(correlationId, filter, paging));
            }
        }

        public Task<Dummy> GetOneByIdAsync(string correlationId, string id)
        {
            using (var timing = Instrument(correlationId, "dummy.get_one_by_id"))
            {
                return Task.FromResult(_logic.GetOneById(correlationId, id));
            }
        }

        public Task<Dummy> CreateAsync(string correlationId, Dummy entity)
        {
            using (var timing = Instrument(correlationId, "dummy.create"))
            {
                return Task.FromResult(_logic.Create(correlationId, entity));
            }
        }

        public Task<Dummy> UpdateAsync(string correlationId, Dummy entity)
        {
            using (var timing = Instrument(correlationId, "dummy.update"))
            {
                return Task.FromResult(_logic.Update(correlationId, entity));
            }
        }

        public Task<Dummy> DeleteByIdAsync(string correlationId, string id)
        {
            using (var timing = Instrument(correlationId, "dummy.delete_by_id"))
            {
                return Task.FromResult(_logic.DeleteById(correlationId, id));
            }
        }
    }
}
