using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
using RouteGuardian.Middleware;

namespace RouteGuardian.Test
{
    [TestClass]
    public class RouteGuardianMiddlewareTests
    {
        [TestMethod]
        public async Task ShouldReturnForbiddenIfNotAuthenticated ()
        {
            // --- Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            };

            var nextMock = new Mock<RequestDelegate>();
            var middleware = new RouteGuardianMiddleware(nextMock.Object);

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
            var middleware = new RouteGuardianMiddleware(nextMock.Object);

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
            var middleware = new RouteGuardianMiddleware(nextMock.Object, "/api");

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
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Role, "ADMIN")
                }, "FakeAuthTypeToAuthenticateUser")),
                Request =
                {
                    Method = "GET",
                    Path = "/api/test/test"
                }
            };

            var nextMock = new Mock<RequestDelegate>();
            var middleware = new RouteGuardianMiddleware(nextMock.Object, "/api");

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
            var middleware = new RouteGuardianMiddleware(nextMock.Object, "/api");

            // --- Act
            await middleware.Invoke(context);

            // --- Assert
            Assert.AreEqual(401, context.Response.StatusCode);
        }
    }
}