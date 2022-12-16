using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RouteGuardian.Middleware
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


        private async Task ReturnUnauthorized(HttpContext context, string subjects)
        {
            context.Response.StatusCode = 401;
            //TODO: Log as warning
            await context.Response.WriteAsync($"Unauthorized - Access denied!\r\n[{context.Request.Method}] " +
                                              $"{context.Request.Path} <- {context.User.Identity!.Name} " +
                                              $"With roles {(subjects == string.Empty ? "missing!" : subjects)}");
        }

        private async Task ReturnForbidden(HttpContext context)
        {
            context.Response.StatusCode = 403;
            //TODO: Log as warning
            await context.Response.WriteAsync($"Forbidden - Authentication failed (not authenticated)!\r\n[{context.Request.Method}] " +
                                              $"{context.Request.Path} <- {context.User.Identity!.Name}");
        }


        public async Task Invoke(HttpContext context)
        {
            if (context.User.Identity!.IsAuthenticated)
            {
                if (context.Request.Path.ToString().StartsWith(_guardedPath))
                {
                    if (context.User.Claims.Any())
                    {
                        var roles = context.User.Claims
                            .Where(c => c.Type == ClaimTypes.Role)
                            .ToList();

                        if (roles.Any())
                        {
                            var subjects = roles
                                .Select(c => c.Value.ToString())
                                .Aggregate((c1, c2) => $"{c1}|{c2}");

                            if (_routeGuardian.isGranted(context.Request.Method, context.Request.Path, subjects))
                            {
                                await _next(context);
                            }
                            else
                            {
                                await ReturnUnauthorized(context, subjects);
                            }
                        }
                        else
                        {
                            await _next(context);
                        }
                    }
                    else
                    {
                        await ReturnUnauthorized(context, string.Empty);
                    }
                }
                else
                {
                    await _next(context);
                }
            }
            else
            {
                await ReturnForbidden(context);
            }
        }
    }
}
