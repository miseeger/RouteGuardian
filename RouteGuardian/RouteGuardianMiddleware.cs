using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RouteGuardian
{
    public class RouteGuardianMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RouteGuardian _routeGuardian;
        private string _guardedPath;

        public RouteGuardianMiddleware(RequestDelegate next, string guardedPath = "")
        {
            _next = next;
            _routeGuardian = new RouteGuardian("access.json"); 
            _guardedPath = guardedPath;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.User.Identity!.IsAuthenticated)
            {
                if (context.Request.Path.ToString().StartsWith(_guardedPath))
                {
                    var subjects = context.User.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value.ToString())
                        .Aggregate((c1, c2) => $"{c1}|{c2}");

                    if (_routeGuardian.isGranted(context.Request.Method, context.Request.Path, subjects))
                    {
                        await _next(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                        //await context.Response.CompleteAsync();
                        await context.Response.WriteAsync($"Unauthorized - Access denied!");
                    }
                }
                else
                {
                    await _next(context);
                }
            }
            else
            {
                context.Response.StatusCode = 403;
                //await context.Response.CompleteAsync();
                await context.Response.WriteAsync($"Forbidden - Authentication failed!");
            }
        }
    }
}
