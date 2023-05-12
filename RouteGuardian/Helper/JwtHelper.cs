using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace RouteGuardian.Helper
{
    public class JwtHelper: IJwtHelper
    {
        // References:
        // - https://dotnetcoretutorials.com/2020/01/15/creating-and-validating-jwt-tokens-in-asp-net-core/
        // - https://developer.okta.com/blog/2019/06/26/decode-jwt-in-csharp-for-authorization

        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }


        public TokenValidationParameters GetTokenValidationParameters()
        {
            var jwtSettings = _config.GetSection("RouteGuardian:JwtAuthentication");
            var secretKey = Environment.GetEnvironmentVariable(jwtSettings["ApiSecretEnVarName"]);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));

            return new TokenValidationParameters
            {
                ValidateIssuer = jwtSettings["ValidateIssuer"].ToLower() != "false",
                ValidateAudience = jwtSettings["ValidateAudience"].ToLower() != "false",
                ValidateIssuerSigningKey = jwtSettings["ValidateIssuerSigningKey"].ToLower() != "false",
                ValidateLifetime = jwtSettings["ValidateLifetime"].ToLower() != "false",
                ValidIssuer = jwtSettings["ValidIssuer"],
                ValidAudience = jwtSettings["ValidAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
            };
        }

        public string GenerateToken(List<Claim> claims, string key, 
            string userName = "", string userId = "",
            string issuer = "", string audience = "", int validForMinutes = 1440, 
            string algorithm = SecurityAlgorithms.HmacSha256)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, algorithm);

            if (claims.All(c => c.Type != Const.JwtClaimTypeUserId) && userId != string.Empty)
            {
                claims!.Add(new Claim(Const.JwtClaimTypeUserId, userId));
            }

            if (claims.All(c => c.Type != Const.JwtClaimTypeUsername) && userName != string.Empty)
            {
                claims!.Add(new Claim(Const.JwtClaimTypeUsername, userName));
            }
            
            if (claims.All(c => c.Type != Const.JwtClaimTypeIssuedAt))
            {
                claims.Add(new Claim(Const.JwtClaimTypeIssuedAt,
                    ((DateTime.UtcNow.Ticks - 621355968000000000) / 10000000).ToString()));
            }

            var secToken = new JwtSecurityToken(
                signingCredentials: credentials,
                issuer: issuer,
                audience: audience,
                claims: claims, 
                expires: DateTime.UtcNow.AddMinutes(validForMinutes));

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(secToken);
        }

        public bool ValidateToken(string authToken)
        {
            authToken = authToken.Replace(Const.BearerTokenPrefix, string.Empty);

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(authToken,
                    GetTokenValidationParameters(), out var validatedToken);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public JwtSecurityToken? ReadToken(string authToken)
        {
            var jwtSecHandler = new JwtSecurityTokenHandler();

            authToken = authToken.Replace(Const.BearerTokenPrefix, string.Empty);

            return ValidateToken(authToken)
                ? jwtSecHandler.ReadJwtToken(authToken)
                : null;
        }

        public string GetSubjectsFromJwtToken(string authToken)
        {
            var subjects = string.Empty;
            var jwt = ReadToken(authToken);

            if (jwt == null)
                return subjects;

            return jwt.Claims
                .FirstOrDefault(c => c.Type == Const.JwtClaimTypeRole)!.Value
                .Split(Const.SeparatorPipe)
                .Where(r => r != Const.JwtDbLookupRole)
                .OrderBy(r => r)
                .Aggregate((r1, r2) => $"{r1.ToUpper()}|{r2.ToUpper()}");
        }
    }
}
