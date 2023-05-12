using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using RouteGuardian.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using RouteGuardian.Helper;
using RouteGuardian.Test.Mocks;

namespace RouteGuardian.Test
{
    [TestClass]
    public class RouteGuardianMiddlewareTests
    {
        private static IConfiguration _config;
        private static IJwtHelper _jwtHelper;
        private static string _secretKey;
        private static string _jwtToken;
        
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _jwtHelper = new JwtHelper(_config);
            _secretKey = Environment.GetEnvironmentVariable(_config["RouteGuardian:JwtAuthentication:ApiSecretEnVarName"])!;
            
            var jwtSettings = _config.GetSection("RouteGuardian:JwtAuthentication");
            
            _jwtToken = _jwtHelper.GenerateToken(new List<Claim>()
                {
                    new Claim(Const.JwtClaimTypeRole, "ADMIN|ADMIN_SALES|ADMIN_MARKETING"),
                }, _secretKey, "admin", "0815",
                jwtSettings["ValidIssuer"], jwtSettings["ValidAudience"]);
        }
        
        [TestMethod]
        public async Task ShouldReturnForbiddenIfNotAuthenticated ()
        {
            // --- Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            };

            var nextMock = new Mock<RequestDelegate>();
            var loggerMock = new Mock<ILogger<RouteGuardianMiddleware>>();
            var middleware = new RouteGuardianMiddleware(nextMock.Object, loggerMock.Object, 
                new ServiceProviderMock(_config).Object);

            // --- Act
            await middleware.Invoke(context);

            // --- Assert
            Assert.AreEqual(403, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ShouldReturnUnauthorizedIfUserHasNoClaims()
        {
            // --- Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(null, "FakeAuthTypeToAuthenticateUser"))
            };

            var nextMock = new Mock<RequestDelegate>();
            var loggerMock = new Mock<ILogger<RouteGuardianMiddleware>>();
            var middleware = new RouteGuardianMiddleware(nextMock.Object, loggerMock.Object, 
                new ServiceProviderMock(_config).Object);

            // --- Act
            await middleware.Invoke(context);

            // --- Assert
            Assert.AreEqual(401, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ShouldInvokeNextMiddlewareIfPathIsNotGuarded()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(null, "FakeAuthTypeToAuthenticateUser")),
                Request =
                {
                    Path = "/foo"
                }
            };

            var nextMock = new Mock<RequestDelegate>();
            var loggerMock = new Mock<ILogger<RouteGuardianMiddleware>>();
            var middleware = new RouteGuardianMiddleware(nextMock.Object, loggerMock.Object, 
                new ServiceProviderMock(_config).Object, "/api");

            // --- Act
            await middleware.Invoke(context);

            // --- Assert
            nextMock.Verify(n => n.Invoke(context), Times.Once);
        }

        [TestMethod]
        public async Task ShouldInvokeNextMiddlewareIfUserAccessForGuardedPathIsAllowed()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity("FakeAuthTypeToAuthenticateUser")),
                Request =
                {
                    Method = "GET",
                    Path = "/api/test/test",
                    Headers =
                    {
                        new (Const.AuthHeader, $"{Const.BearerTokenPrefix}{_jwtToken}")
                    }
                },
            };

            // context.Request.Headers[Const.AuthHeader] = _jwtToken;

            var nextMock = new Mock<RequestDelegate>();
            var loggerMock = new Mock<ILogger<RouteGuardianMiddleware>>();
            var middleware = new RouteGuardianMiddleware(nextMock.Object, loggerMock.Object, 
                new ServiceProviderMock(_config).Object, "/api");

            // --- Act
            await middleware.Invoke(context);

            // --- Assert
            nextMock.Verify(n => n.Invoke(context), Times.Once);
        }

        [TestMethod]
        public async Task ShouldReturnUnauthorizedIfUserAccessForGuardedPathIsDenied()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Role, "PROD")
                }, "FakeAuthTypeToAuthenticateUser")),
                Request =
                {
                    Path = "/api/foo"
                }
            };

            var nextMock = new Mock<RequestDelegate>();
            var loggerMock = new Mock<ILogger<RouteGuardianMiddleware>>();
            var middleware = new RouteGuardianMiddleware(nextMock.Object, loggerMock.Object, 
                new ServiceProviderMock(_config).Object, "/api");

            // --- Act
            await middleware.Invoke(context);

            // --- Assert
            Assert.AreEqual(401, context.Response.StatusCode);
        }
    }
}