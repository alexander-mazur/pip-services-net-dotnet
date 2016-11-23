using System;
using System.Web.Http;
using PipServices.Commons.Data;
using PipServices.Net.Test;

namespace PipServices.Net.Rest
{
    [RoutePrefix("dummies")]
    //[MicroserviceExceptionFilterAttribute]
    public class DummyWebApiController : ApiController, IHttpLogicController<IDummyController>
    {
        public DummyWebApiController(IDummyController controller)
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
        public DataPage<Dummy> GetPageByFilter(
            [FromUri(Name = "correlation_id")] string correlationId = null, string key = null, string skip = null,
            string take = null, string total = null)
        {
            return Logic.GetPageByFilter(correlationId, new FilterParams(), new PagingParams());
        }

        [Route("{dummyId}")]
        [HttpGet]
        public Dummy GetDummyById(string dummyId,
            [FromUri(Name = "correlation_id")] string correlationId = null)
        {
            return Logic.GetOneById(correlationId, dummyId);
        }

        [Route("")]
        [HttpPost]
        public Dummy CreateDummy([FromBody] Dummy dummy,
            [FromUri(Name = "correlation_id")] string correlationId = null)
        {
            return Logic.Create(correlationId, dummy);
        }

        [Route("")]
        [HttpPut]
        public Dummy UpdateDummyAsync([FromBody] Dummy dummy,
            [FromUri(Name = "correlation_id")] string correlationId = null)
        {
            return Logic.Update(correlationId, dummy);
        }

        [Route("{dummyId}")]
        [HttpDelete]
        public void DeleteDummyAsync(string dummyId,
            [FromUri(Name = "correlation_id")] string correlationId = null)
        {
            Logic.DeleteById(correlationId, dummyId);
        }

        public IDummyController Logic { get; set; }
    }
}
