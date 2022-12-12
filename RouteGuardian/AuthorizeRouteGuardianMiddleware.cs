using System.Collections.Immutable;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using RouteGuardian.Helper;

namespace RouteGuardian
{
    public class AuthorizeRouteGuardianMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IJwtHelper? _jwtHelper;

        public AuthorizeRouteGuardianMiddleware(RequestDelegate next, IJwtHelper? jwtHelper = null)
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

            var authHeader = context.Request.Headers["Authorization"].ToString();

            // ----- Jwt processing -------------------------------------------
            if (authHeader != string.Empty && authHeader.StartsWith("Bearer "))
            {
                var jwt = _jwtHelper!.ReadToken(authHeader);

                if (jwt != null)
                {
                    var roles = jwt.Claims.FirstOrDefault(c => c.Type == "rol")!.Value.Split('|');

                    if (roles.Any())
                    {
                        var claims = roles
                            .Where(r => r != "DB^")
                            .OrderBy(r => r)
                            .Select(role => new Claim(ClaimTypes.Role, role.ToUpper()))
                            .ToList();
                        context.User.AddIdentity(new ClaimsIdentity(claims));
                    }

                    if (roles.Contains("DB^"))
                    {
                        //TODO: Zusätzlich Rollen des Users aus der Datenbank ermitteln
                        //      und als weitere Identity hinzufügen (void AddDbUserRoles(string userName))
                        var userName = context.User.Identity!.Name;
                    }
                }
            }
            else
            {
                if (authHeader != string.Empty && authHeader.StartsWith("Negotiate "))
                {
                    //TODO: Auslagern in eigenen WinAuthHelper und die AuthorizeRouteGuardianMiddleware
                    //      in Jwt und WinAuth splitten. 

                    // ----- Nur wenn explizit über die Config die Gruppen als
                    // ----- Rollen aus dem AD berücksichtigt werden sollen
                    // Quelle: https://stackoverflow.com/questions/34951713/aspnet5-windows-authentication-get-group-name-from-claims
                    var groups = new List<string>();

                    var wi = (WindowsIdentity)context.User.Identity!;
                    if (wi.Groups != null)
                        foreach (var group in wi.Groups)
                        {
                            try
                            {
                                groups.Add(group.Translate(typeof(NTAccount)).ToString());
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }

                    var claims = groups
                        .OrderBy(r => r)
                        .Select(group => new Claim(ClaimTypes.Role, group.ToUpper()))
                        .ToList();

                    // TODO: Entfernen, weil Test
                    claims.Add(new Claim(ClaimTypes.Role, "TRALALA"));

                    // "name"-Claim aus dem User.Name (ohne Domain), also bei "\\" trennen und zweiten Token nehmen
                    // "sub"-Claim aus der 
                    context.User.AddIdentity(new ClaimsIdentity(claims));

                    // --------------------------------------------------------

                    //TODO: Zusätzlich Rollen des Users aus der Datenbank ermitteln
                    //      und als weitere Identity hinzufügen (void AddDbUserRoles(string userName))
                    var userName = context.User.Identity!.Name;
                }
            }

            //Nur zum Testen hier drin! 
            //var isProd = context.User.IsInRole("PROD");
            //var isTralala = context.User.IsInRole("TRALALA");

            await _next(context);
        }
    }
}
