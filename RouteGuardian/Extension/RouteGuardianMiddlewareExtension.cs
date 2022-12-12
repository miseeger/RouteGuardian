using Microsoft.AspNetCore.Builder;
using RouteGuardian.Middleware;
using RouteGuardian.Middleware.Authorization;

namespace RouteGuardian.Extension
{
    public static class RouteGuardianMiddlewareExtension
    {
        public static IApplicationBuilder UseRouteGuardian(this IApplicationBuilder app, string guardedPath)
        {
            app.UseMiddleware<RouteGuardianMiddleware>(guardedPath);
            return app;
        }

        public static IApplicationBuilder UseRouteGuardianJwtAuthorization(this IApplicationBuilder app)
        {
            app.UseMiddleware<RouteGuardianJwtAuthorizationMiddleware>();
            return app;
        }

        public static IApplicationBuilder UseRouteGuardianWinAuthorization(this IApplicationBuilder app)
        {
            app.UseMiddleware<RouteGuardianWinAuthorizationMiddleware>();
            return app;
        }
    }
}
