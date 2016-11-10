using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PipServices.Commons.Errors;

namespace PipServices.Net.Errors
{
    public class GlobalExceptionHandler : IExceptionFilter, IDisposable
    {
        public void OnException(ExceptionContext context)
        {
            var correlationId = context.HttpContext.Request.Query["correlation_id"];

            var response = ErrorDescriptionFactory.Create(context.Exception);
            response.CorrelationId = correlationId;

            context.Result = new JsonResult(response)
            {
                StatusCode = response.Status
            };
        }

        public void Dispose()
        {
        }
    }
}
