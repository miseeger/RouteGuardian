using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;
using RouteGuardian.Middleware;
using RouteGuardian.Middleware.Authorization;

namespace RouteGuardian.Extension
{
    public static class JwtExtensions
    {
        public static string GetUserId(this JwtSecurityToken secToken)
        {
            return secToken.Claims.FirstOrDefault(t => t.Type == Const.JwtClaimTypeUserId)!.Value;
        }

        public static string GetUserName(this JwtSecurityToken secToken)
        {
            return secToken.Claims.FirstOrDefault(t => t.Type == Const.JwtClaimTypeUsername)!.Value;
        }
    }
}
