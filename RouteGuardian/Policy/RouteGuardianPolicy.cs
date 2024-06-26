﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RouteGuardian.Helper;

namespace RouteGuardian.Policy;

public class RouteGuardianPolicy
{
    public class Requirement : IAuthorizationRequirement
    {
    }

    public class AuthorizationHandler : AuthorizationHandler<Requirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthorizationHandler> _logger;
        private readonly RouteGuardian _routeGuardian;
        private readonly IWinHelper _winHelper;
        private readonly IJwtHelper _jwtHelper;

        public AuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            RouteGuardian routeGuardian,
            IServiceProvider serviceProvider,
            ILogger<AuthorizationHandler> logger
        )
        {
            _httpContextAccessor = httpContextAccessor;
            _routeGuardian = routeGuardian;
            _logger = logger;
            _winHelper = (IWinHelper) serviceProvider.GetService(typeof(IWinHelper))!;
            _jwtHelper = (IJwtHelper) serviceProvider.GetService(typeof(IJwtHelper))!;
        }

        private void LogUnauthorized(HttpContext context, string subjects)
        {
            var userName = context.User.Identity?.Name ?? "Unauthorized User";
            
            _logger.LogWarning(
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                $"Unauthorized - Access denied!\r\n[{context.Request.Method}] " +
                $"{context.Request.Path} <- {userName} " +
                $"With roles {(subjects == string.Empty ? "missing!" : subjects)}");
        }
        
        private void LogForbidden(HttpContext context)
        {
            var userName = context.User.Identity?.Name ?? "Unauthorized User";
            
            _logger.LogWarning(
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                "Forbidden - Authentication failed (not authenticated)!\r\n" +
                $"[{context.Request.Method}] {context.Request.Path} <- {userName}");
        }


        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            Requirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (!context.User.Identity!.IsAuthenticated)
            {
                LogForbidden(httpContext!);
                return Task.CompletedTask;
            } 
          
            var request = httpContext!.Request;
            var authHeader = request.Headers[Const.AuthHeader].ToString();
            var subjects = string.Empty;

            if (!Const.WinAuthTypes.Contains(httpContext.User.Identity!.AuthenticationType!) && string.IsNullOrEmpty(authHeader))
            {
                LogUnauthorized(httpContext, subjects);
                context.Fail();
                return Task.CompletedTask;
            }

            if (authHeader!.StartsWith(Const.BearerTokenPrefix))
                subjects = _jwtHelper.GetSubjectsFromJwtToken(authHeader);

            if (Const.WinAuthTypes.Contains(httpContext.User.Identity!.AuthenticationType!))
                subjects = _winHelper.GetSubjectsFromWinUserGroups(httpContext);
            
            if (_routeGuardian.IsGranted(request.Method, request.Path, subjects))
                context.Succeed(requirement);
            else
            {
                LogUnauthorized(httpContext, subjects);
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}