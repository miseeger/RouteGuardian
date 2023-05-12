using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace RouteGuardian.Helper
{
    public interface IJwtHelper
    {
        TokenValidationParameters GetTokenValidationParameters();

        string GenerateToken(List<Claim> claims, string key,
            string userName = "", string userId = "",
            string issuer = "", string audience = "", int validForMinutes = 1440,
            string algorithm = SecurityAlgorithms.HmacSha256);
        bool ValidateToken(string authToken);
        JwtSecurityToken ReadToken(string jwt);
        string GetSubjectsFromJwtToken(string authToken);
    }
}
