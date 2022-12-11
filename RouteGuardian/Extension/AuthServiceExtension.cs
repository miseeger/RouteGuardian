using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RouteGuardian.Helper;

namespace RouteGuardian.Extension
{
    public static class AuthServiceExtension
    {
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

        public static void AddWindowsAuthentication(this IServiceCollection services)
        {
            services
                .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

            services
                .AddAuthorization(options =>
                {
                    options.FallbackPolicy = options.DefaultPolicy;
                });
        }
    }
}
