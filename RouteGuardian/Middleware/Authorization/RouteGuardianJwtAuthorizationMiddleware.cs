using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RouteGuardian.Helper;

namespace RouteGuardian.Middleware.Authorization
{
    public class RouteGuardianJwtAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IJwtHelper _jwtHelper;
        private readonly ILogger<RouteGuardianJwtAuthorizationMiddleware> _logger;

        public RouteGuardianJwtAuthorizationMiddleware(RequestDelegate next, IJwtHelper jwtHelper,
            ILogger<RouteGuardianJwtAuthorizationMiddleware> logger)
        {
            _next = next;
            _jwtHelper = jwtHelper;
            _logger = logger;
        }


        private async Task ReturnForbidden(HttpContext context, string detail)
        {
            var message = $"Forbidden - Authentication failed ({detail})!\r\n[{context.Request.Method}] " +
                          $"{context.Request.Path} <- {context.User.Identity!.Name}";

            _logger.LogWarning(message);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(message);
        }

        //Inject Service into Middleware: https://stackoverflow.com/a/52204074
        public async Task Invoke(HttpContext context, IServiceProvider services)
        {
            var authHeader = context.Request.Headers[Const.AuthHeader].ToString();

            if (context.User.Identity!.IsAuthenticated)
            {
                if (authHeader != string.Empty && authHeader.StartsWith(Const.BearerTokenPrefix))
                {
                    var userId = string.Empty;
                    var jwt = _jwtHelper!.ReadToken(authHeader);

                    if (jwt != null)
                    {
                        IRouteGuardianRoleLookup? roleLookup = services.GetService<IRouteGuardianRoleLookup>();

                        var roles = jwt.Claims
                            .FirstOrDefault(c => c.Type == Const.JwtClaimTypeRole)!.Value
                            .Split(Const.SeparatorPipe)
                        .ToList();
                        
                        try
                        {
                            // Lookup additional roles for the authenticated user
                            if (roles.Contains(Const.JwtDbLookupRole) && roleLookup != null)
                            {
                                userId = jwt.Claims.FirstOrDefault(c => c.Type == Const.JwtClaimTypeUserId)!.Value;
                                var lookupRoles = await roleLookup.LookupRolesAsync(userId);
                                roles.AddRange(lookupRoles.Split(Const.SeparatorPipe).ToList());
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Problem when looking up additional roles for user {userId}: {e.Message} - Pipeline continues!");
                        }

                        if (roles.Any())
                        {
                            var claims = roles
                                .Where(r => r != Const.JwtDbLookupRole)
                                .OrderBy(r => r)
                                .Select(role => new Claim(ClaimTypes.Role, role.ToUpper()))
                                .ToList();

                            context.User.AddIdentity(new ClaimsIdentity(claims));
                        }

                        await _next(context);
                    }
                    else
                    {
                        await ReturnForbidden(context, "invalid token");
                    }
                }
                else
                {
                    await ReturnForbidden(context, "not a bearer token");
                }
            }
            else
            {
                await ReturnForbidden(context, "not authenticated");
            }
        }
    }
}
