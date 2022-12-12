using Microsoft.AspNetCore.Http;

namespace RouteGuardian.Helper
{
    public interface IWinHelper
    {
        void RegisterGroupsAsRoleClaims(HttpContext context);
    }
}
