using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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


        public async Task Invoke(HttpContext context)
        {
            var authHeader = context.Request.Headers[Const.AuthHeader].ToString();

            if (context.User.Identity!.IsAuthenticated)
            {
                if (authHeader != string.Empty && authHeader.StartsWith(Const.BearerTokenPrefix))
                {
                    var jwt = _jwtHelper!.ReadToken(authHeader);

                    if (jwt != null)
                    {
                        var roles = jwt.Claims
                            .FirstOrDefault(c => c.Type == Const.JwtClaimTypeRole)!.Value
                            .Split(Const.SeparatorPipe);

                        if (roles.Any())
                        {
                            var claims = roles
                                .Where(r => r != Const.JwtDbLookupRole)
                                .OrderBy(r => r)
                                .Select(role => new Claim(ClaimTypes.Role, role.ToUpper()))
                                .ToList();

                            context.User.AddIdentity(new ClaimsIdentity(claims));
                        }

                        //TODO ... maybe.
                        //if (roles.Contains(Const.JwtDbLookupRole))
                        //{
                        //    //Add additional Roles/Groups from a database and add them as
                        //    //an Identity to the user (void AddDbUserRoles(string userName))
                        //}

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
