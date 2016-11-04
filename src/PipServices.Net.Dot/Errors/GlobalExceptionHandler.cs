using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using Newtonsoft.Json;
using PipServices.Commons.Errors;

namespace PipServices.Net.Errors
{
    public sealed class GlobalExceptionHandler : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext context)
        {
            var correlationId =
                context.Request.GetQueryNameValuePairs().FirstOrDefault(x => x.Key == "correlation_id").Value;

            var errorDescription = ErrorDescriptionFactory.Create(context.Exception);
            errorDescription.CorrelationId = correlationId;

            var resp = new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(errorDescription)),
                ReasonPhrase = errorDescription.Category,
                StatusCode = (HttpStatusCode)errorDescription.Status
            };

            context.Result = new MicroserviceErrorResult(context.Request, resp);
        }
    }
}
