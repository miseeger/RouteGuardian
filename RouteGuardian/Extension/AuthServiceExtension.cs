using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RouteGuardian.Authentication;
using RouteGuardian.Helper;
using RouteGuardian.Model;
using RouteGuardian.Policy;

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
            var winHelper = new WinHelper(config);

            services
                .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

            services
                .AddAuthorization(options => { options.FallbackPolicy = options.DefaultPolicy; });

            services.AddSingleton<IWinHelper>(winHelper);
        }

        public static void AddRouteGuardianPolicy(this IServiceCollection services,
            string accessFileName = "access.json")
        {
            var routeGuardian = new RouteGuardian(accessFileName);
            services.AddSingleton(routeGuardian);

            services.AddHttpContextAccessor();

            services.AddAuthorization(builder =>
            {
                builder.AddPolicy("RouteGuardian", pBuilder => pBuilder
                    .RequireAuthenticatedUser()
                    .AddRequirements(new RouteGuardianPolicy.Requirement())
                );
            });

            services.AddSingleton<IAuthorizationHandler, RouteGuardianPolicy.AuthorizationHandler>();
        }

        public static void AddRouteGuardianApiKeyPolicy(this IServiceCollection services,
            string accessFileName = "access.json")
        {
            var apiKeys = "{}";

            try
            {
                apiKeys = File.ReadAllText(Const.DefaultApiKeysFile);
            }
            catch
            {
                // ignored ... no logging at this place.
            }

            services.AddSingleton(JsonSerializer.Deserialize<ApiKeyVault>(apiKeys) ?? new ApiKeyVault());
            services.AddSingleton(new RouteGuardian(accessFileName));
            services.AddHttpContextAccessor();

            services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
                .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>
                    (ApiKeyAuthenticationOptions.DefaultScheme, options => { });

            services.AddAuthorization(builder =>
            {
                builder.AddPolicy("RouteGuardianApiKey", pBuilder => pBuilder
                    .RequireAuthenticatedUser()
                    .AddRequirements(new RouteGuardianApiKeyPolicy.Requirement())
                );
            });

            services.AddSingleton<IAuthorizationHandler, RouteGuardianApiKeyPolicy.AuthorizationHandler>();
        }
    }
}