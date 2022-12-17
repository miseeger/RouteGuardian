using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RouteGuardian.Middleware
{
    public class RouteGuardianMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RouteGuardian _routeGuardian;
        private string _guardedPath;
        private readonly ILogger<RouteGuardianMiddleware> _logger;

        public RouteGuardianMiddleware(RequestDelegate next, ILogger<RouteGuardianMiddleware> logger, 
            string guardedPath = "")
        {
            _next = next;
            _routeGuardian = new RouteGuardian("access.json");
            _guardedPath = guardedPath;
            _logger = logger;
        }


        private async Task ReturnUnauthorized(HttpContext context, string subjects)
        {
            var message = $"Unauthorized - Access denied!\r\n[{context.Request.Method}] " +
                          $"{context.Request.Path} <- {context.User.Identity!.Name} " +
                          $"With roles {(subjects == string.Empty ? "missing!" : subjects)}";
            _logger.LogWarning(message);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(message);
        }

        private async Task ReturnForbidden(HttpContext context)
        {
            var message = $"Forbidden - Authentication failed (not authenticated)!\r\n[{context.Request.Method}] " +
                          $"{context.Request.Path} <- {context.User.Identity!.Name}";
            _logger.LogWarning(message);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(message);
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
