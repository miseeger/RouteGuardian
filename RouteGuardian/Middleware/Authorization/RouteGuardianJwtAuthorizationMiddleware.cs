using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RouteGuardian.Helper;

namespace RouteGuardian.Middleware.Authorization
{
    public class RouteGuardianJwtAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IJwtHelper _jwtHelper;

        public RouteGuardianJwtAuthorizationMiddleware(RequestDelegate next, IJwtHelper jwtHelper)
        {
            _next = next;
            _jwtHelper = jwtHelper;
        }


        public async Task Invoke(HttpContext context)
        {
            var authHeader = context.Request.Headers[Const.AuthHeader].ToString();

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

                    if (roles.Contains(Const.JwtDbLookupRole))
                    {
                        //TODO: Zusätzlich Rollen des Users aus der Datenbank ermitteln
                        //      und als weitere Identity hinzufügen (void AddDbUserRoles(string userName))
                        var userName = context.User.Identity!.Name;
                    }
                }
            }

            await _next(context);
        }
    }
}
