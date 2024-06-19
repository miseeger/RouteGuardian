using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.Extensions.Logging;
using RouteGuardian.Helper;
using RouteGuardian.Model;
using RouteGuardian.Policy;
using RouteGuardian.Test.Mocks;

namespace RouteGuardian.Test
{
    // Resources:
    //   - https://blog.stoverud.no/posts/how-to-unit-test-asp-net-core-authorizationhandler
    
    [TestClass]
    public class RouteGuardianApiKeyPolicyTests
    {
        private static IConfiguration? _config;
        private static ILogger<RouteGuardianApiKeyPolicy.AuthorizationHandler>? _loggerMock;
        private static ApiKeyVault? _vault;
        
        
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _loggerMock = new Mock<ILogger<RouteGuardianApiKeyPolicy.AuthorizationHandler>>().Object;
            _vault = JsonSerializer.Deserialize<ApiKeyVault>(File.ReadAllText(Const.DefaultApiKeysFile));
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedWithEmptyHeader()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianApiKeyPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianApiKeyPolicy.AuthorizationHandler(
                new HttpContextApiKeyAccessorMock("ctxNull", testUser).Object,
                new RouteGuardian(Const.DefaultAccessFile),
                _loggerMock!, _vault!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsFalse(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedWithEmptyClientHeaders()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianApiKeyPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianApiKeyPolicy.AuthorizationHandler(
                new HttpContextApiKeyAccessorMock("ctxEmpty", testUser).Object,
                new RouteGuardian(Const.DefaultAccessFile),
                _loggerMock!, _vault!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsFalse(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedWithInvalidClientIdClientHeaders()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianApiKeyPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianApiKeyPolicy.AuthorizationHandler(
                new HttpContextApiKeyAccessorMock("ctxInvalidClientId", testUser).Object,
                new RouteGuardian(Const.DefaultAccessFile),
                _loggerMock!, _vault!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsFalse(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }        
        
        [TestMethod]
        public async Task ShouldNotSucceedWithInvalidIpClientHeaders()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianApiKeyPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianApiKeyPolicy.AuthorizationHandler(
                new HttpContextApiKeyAccessorMock("ctxInvalidIp", testUser).Object,
                new RouteGuardian(Const.DefaultAccessFile),
                _loggerMock!, _vault!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsFalse(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedWithInvalidClientKeyClientHeaders()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianApiKeyPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianApiKeyPolicy.AuthorizationHandler(
                new HttpContextApiKeyAccessorMock("ctxInvalidClientKey", testUser).Object,
                new RouteGuardian(Const.DefaultAccessFile),
                _loggerMock!, _vault!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsFalse(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }

        [TestMethod]
        public async Task ShouldNotSucceedWithExpiredKey()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianApiKeyPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianApiKeyPolicy.AuthorizationHandler(
                new HttpContextApiKeyAccessorMock("ctxExpiredKey", testUser).Object,
                new RouteGuardian(Const.DefaultAccessFile),
                _loggerMock!, _vault!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsFalse(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedWithValidKeyButNoGuardAccess()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianApiKeyPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianApiKeyPolicy.AuthorizationHandler(
                new HttpContextApiKeyAccessorMock("ctxValidKeyButNoGuardAccess", testUser).Object,
                new RouteGuardian(Const.DefaultAccessFile),
                _loggerMock!, _vault!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsFalse(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }
        
        [TestMethod]
        public async Task ShouldSucceedWithValidKeyAndGuardAccess()
        {
            // --- Arrange
            var testUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authContext = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { new RouteGuardianApiKeyPolicy.Requirement() }, 
                testUser, null); 
            var authHandler = new RouteGuardianApiKeyPolicy.AuthorizationHandler(
                new HttpContextApiKeyAccessorMock("ctxValidAndGuardAccess", testUser).Object,
                new RouteGuardian(Const.DefaultAccessFile),
                _loggerMock!, _vault!);

            // --- Act
            await authHandler.HandleAsync(authContext);

            // --- Assert
            Assert.IsTrue(authContext.HasSucceeded);
            Assert.IsFalse(testUser.Identity!.IsAuthenticated);
        }
    }
}