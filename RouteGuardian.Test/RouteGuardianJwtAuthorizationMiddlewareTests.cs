using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using RouteGuardian.Helper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RouteGuardian.Extension;

namespace RouteGuardian.Test
{
    
    // ============================================================= //
    // Remains to have a demo on how to setup Http-Hosts for testing //
    // ============================================================= //
    
    public class RouteGuardianJwtAuthorizationMiddlewareTests
    {
        private static IConfiguration _config;
        private static JwtHelper _jwtHelper;
        private static string _secretKey;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _jwtHelper = new JwtHelper(_config);
            _secretKey = Environment.GetEnvironmentVariable(_config["RouteGuardian:JwtAuthentication:ApiSecretEnVarName"])!;
        }


        [TestMethod]
        public async Task ShouldReturnForbiddenNotAuthenticated()
        {
            // Arrange - Testserver
            var cBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            var config = cBuilder.Build();

            using var host = await new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.AddAuthentication();
                        services.AddJwtAuthentication(config);
                        services.AddScoped<IJwtHelper>(h => new JwtHelper(config));

                    });
                    builder.Configure(app =>
                    {
                        app.UseAuthentication();
                        //app.UseRouteGuardianJwtAuthorization();
                    });
                    builder.UseTestServer();
                }).StartAsync();

            var server = host.GetTestServer();
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _secretKey);

            // Arrange - Request & Act
            var bodyStream = new MemoryStream();
            var context = await server.SendAsync(ctx =>
            {
                ctx.Request.Method = HttpMethods.Get;
                ctx.Request.Headers[Const.AuthHeader] = $"{Const.BearerTokenPrefix}{token}";
                ctx.Response.Body = bodyStream;
            });

            bodyStream.Seek(0, SeekOrigin.Begin);
            using var stringReader = new StreamReader(bodyStream);
            var body = await stringReader.ReadToEndAsync();

            // --- Assert
            Assert.AreEqual(403, context.Response.StatusCode);
            Assert.AreEqual("Forbidden - Authentication failed (not authenticated)!\r\n[GET]  <- ", body);
        }

        [TestMethod]
        public async Task ShouldReturnForbiddenNoBearerToken()
        {
            // Arrange - Testserver
            var cBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            var config = cBuilder.Build();

            using var host = await new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.AddAuthentication();
                        services.AddJwtAuthentication(config);
                        services.AddScoped<IJwtHelper>(h => new JwtHelper(config));

                    });
                    builder.Configure(app =>
                    {
                        app.UseAuthentication();
                        //app.UseRouteGuardianJwtAuthorization();
                    });
                    builder.UseTestServer();
                }).StartAsync();

            var server = host.GetTestServer();
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _secretKey);

            // Arrange - Request & Act
            var bodyStream = new MemoryStream();
            var context = await server.SendAsync(ctx =>
            {
                // fake authenticated User
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(null, "FakeAuthTypeToAuthenticateUser")); 
                ctx.Request.Method = HttpMethods.Get;
                ctx.Request.Headers[Const.AuthHeader] = token; // no "Bearer "-prefix!
                ctx.Response.Body = bodyStream;
            });

            bodyStream.Seek(0, SeekOrigin.Begin);
            using var stringReader = new StreamReader(bodyStream);
            var body = await stringReader.ReadToEndAsync();

            // --- Assert
            Assert.AreEqual(403, context.Response.StatusCode);
            Assert.AreEqual("Forbidden - Authentication failed (not a bearer token)!\r\n[GET]  <- ", body);
        }

        [TestMethod]
        public async Task ShouldReturnForbiddenInvalidToken()
        {
            // Arrange - Testserver
            var cBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            var config = cBuilder.Build();

            using var host = await new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.AddAuthentication();
                        services.AddJwtAuthentication(config);
                        services.AddScoped<IJwtHelper>(h => new JwtHelper(config));

                    });
                    builder.Configure(app =>
                    {
                        app.UseAuthentication();
                        // app.UseRouteGuardianJwtAuthorization();
                    });
                    builder.UseTestServer();
                }).StartAsync();

            var server = host.GetTestServer();
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _secretKey);

            // Arrange - Request & Act
            var bodyStream = new MemoryStream();
            var context = await server.SendAsync(ctx =>
            {
                // fake authenticated User
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(null, "FakeAuthTypeToAuthenticateUser"));
                ctx.Request.Method = HttpMethods.Get;
                ctx.Request.Headers[Const.AuthHeader] = $"{Const.BearerTokenPrefix}{token}";
                ctx.Response.Body = bodyStream;
            });

            bodyStream.Seek(0, SeekOrigin.Begin);
            using var stringReader = new StreamReader(bodyStream);
            var body = await stringReader.ReadToEndAsync();

            // --- Assert
            Assert.AreEqual(403, context.Response.StatusCode);
            Assert.AreEqual("Forbidden - Authentication failed (invalid token)!\r\n[GET]  <- ", body);
        }

        [TestMethod]
        public async Task ShouldInvokeNextMiddleware()
        {
            // Arrange - Testserver
            var cBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            var config = cBuilder.Build();

            using var host = await new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.AddAuthentication();
                        services.AddJwtAuthentication(config);
                        services.AddScoped<IJwtHelper>(h => new JwtHelper(config));

                    });
                    builder.Configure(app =>
                    {
                        app.UseAuthentication();
                        // app.UseRouteGuardianJwtAuthorization();
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("Pipeline successfully terminated.");
                        });
                    });
                    builder.UseTestServer();
                }).StartAsync();

            var server = host.GetTestServer();

            var jwtSettings = _config.GetSection("RouteGuardian:JwtAuthentication");
            var token = _jwtHelper.GenerateToken(
                new List<Claim>()
                {
                    new Claim(Const.JwtClaimTypeRole, "ADMIN|PROD")
                }
                , _secretKey, "TestUser", "0815", jwtSettings["ValidIssuer"],
                jwtSettings["ValidAudience"]);

            // Arrange - Request & Act
            var bodyStream = new MemoryStream();
            var context = await server.SendAsync(ctx =>
            {
                // fake authenticated User
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(null, "FakeAuthTypeToAuthenticateUser"));
                ctx.Request.Method = HttpMethods.Get;
                ctx.Request.Headers[Const.AuthHeader] = $"{Const.BearerTokenPrefix}{token}";
                ctx.Response.Body = bodyStream;
            });

            bodyStream.Seek(0, SeekOrigin.Begin);
            using var stringReader = new StreamReader(bodyStream);
            var body = await stringReader.ReadToEndAsync();

            // --- Assert
            Assert.AreEqual(200, context.Response.StatusCode);
            Assert.AreEqual("Pipeline successfully terminated.", body);
        }
    }
}