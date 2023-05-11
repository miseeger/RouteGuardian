using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RouteGuardian.Helper;

namespace RouteGuardian.Policy;

public class RouteGuardianPolicy
{
    public class Requirement : IAuthorizationRequirement
    {
    }

    public class AuthorizationHandler : AuthorizationHandler<Requirement>
    {
        private IHttpContextAccessor _httpContextAccessor;
        private RouteGuardian _routeGuardian;
        private WinHelper _winHelper;

        public AuthorizationHandler(
            IHttpContextAccessor httpContextAccesor,
            RouteGuardian routeGuardian,
            WinHelper winHelper
        )
        {
            _httpContextAccessor = httpContextAccesor;
            _routeGuardian = routeGuardian;
            _winHelper = winHelper;
        }


        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            Requirement requirement)
        {
            var request = _httpContextAccessor?.HttpContext!.Request;

            if (!context.User.Identity!.IsAuthenticated) 
                return Task.CompletedTask;

            if (context.User.Identity!.AuthenticationType == Const.Ntlm)
            {
                _winHelper.RegisterWinUserGroupsAsRoleClaims(_httpContextAccessor?.HttpContext!);
            }
            
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

                    if (_routeGuardian.IsGranted(request!.Method, request.Path, subjects))
                    {
                        context.Succeed(requirement);
                    }
                    else
                    {
                        context.Fail();
                    }
                }
                else
                {
                    context.Fail();
                }
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}