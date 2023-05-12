using Microsoft.AspNetCore.Builder;
using RouteGuardian.Middleware;
using RouteGuardian.Middleware.Misc;

namespace RouteGuardian.Extension
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseRouteGuardian(this IApplicationBuilder app, string guardedPath)
        {
            app.UseMiddleware<RouteGuardianMiddleware>(guardedPath);
            return app;
        }
        
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
            return app;
        }
    }
}
