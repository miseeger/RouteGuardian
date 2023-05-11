using System.Security.Cryptography;
using System.Text;

namespace RouteGuardian.Extension;

public static class StringExtensions
{
    public static string ComputeMd5(this string s)
    {
        using var md5 = MD5.Create();
        
        return
            Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }
}