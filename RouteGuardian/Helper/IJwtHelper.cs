using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace RouteGuardian.Helper
{
    public interface IJwtHelper
    {
        IConfigurationSection Settings { get; }
        string Secret { get; }
        TokenValidationParameters GetTokenValidationParameters();

        string GenerateToken(List<Claim> claims, string key,
            string userName = "", string userId = "",
            string issuer = "", string audience = "", int validForMinutes = 1440,
            string algorithm = SecurityAlgorithms.HmacSha256);
        bool ValidateToken(string authToken);
        JwtSecurityToken? ParseToken(string jwt);
        string GetSubjectsFromJwtToken(string authToken);
        string GetTokenFromContext(HttpContext context);
        List<Claim>? GetTokenClaimsFromContext(HttpContext context);
        string? GetTokenClaimValueFromContext(HttpContext context, string claimType);
    }
}
