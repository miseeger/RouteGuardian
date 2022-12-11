using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RouteGuardian.Helper;

namespace RouteGuardian
{
    public class AuthorizeRouteGuardianMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IJwtHelper _jwtHelper;

        public AuthorizeRouteGuardianMiddleware(RequestDelegate next, IJwtHelper jwtHelper)
        {
            _next = next;
            _jwtHelper = jwtHelper;
        }

        // TODO für RouteGuardianDbRolesMiddleware, um Benutzerrollen aus
        //      einer Datenbank zu holen und sie dann als Claims einer neuen
        //      ClaimsIdentity zum context.User hinzuzufügen.

        public async Task Invoke(HttpContext context)
        {
            // Info:
            // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-6.0&tabs=visual-studio#impersonation
            // var winUser = (WindowsIdentity) context.User.Identity!;
            // https://dotnetcoretutorials.com/2020/01/15/creating-and-validating-jwt-tokens-in-asp-net-core/

            // ----- Jwt processing -------------------------------------------
            var authHeader = context.Request.Headers["Authorization"].ToString();

            if (authHeader != string.Empty)
            {
                var jwt = _jwtHelper.ReadToken(authHeader);

                if (jwt != null)
                {
                    var roles = jwt.Claims.FirstOrDefault(c => c.Type == "rol")!.Value.Split('|');

                    if (roles.Any())
                    {
                        var claims = roles
                            .Select(role => new Claim(ClaimTypes.Role, role.ToUpper()))
                            .ToList();
                        context.User.AddIdentity(new ClaimsIdentity(claims));
                    }

                    // TODO: Prüfen, ob das geht:
                    //claims.AddRange(context.User.Claims);
                    //var ci = new ClaimsIdentity(claims);
                    //context.User = new ClaimsPrincipal(ci);
                }
            }

            //Nur zum Testen hier drin! 
            //var isProd = context.User.IsInRole("PROD");
            //var isTralala = context.User.IsInRole("TRALALA");

            await _next(context);
        }
    }
}
