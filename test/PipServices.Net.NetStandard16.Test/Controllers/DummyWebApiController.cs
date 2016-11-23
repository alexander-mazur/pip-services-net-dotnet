using System;
using Microsoft.AspNetCore.Mvc;
using PipServices.Commons.Data;
using PipServices.Net.Test;

namespace PipServices.Net.Controllers
{
    [Route("dummies")]
    //[MicroserviceExceptionFilterAttribute]
    public class DummyWebApiController : Controller //, IHttpLogicController<IDummyBusinessLogic>
    {
        public DummyWebApiController(IDummyController controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            Controller = controller;
        }

        public IDummyController Controller { get; set; }

        [HttpGet]
        public DataPage<Dummy> GetPageByFilter(
            [FromQuery(Name = "correlation_id")] string correlationId = null, string key = null, string skip = null,
            string take = null, string total = null)
        {
            //throw new CallError("call code", "call message");
            //throw new ArgumentNullException(nameof(correlationId));

            //var filter = FilterParams.FromTuples("key", key);
            //var paging = new PagingParams(skip, take, total);

            return Controller.GetPageByFilter(correlationId, new FilterParams(), new PagingParams());
        }

        [HttpGet("{dummyId}")]
        public Dummy GetDummyById(string dummyId,
            [FromQuery(Name = "correlation_id")] string correlationId = null)
        {
            return Controller.GetOneById(correlationId, dummyId);
        }

        [HttpPost]
        public Dummy CreateDummy([FromBody] Dummy dummy,
            [FromQuery(Name = "correlation_id")] string correlationId = null)
        {
            return Controller.Create(correlationId, dummy);
        }

        [HttpPut]
        public Dummy UpdateDummyAsync([FromBody] Dummy dummy,
            [FromQuery(Name = "correlation_id")] string correlationId = null)
        {
            return Controller.Update(correlationId, dummy);
        }

        [HttpDelete("{dummyId}")]
        public void DeleteDummyAsync(string dummyId,
            [FromQuery(Name = "correlation_id")] string correlationId = null)
        {
            Controller.DeleteById(correlationId, dummyId);
        }
    }
}
