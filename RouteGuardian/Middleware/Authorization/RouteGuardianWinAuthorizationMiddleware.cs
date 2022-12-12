using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RouteGuardian.Helper;

namespace RouteGuardian.Middleware.Authorization
{
    public class RouteGuardianWinAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;
        private readonly IWinHelper? _winHelper;

        public RouteGuardianWinAuthorizationMiddleware(RequestDelegate next,
            IConfiguration config, IWinHelper winHelper)
        {
            _next = next;
            _config = config;
            _winHelper = winHelper;
        }

        public async Task Invoke(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();

            if (authHeader != string.Empty && authHeader.StartsWith("Negotiate "))
            {
                if (_config[Const.SetRegisterGroupsAsRoles].ToLower() == "true")
                {
                    _winHelper!.RegisterGroupsAsRoleClaims(context);
                }

                //TODO: Zusätzlich Rollen des Users aus der Datenbank ermitteln und als
                //TODO: weitere Identity hinzufügen (void AddDbUserRoles(string userName))
            }

            await _next(context);
        }
    }
}
