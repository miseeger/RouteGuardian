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
                _winHelper!.RegisterGroupsAsRoleClaims(context);

                //TODO ... maybe.
                //if (_config[Const.SetReigsterAdditionalGroupsFromDb].ToLower() == "true")
                //{
                //    //Add additional Roles/Groups from a database and add them as
                //    //an Identity to the user (void AddDbUserRoles(string userName))
                //}
            }

            await _next(context);
        }
    }
}
