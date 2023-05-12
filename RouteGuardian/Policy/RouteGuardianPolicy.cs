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
        private IWinHelper _winHelper;
        private IJwtHelper _jwtHelper;

        public AuthorizationHandler(
            IHttpContextAccessor httpContextAccesor,
            RouteGuardian routeGuardian,
            IServiceProvider serviceProvider
        )
        {
            _httpContextAccessor = httpContextAccesor;
            _routeGuardian = routeGuardian;
            _winHelper = (IWinHelper) serviceProvider.GetService(typeof(IWinHelper));
            _jwtHelper = (IJwtHelper) serviceProvider.GetService(typeof(IJwtHelper));
        }


        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            Requirement requirement)
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            if (!context.User.Identity!.IsAuthenticated) 
                return Task.CompletedTask;
            
            var request = httpContext.Request;
            var authHeader = request.Headers[Const.AuthHeader].ToString();
            var subjects = string.Empty;

            if (authHeader.StartsWith(Const.BearerTokenPrefix) && string.IsNullOrEmpty(authHeader))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (authHeader.StartsWith(Const.BearerTokenPrefix))
                subjects = _jwtHelper?.GetSubjectsFromJwtToken(authHeader);

            if (httpContext.User.Identity!.AuthenticationType == Const.Ntlm)
                subjects = _winHelper.GetSubjectsFromWinUserGroups(httpContext);
            
            if (_routeGuardian.IsGranted(request!.Method, request.Path, subjects))
                context.Succeed(requirement);
            else
                context.Fail();

            return Task.CompletedTask;
        }
    }
}