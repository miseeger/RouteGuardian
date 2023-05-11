using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using RouteGuardian.Extension;
using RouteGuardian.Model;

namespace RouteGuardian.Helper
{
    public class WinHelper : IWinHelper
    {
        // References:
        // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-6.0&tabs=visual-studio#impersonation
        // https://stackoverflow.com/questions/34951713/aspnet5-windows-authentication-get-group-name-from-claims
        
        private Dictionary<string, WinUserClaims> _winUserClaimsCache { get; }

        public WinHelper()
        {
            _winUserClaimsCache = new Dictionary<string, WinUserClaims>();
        }
        
        
        public string GetWinUserGroupsHash(WindowsIdentity identity)
        {
            return identity.Groups!
                .Select(group => group.Value.ToUpper())
                .Aggregate((g1, g2) => $"{g1}{g2}")
                .ComputeMd5();
        }

        public void RegisterWinUserGroupsAsRoleClaims(HttpContext context)
        {
            var wi = (WindowsIdentity) context.User.Identity!;

            if (wi.Groups == null) return;

            var cachedWinUserClaims = ReadWinUserClaimsCache(wi.Name);
            var winUserGroupsHashCode = GetWinUserGroupsHash(wi);

            if (cachedWinUserClaims == null || cachedWinUserClaims?.HashCode != winUserGroupsHashCode)
            {
                var claims = wi.Groups
                    .Select(g => g.Translate(typeof(NTAccount)).ToString())
                    .OrderBy(g => g)
                    .Select(g => new Claim(ClaimTypes.Role, g.ToUpper()))
                    .ToList();

                context.User.AddIdentity(new ClaimsIdentity(claims));

                WriteWinUserClaimsCache(wi.Name, new WinUserClaims()
                {
                    Claims = claims,
                    HashCode = winUserGroupsHashCode
                });
            }
            else
            {
                context.User.AddIdentity(new ClaimsIdentity(cachedWinUserClaims.Claims));
            }
        }

        // ----- WinUserGroupsCache -------------------------------------------

        public void ClearWinUserClaimsCache()
        {
            _winUserClaimsCache.Clear();
        }

        public bool IsInWinUserClaimsCache(string username)
        {
            return _winUserClaimsCache!.ContainsKey(username.ToUpper());
        }

        public WinUserClaims? ReadWinUserClaimsCache(string username)
        {
            return IsInWinUserClaimsCache(username)
                ? _winUserClaimsCache[username.ToUpper()]
                : null;
        }

        public void WriteWinUserClaimsCache(string username, WinUserClaims claims)
        {
            username = username.ToUpper();

            if (IsInWinUserClaimsCache(username))
            {
                _winUserClaimsCache[username] = claims;
            }
            else
            {
                _winUserClaimsCache.Add(username, claims);
            }
        }
    }
}
