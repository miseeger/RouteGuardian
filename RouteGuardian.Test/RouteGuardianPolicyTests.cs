using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.Extensions.Logging;
using RouteGuardian.Helper;
using RouteGuardian.Policy;
using RouteGuardian.Test.Mocks;

namespace RouteGuardian.Test
{
    // Resources:
    //   - https://blog.stoverud.no/posts/how-to-unit-test-asp-net-core-authorizationhandler
    
    [TestClass]
    public class RouteGuardianPolicyTests
    {
        private static IConfiguration? _config;
        private static IJwtHelper? _jwtHelper;
        private static ILogger<RouteGuardianPolicy.AuthorizationHandler>? _loggerMock;
        private static string? _jwtToken;
        
        
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _jwtHelper = new JwtHelper(_config);
            _loggerMock = new Mock<ILogger<RouteGuardianPolicy.AuthorizationHandler>>().Object;
            
            var jwtSettings = _config.GetSection("RouteGuardian:JwtAuthentication");
            
            _jwtToken = _jwtHelper.GenerateToken(new List<Claim>()
                {
                    new (Const.JwtClaimTypeRole, "Admin|ADMIN_Sales|admin_MARKETING"),
                }, _jwtHelper.Secret, "admin", "0815",
                jwtSettings["ValidIssuer"], jwtSettings["ValidAudience"]);
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedIfNotAuthenticated ()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianPolicy.Requirement() }, 
                testUser, 
                null); 
            var authHandler = new RouteGuardianPolicy.AuthorizationHandler(
                new HttpContextAccessorMock("ctx1", testUser).Object,
                new RouteGuardian("access.json"),
                new ServiceProviderMock(_config!).Object,
                _loggerMock!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsFalse(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }

        [TestMethod]
        public async Task ShouldFailIfUserHasNoAuthHeaderToken()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity(null, "NotNtlmAuthType"));
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianPolicy.AuthorizationHandler(
                new HttpContextAccessorMock("ctx2", testUser).Object,
                new RouteGuardian("access.json"),
                new ServiceProviderMock(_config!).Object, _loggerMock!);

            // --- Act
            await authHandler.HandleAsync(authContext);
            
            // --- Assert
            Assert.IsTrue(authContext.HasFailed);
            Assert.IsFalse(authContext.HasSucceeded);
        }

        [TestMethod]
        public async Task ShouldSucceedIfUserAccessForGuardedPathIsAllowed()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity("JsonWebToken"));
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianPolicy.Requirement() }, 
                testUser, 
                null); 
            var authHandler = new RouteGuardianPolicy.AuthorizationHandler(
                new HttpContextAccessorMock("ctx3", testUser, _jwtToken).Object,
                new RouteGuardian("access.json"),
                new ServiceProviderMock(_config!).Object,
                _loggerMock!);

            // --- Act
            await authHandler.HandleAsync(authContext);
            
            // --- Assert
            Assert.IsTrue(authContext.HasSucceeded);
            Assert.IsFalse(authContext.HasFailed);
        }

        [TestMethod]
        public async Task ShouldFailIfUserAccessForGuardedPathIsDenied()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity("JsonWebToken"));
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianPolicy.Requirement() }, 
                testUser, 
                null); 
            var authHandler = new RouteGuardianPolicy.AuthorizationHandler(
                new HttpContextAccessorMock("ctx4", testUser, _jwtToken).Object,
                new RouteGuardian("access.json"),
                new ServiceProviderMock(_config!).Object,
                _loggerMock!);

            // --- Act
            await authHandler.HandleAsync(authContext);
            
            // --- Assert
            Assert.IsTrue(authContext.HasFailed);
        }
    }
}