using PipServices.Commons.Convert;
using PipServices.Commons.Errors;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace PipServices.Net.Rest
{
    public sealed class RestExceptionHandler : ExceptionHandler
    {
        public sealed class MicroserviceErrorResult : IHttpActionResult
        {
            private HttpRequestMessage _request;
            private readonly HttpResponseMessage _httpResponseMessage;

            public MicroserviceErrorResult(HttpRequestMessage request, HttpResponseMessage httpResponseMessage)
            {
                _request = request;
                _httpResponseMessage = httpResponseMessage;
            }

            Task<HttpResponseMessage> IHttpActionResult.ExecuteAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(_httpResponseMessage);
            }
        }

        public override void Handle(ExceptionHandlerContext context)
        {
            var correlationId =
                context.Request.GetQueryNameValuePairs().FirstOrDefault(x => x.Key == "correlation_id").Value;

            var errorDescription = ErrorDescriptionFactory.Create(context.Exception);
            errorDescription.CorrelationId = correlationId;

            var resp = new HttpResponseMessage
            {
                Content = new StringContent(JsonConverter.ToJson(errorDescription)),
                ReasonPhrase = errorDescription.Category,
                StatusCode = (HttpStatusCode)errorDescription.Status
            };

            context.Result = new MicroserviceErrorResult(context.Request, resp);
        }
    }
}
