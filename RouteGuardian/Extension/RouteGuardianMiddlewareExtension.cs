using Microsoft.AspNetCore.Builder;
using RouteGuardian;

namespace RouteGuardian.Extension
{
    public static class RouteGuardianMiddlewareExtension
    {
        public static IApplicationBuilder UseRouteGuardian(this IApplicationBuilder app, string guardedPath)
        {
            app.UseMiddleware<RouteGuardianMiddleware>(guardedPath);
            return app;
        }

        public static IApplicationBuilder AuthorizeRouteGuardian(this IApplicationBuilder app)
        {
            app.UseMiddleware<AuthorizeRouteGuardianMiddleware>();
            return app;
        }
    }
}
