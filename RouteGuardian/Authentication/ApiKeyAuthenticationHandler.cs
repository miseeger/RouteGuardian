using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RouteGuardian.Model;

namespace RouteGuardian.Authentication;

public class ApiKeyAuthenticationHandler :
    AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private ApiKeyVault _vault;
    
    public ApiKeyAuthenticationHandler
    (IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, 
        UrlEncoder encoder, ISystemClock clock, ApiKeyVault vault) : base(options, logger, encoder, clock)
    {
        _vault = vault;
    }

    
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1) Check for header
        if (!Request.Headers.ContainsKey(Options.TokenHeaderName))
        {
            return 
                Task.FromResult(
                    AuthenticateResult.Fail($"Missing Authorization Header: {Options.TokenHeaderName}")
                );
        }

        // 2) Get token and IP address
        var token = Request.Headers[Options.TokenHeaderName]!;
        var ipAddress = Context.Connection.RemoteIpAddress?.ToString();


        // 3) Find Client by token (x-client-id) and IP address
        var client = _vault.ApiKeys
            .FirstOrDefault(v => v.ClientId == token && v.IpAddresses.Contains(ipAddress!));
        
        // 3a) No corresponding client found from vault
        if (client is null)
        {
            return Task.FromResult(AuthenticateResult
                .Fail($"Invalid token from {ipAddress}"));
        }

        // 3b) Success
        var claims = new List<Claim>()
        {
            new Claim("ClientName", client.ClientName)
        };

        var claimsIdentity = new ClaimsIdentity(claims, this.Scheme.Name);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        return 
            Task.FromResult(
                AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, this.Scheme.Name))
            );
    }
}