using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RouteGuardian.Helper;
using RouteGuardian.Middleware;

namespace RouteGuardian.Policy;

public class RouteGuardianPolicy
{
    public class Requirement : IAuthorizationRequirement
    {
    }

    public class AuthorizationHandler : AuthorizationHandler<Requirement>
    {
        private IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RouteGuardianPolicy> _logger;
        private RouteGuardian _routeGuardian;
        private IWinHelper _winHelper;
        private IJwtHelper _jwtHelper;

        public AuthorizationHandler(
            IHttpContextAccessor httpContextAccesor,
            RouteGuardian routeGuardian,
            IServiceProvider serviceProvider,
            ILogger<RouteGuardianPolicy> logger
        )
        {
            _httpContextAccessor = httpContextAccesor;
            _routeGuardian = routeGuardian;
            _logger = logger;
            _winHelper = (IWinHelper) serviceProvider.GetService(typeof(IWinHelper));
            _jwtHelper = (IJwtHelper) serviceProvider.GetService(typeof(IJwtHelper));
        }

        private void LogUnauthorized(HttpContext context, string subjects)
        {
            _logger.LogWarning(
                $"Unauthorized - Access denied!\r\n[{context.Request.Method}] " +
                $"{context.Request.Path} <- {context.User.Identity.Name} " +
                $"With roles {(subjects == string.Empty ? "missing!" : subjects)}");
        }
        
        private void LogForbidden(HttpContext context)
        {
            _logger.LogWarning(
                $"Forbidden - Authentication failed (not authenticated)!\r\n[{context.Request.Method}] " +
                $"{context.Request.Path} <- {context.User.Identity.Name}");
        }


        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            Requirement requirement)
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            if (!context.User.Identity!.IsAuthenticated)
            {
                LogForbidden(httpContext!);
                context.Fail();
                return Task.CompletedTask;
            } 
          
            var request = httpContext!.Request;
            var authHeader = request.Headers[Const.AuthHeader].ToString();
            var subjects = string.Empty;

            if (httpContext.User.Identity!.AuthenticationType != Const.Ntlm && string.IsNullOrEmpty(authHeader))
            {
                LogUnauthorized(httpContext, subjects);
                context.Fail();
                return Task.CompletedTask;
            }

            if (authHeader.StartsWith(Const.BearerTokenPrefix))
                subjects = _jwtHelper?.GetSubjectsFromJwtToken(authHeader);

            if (httpContext.User.Identity!.AuthenticationType == Const.Ntlm)
                subjects = _winHelper.GetSubjectsFromWinUserGroups(httpContext);
            
            if (_routeGuardian.IsGranted(request!.Method, request.Path, subjects!))
                context.Succeed(requirement);
            else
            {
                LogUnauthorized(httpContext, subjects!);
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}