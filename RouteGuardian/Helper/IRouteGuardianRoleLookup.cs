namespace RouteGuardian.Helper
{
    public interface IRouteGuardianRoleLookup
    {
        Task<string> LookupRolesAsync(string userId);
        string LookupRoles(string userId);
    }
}
