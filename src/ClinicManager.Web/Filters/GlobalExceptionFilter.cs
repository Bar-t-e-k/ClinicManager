using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinicManager.Web.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Nieobsłużony wyjątek: {Message}", context.Exception.Message);

            var isApiRequest = context.HttpContext.Request.Path.StartsWithSegments("/api");

            if (isApiRequest)
            {
                context.Result = new ObjectResult(new { error = "Wystąpił wewnętrzny błąd serwera." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
            else
            {
                context.Result = new ViewResult { ViewName = "Error" };
                context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            context.ExceptionHandled = true;
        }
    }
}