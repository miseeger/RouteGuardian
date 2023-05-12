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
        
        private Dictionary<string, WinUserGroups> _winUserGroupsCache { get; }

        public WinHelper()
        {
            _winUserGroupsCache = new Dictionary<string, WinUserGroups>();
        }
        
        
        public string GetWinUserGroupsHash(WindowsIdentity identity)
        {
            return identity.Groups!
                .Select(group => group.Value.ToUpper())
                .Aggregate((g1, g2) => $"{g1}{g2}")
                .ComputeMd5();
        }

        public string GetSubjectsFromWinUserGroups(HttpContext context)
        {
            var subjects = string.Empty;
            var wi = (WindowsIdentity) context.User.Identity!;

            if (wi.Groups == null)
                return subjects;

            var cachedWinUserGroups = ReadWinUserGroupsCache(wi.Name);
            var winUserGroupsHashCode = GetWinUserGroupsHash(wi);

            if (cachedWinUserGroups == null || cachedWinUserGroups?.HashCode != winUserGroupsHashCode)
            {
                subjects = wi.Groups
                    .Select(g => g.Translate(typeof(NTAccount)).ToString().ToUpper())
                    .OrderBy(g => g)
                    .Aggregate((g1, g2) => $"{g1}|{g2}");

                WriteWinUserGroupsCache(wi.Name, new WinUserGroups()
                {
                    Groups = subjects,
                    HashCode = winUserGroupsHashCode
                });
            }
            else
            {
                subjects = cachedWinUserGroups.Groups;
            }

            return subjects;
        }

        // ----- WinUserGroupsCache -------------------------------------------

        public void ClearWinUserGroupsCache()
        {
            _winUserGroupsCache.Clear();
        }

        private bool IsInWinUserGroupsCache(string username)
        {
            return _winUserGroupsCache!.ContainsKey(username.ToUpper());
        }

        private WinUserGroups? ReadWinUserGroupsCache(string username)
        {
            return IsInWinUserGroupsCache(username)
                ? _winUserGroupsCache[username.ToUpper()]
                : null;
        }

        private void WriteWinUserGroupsCache(string username, WinUserGroups claims)
        {
            username = username.ToUpper();

            if (IsInWinUserGroupsCache(username))
            {
                _winUserGroupsCache[username] = claims;
            }
            else
            {
                _winUserGroupsCache.Add(username, claims);
            }
        }
    }
}
