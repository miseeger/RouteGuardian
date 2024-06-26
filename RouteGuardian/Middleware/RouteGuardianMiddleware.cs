using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RouteGuardian.Helper;

namespace RouteGuardian.Middleware
{
    public class RouteGuardianMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RouteGuardian _routeGuardian;
        private readonly string _guardedPath;
        private readonly ILogger<RouteGuardianMiddleware> _logger;
        private readonly IWinHelper _winHelper;
        private readonly IJwtHelper _jwtHelper;

        public RouteGuardianMiddleware(
            RequestDelegate next, 
            ILogger<RouteGuardianMiddleware> logger,
            IServiceProvider serviceProvider,
            string guardedPath = "")
        {
            _next = next;
            _routeGuardian = new RouteGuardian(Const.DefaultAccessFile);
            _guardedPath = guardedPath;
            _logger = logger;
            _winHelper = (IWinHelper) serviceProvider.GetService(typeof(IWinHelper))!;
            _jwtHelper = (IJwtHelper) serviceProvider.GetService(typeof(IJwtHelper))!;
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
                    var request = context.Request!;
                    var authHeader = request.Headers[Const.AuthHeader].ToString();
                    var user = context.User;
                    
                    if (Const.WinAuthTypes.Contains(user.Identity.AuthenticationType!)  
                        || (!Const.WinAuthTypes.Contains(user.Identity.AuthenticationType!) && !string.IsNullOrEmpty(authHeader)))
                    {
                        var subjects = string.Empty;
                        
                        if (authHeader.StartsWith(Const.BearerTokenPrefix))
                            subjects = _jwtHelper?.GetSubjectsFromJwtToken(authHeader);

                        if (Const.WinAuthTypes.Contains(user.Identity.AuthenticationType!))
                            subjects = _winHelper.GetSubjectsFromWinUserGroups(context);
                        
                        if (_routeGuardian.IsGranted(context.Request.Method, context.Request.Path, subjects!))
                            await _next(context);
                        else
                            await ReturnUnauthorized(context, subjects!);
                    }
                    else
                        await ReturnUnauthorized(context, string.Empty);
                }
                else
                    await _next(context);
            }
            else
            {
                await ReturnForbidden(context);
            }
        }
    }
}
