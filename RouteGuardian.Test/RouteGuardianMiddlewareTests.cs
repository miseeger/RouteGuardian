using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RouteGuardian.Model;
using System.Security.Claims;
using System.Security.Principal;
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
        public async Task ShouldReturnUnauthorizedIfUserAccessIsDenied()
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
            var middleware = new RouteGuardianMiddleware(nextMock.Object,"/api");

            // --- Act
            await middleware.Invoke(context);

            Assert.AreEqual(401, context.Response.StatusCode);
        }
    }
}