﻿using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace RouteGuardian.Helper
{
    public class WinHelper : IWinHelper
    {
        // References:
        // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-6.0&tabs=visual-studio#impersonation
        // https://stackoverflow.com/questions/34951713/aspnet5-windows-authentication-get-group-name-from-claims

        public void RegisterGroupsAsRoleClaims(HttpContext context)
        {
            var groups = new List<string>();
            var wi = (WindowsIdentity)context.User.Identity!;

            if (wi.Groups != null)
            {
                foreach (var group in wi.Groups)
                {
                    try
                    {
                        groups.Add(group.Translate(typeof(NTAccount)).ToString());
                    }
                    catch 
                    {
                        // ignored
                    }
                }
            }

            var claims = groups
                .OrderBy(r => r)
                .Select(group => new Claim(ClaimTypes.Role, group.ToUpper()))
                .ToList();

            context.User.AddIdentity(new ClaimsIdentity(claims));
        }
    }
}
