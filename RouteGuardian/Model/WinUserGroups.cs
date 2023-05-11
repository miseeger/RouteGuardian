using System.Security.Claims;

namespace RouteGuardian.Model;

public class WinUserClaims
{
    public List<Claim> Claims { get; set; }
    public string HashCode { get; set; }
}