using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        JwtSecurityToken? ReadToken(string jwt);
        string GetSubjectsFromJwtToken(string authToken);
    }
}
