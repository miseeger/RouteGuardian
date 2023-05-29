using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RouteGuardian.Middleware.Misc
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _loggerFactory;
        private bool _returnQualifiedResponse;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, 
            bool returnQualifiedResponse)
        {
            _next = next;
            _loggerFactory = loggerFactory;
            _returnQualifiedResponse = returnQualifiedResponse;
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

                var result = _returnQualifiedResponse
                    ? JsonSerializer.Serialize(new {message = e?.Message})
                    : Const.GlobalException;
            
                var response = context.Response;
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                
                await response.WriteAsync(result);
            }
        }
    }
}
