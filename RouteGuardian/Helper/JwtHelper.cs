﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace RouteGuardian.Helper
{
    public class JwtHelper: IJwtHelper
    {
        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }


        public TokenValidationParameters GetTokenValidationParameters()
        {
            var jwtSettings = _config.GetSection("JwtAuthentication");
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

        public string GenerateToken(IEnumerable<Claim> clms, string key, 
            string iss = "", string aud = "", string algo = SecurityAlgorithms.HmacSha256)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, algo);

            var secToken = new JwtSecurityToken(
                signingCredentials: credentials,
                issuer: iss,
                audience: aud,
                claims: clms,
                expires: DateTime.UtcNow.AddDays(1));

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(secToken);
        }

        public bool ValidateToken(string authToken)
        {
            if (authToken.StartsWith("Bearer "))
            {
                authToken = authToken.Replace("Bearer ", string.Empty);
            }

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
            // Info: https://developer.okta.com/blog/2019/06/26/decode-jwt-in-csharp-for-authorization

            var jwtSecHandler = new JwtSecurityTokenHandler();

            authToken = authToken.Replace("Bearer ", "");

            return ValidateToken(authToken)
                ? jwtSecHandler.ReadJwtToken(authToken)
                : null;
        }
    }
}