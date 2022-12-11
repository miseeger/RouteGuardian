using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace RouteGuardian.Helper
{
    public interface IJwtHelper
    {
        TokenValidationParameters GetTokenValidationParameters();
        string GenerateToken(IEnumerable<Claim> clms, string key,
            string iss = "", string aud = "", string algo = SecurityAlgorithms.HmacSha256);
        bool ValidateToken(string authToken);
        JwtSecurityToken? ReadToken(string jwt);
    }
}
