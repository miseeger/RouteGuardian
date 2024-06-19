using Microsoft.AspNetCore.Authentication;

namespace RouteGuardian.Authentication;

public class ApiKeyAuthenticationOptions:         
    AuthenticationSchemeOptions
{
    public const string DefaultScheme = Const.ApiKeyDefaultAuthScheme;
    public string TokenHeaderName { get; set; } = Const.HeaderClientId;
}