using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RouteGuardian.Model;

namespace RouteGuardian.Policy;

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

// https://juldhais.net/how-to-get-client-ip-address-and-location-information-in-asp-net-core-c2bb50e689c3

public class RouteGuardianApiKeyPolicy
{
    public class Requirement : IAuthorizationRequirement
    {
    }

    public class AuthorizationHandler : AuthorizationHandler<Requirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ILogger<AuthorizationHandler> _logger;
        private readonly RouteGuardian _routeGuardian;
        private readonly ApiKeyVault _vault;


        public AuthorizationHandler(IHttpContextAccessor httpContextAccessor,
            RouteGuardian routeGuardian, ILogger<AuthorizationHandler> logger,
            ApiKeyVault vault)
        {
            _httpContextAccessor = httpContextAccessor;
            _routeGuardian = routeGuardian;
            _logger = logger;
            _vault = vault;

            try
            {
                var apiKeys = File.ReadAllText(Const.DefaultApiKeysFile);
                _vault = JsonSerializer.Deserialize<ApiKeyVault>(apiKeys) ?? new ApiKeyVault();
            }
            catch (Exception e)
            {
                _logger.LogError("Problems reading '{ApiKeysFile}'\r\n{Message}\r\n{StackTrace}",
                    Const.DefaultApiKeysFile, e.Message, e.StackTrace);
            }
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Requirement requirement)
        {
            if (_vault.ApiKeys.Count == 0)
            {
                context.Fail();
                _logger.LogError("No API-Keys found, key vault is empty");
            }
            else
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var headers = httpContext!.Request.Headers;
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

                headers.TryGetValue(Const.HeaderClientId, out var clientId);
                headers.TryGetValue(Const.HeaderClientKey, out var clientKey);

                List<string>? validKeys = null;

                // 1) find valid keys for IP/ClientId combination
                try
                {
                    validKeys = _vault.ApiKeys
                        .FirstOrDefault(v => v.ClientId == clientId.ToString() && v.IpAddresses.Contains(ipAddress))?
                        .Keys
                        .Where(k => k.ValidUntil >= DateTime.Now)
                        .Select(k => k.Secret)
                        .ToList();
                }
                catch (Exception e)
                {
                    context.Fail();
                    _logger.LogError(
                        "Problem while checking key vault. Check '{ApiKeysFile}' for missing data and typos." +
                        "\r\n{Message}\r\n{Stacktrace}", Const.DefaultApiKeysFile, e.Message, e.StackTrace);
                }

                if (context.HasFailed)
                    return Task.CompletedTask;

                // 1a) no fail when searching key vault but no valid key found
                if (validKeys is null || validKeys.Count == 0)
                {
                    context.Fail();
                    _logger.LogWarning(
                        "Forbidden - No valid Key!\r\n[{Method}] {Path} <- Client {ClientId}/IP {Ip}/Key {ClientKey}\r\n",
                        httpContext.Request.Method, httpContext.Request.Path, clientId, ipAddress, clientKey);
                }
                else
                {
                    // 2) valid key?
                    if (validKeys.Contains(clientKey.ToString()))
                    {
                        var subject = _vault.ApiKeys
                            .FirstOrDefault(v =>
                                v.ClientId == clientId.ToString() && v.IpAddresses.Contains(ipAddress!))!
                            .ClientName;

                        // 3) check Guard Access for Client
                        if (_routeGuardian.IsGranted(httpContext.Request.Method, httpContext.Request.Path, subject))
                            context.Succeed(requirement);
                        else
                        {
                            // 3a) no access to route
                            _logger.LogWarning(
                                "Unauthorized - Access denied!\r\n[{Method}] {Path} <- {Client}",
                                httpContext.Request.Method, httpContext.Request.Path, subject);
                            context.Fail();
                        }
                    }
                    else
                    {
                        // 2a) Invalid key!
                        context.Fail();
                        _logger.LogWarning(
                            "Forbidden - Invalid Key Client!\r\n[{Method}] {Path} <- {ClientId}/IP {Ip}/Key {ClientKey}",
                            httpContext.Request.Method, httpContext.Request.Path, clientKey, ipAddress, clientId);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}