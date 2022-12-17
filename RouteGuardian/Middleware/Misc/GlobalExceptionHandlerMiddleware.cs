using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RouteGuardian.Middleware.Misc
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        ILoggerFactory _loggerFactory;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _loggerFactory = loggerFactory;
        }


        public async Task Invoke(HttpContext context)
        {
            var logger = _loggerFactory.CreateLogger<GlobalExceptionHandlerMiddleware>();
            
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);

                var result = JsonSerializer.Serialize(new { message = e?.Message });
                
                var response = context.Response;
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                
                await response.WriteAsync(result);
            }
        }
    }
}
