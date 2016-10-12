using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PipServices.Net.Test.Models;
using PipServices.Commons.Data;
using PipServices.Net.Test.Persistance;

namespace PipServices.Dummy.Runner.Controllers
{
    [Route("dummies")]
    //[MicroserviceExceptionFilterAttribute]
    public class DummyWebApiController : Controller//, IHttpLogicController<IDummyBusinessLogic>
    {
        public DummyWebApiController(IDummyRepository repository)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository));

            Repository = repository;
        }

        public IDummyRepository Repository { get; set; }

        [HttpGet]
        public async Task<IEnumerable<DummyObject>> GetDummiesAsync(
            [FromQuery(Name = "correlation_id")] string correlationId = null, string key = null, string skip = null,
            string take = null, string total = null)
        {
            //throw new CallError("call code", "call message");
            //throw new ArgumentNullException(nameof(correlationId));

            //var filter = FilterParams.FromTuples("key", key);
            //var paging = new PagingParams(skip, take, total);

            return await Repository.GetListByFilterAsync(correlationId, "", new SortParams(), CancellationToken.None);
        }

        [HttpGet("{dummyId}")]
        public async Task<DummyObject> GetDummyByIdAsync(string dummyId,
            [FromQuery(Name = "correlation_id")] string correlationId = null)
        {
            return await Repository.GetOneByIdAsync(correlationId, dummyId, CancellationToken.None);
        }

        [HttpPost]
        public async Task<DummyObject> CreateDummyAsync([FromBody] DummyObject dummy,
            [FromQuery(Name = "correlation_id")] string correlationId = null)
        {
            return await Repository.CreateAsync(correlationId, dummy, CancellationToken.None);
        }

        [HttpPut("{dummyId}")]
        public async Task<DummyObject> UpdateDummyAsync(string dummyId, [FromBody] DummyObject dummy,
            [FromQuery(Name = "correlation_id")] string correlationId = null)
        {
            return await Repository.UpdateAsync(correlationId, dummy, CancellationToken.None);
        }

        [HttpDelete("{dummyId}")]
        public async Task DeleteDummyAsync(string dummyId,
            [FromQuery(Name = "correlation_id")] string correlationId = null)
        {
            await Repository.DeleteByIdAsync(correlationId, dummyId, CancellationToken.None);
        }
    }
}
