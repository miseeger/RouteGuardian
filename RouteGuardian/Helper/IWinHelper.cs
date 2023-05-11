using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace RouteGuardian.Helper
{
    public interface IWinHelper
    {
        string GetWinUserGroupsHash(WindowsIdentity identity);
        void RegisterWinUserGroupsAsRoleClaims(HttpContext context);
    }
}
