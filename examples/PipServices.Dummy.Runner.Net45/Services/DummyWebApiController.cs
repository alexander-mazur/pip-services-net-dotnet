using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using PipServices.Commons.Data;
using PipServices.Dummy.Runner.Models;
using PipServices.Dummy.Runner.Persistance;
using PipServices.Net.Rest;

namespace PipServices.Dummy.Runner.Services
{
    [RoutePrefix("dummies")]
    //[MicroserviceExceptionFilterAttribute]
    public class DummyWebApiController : ApiController, IHttpLogicController<IDummyRepository>
    {
        public DummyWebApiController(IDummyRepository controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            Logic = controller;
        }

        public DummyWebApiController()
        {
        }

        [Route("")]
        [HttpGet]
        public async Task<List<DummyObject>> GetDummiesAsync(
            [FromUri(Name = "correlation_id")] string correlationId = null, string key = null, string skip = null,
            string take = null, string total = null)
        {
            return await Logic.GetListByQueryAsync(correlationId, string.Empty, new SortParams());
        }

        [Route("{dummyId}")]
        [HttpGet]
        public async Task<DummyObject> GetDummyById(string dummyId,
            [FromUri(Name = "correlation_id")] string correlationId = null)
        {
            return await Logic.GetOneByIdAsync(correlationId, dummyId);
        }

        [Route("")]
        [HttpPost]
        public async Task<DummyObject> CreateDummy([FromBody] DummyObject dummy,
            [FromUri(Name = "correlation_id")] string correlationId = null)
        {
            return await Logic.CreateAsync(correlationId, dummy);
        }

        [Route("")]
        [HttpPut]
        public async Task<DummyObject> UpdateDummyAsync([FromBody] DummyObject dummy,
            [FromUri(Name = "correlation_id")] string correlationId = null)
        {
            return await Logic.UpdateAsync(correlationId, dummy);
        }

        [Route("{dummyId}")]
        [HttpDelete]
        public async Task DeleteDummyAsync(string dummyId,
            [FromUri(Name = "correlation_id")] string correlationId = null)
        {
            await Logic.DeleteByIdAsync(correlationId, dummyId);
        }

        public IDummyRepository Logic { get; set; }
    }
}
