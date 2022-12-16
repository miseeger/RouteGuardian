using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RouteGuardian.Helper;

namespace RouteGuardian.Extension
{
    public static class AuthServiceExtension
    {
        //References:
        //- https://www.infoworld.com/article/3669188/how-to-implement-jwt-authentication-in-aspnet-core-6.html

        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var jwtHelper = new JwtHelper(config);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = jwtHelper.GetTokenValidationParameters();
                });
            
            services.AddSingleton<IJwtHelper>(jwtHelper);
        }

        public static void AddWindowsAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var winHelper = new WinHelper();

            services
                .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

            services
                .AddAuthorization(options =>
                {
                    options.FallbackPolicy = options.DefaultPolicy;
                });

            services.AddSingleton<IWinHelper>(winHelper);
        }
    }
}
