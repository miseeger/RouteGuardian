using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using RouteGuardian.Authentication;
using RouteGuardian.Model;
using RouteGuardian.Test.Mocks;

namespace RouteGuardian.Test
{

    // Resources:
    //   - https://www.inoaspect.com.au/unit-testing-basic-authentication-handler-in-asp-net-core-c/

    [TestClass]
    public class ApiKeyAuthenticationHandlerTests
    {
        private static Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>> _options;
        private static Mock<ILoggerFactory> _logger;
        private static Mock<UrlEncoder> _encoder;
        private static Mock<ISystemClock> _clock;
        private static ApiKeyVault _vault;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _options = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();

            _options
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new ApiKeyAuthenticationOptions());

            var logger = new Mock<ILogger<ApiKeyAuthenticationHandler>>();
            _logger = new Mock<ILoggerFactory>();
            _logger
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(logger.Object);

            _encoder = new Mock<UrlEncoder>();
            _clock = new Mock<ISystemClock>();
            
            var vault = File.ReadAllText(Const.DefaultApiKeysFile);
            _vault = JsonSerializer.Deserialize<ApiKeyVault>(vault) ?? new ApiKeyVault();
        }
        
        
        [TestMethod]
        public async Task ShouldNotSucceedWithMissingAuthHeader()
        {
            var context = new DefaultHttpContext();
           
            var handler = new ApiKeyAuthenticationHandler(_options.Object, _logger.Object, _encoder.Object,
                _clock.Object, _vault);

            await handler.InitializeAsync(
                new AuthenticationScheme(Const.ApiKeyDefaultAuthScheme, null, typeof(ApiKeyAuthenticationHandler)), 
                context);
            var result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual($"Missing Authorization Header: {Const.HeaderClientId}", result.Failure!.Message);
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedWithEnptyAuthHeader()
        {
            var context = new DefaultHttpContext()
            {
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse("0.0.0.0")
                },
                Request =
                {
                    Headers =
                    {
                        new(Const.HeaderClientId, "")
                    }
                }
            };
           
            var handler = new ApiKeyAuthenticationHandler(_options.Object, _logger.Object, _encoder.Object,
                _clock.Object, _vault);

            await handler.InitializeAsync(
                new AuthenticationScheme(Const.ApiKeyDefaultAuthScheme, null, typeof(ApiKeyAuthenticationHandler)), 
                context);
            var result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("Invalid token from 0.0.0.0", result.Failure!.Message);
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedWithInvalidClientIdInAuthHeader()
        {
            var context = new DefaultHttpContext()
            {
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse("0.0.0.0")
                },
                Request =
                {

                    Headers =
                    {
                        new(Const.HeaderClientId, "6a3ffb74-b1bc-455c-8628-e9f300d93xXx")
                    }
                }
            };
           
            var handler = new ApiKeyAuthenticationHandler(_options.Object, _logger.Object, _encoder.Object,
                _clock.Object, _vault);

            await handler.InitializeAsync(
                new AuthenticationScheme(Const.ApiKeyDefaultAuthScheme, null, typeof(ApiKeyAuthenticationHandler)), 
                context);
            var result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("Invalid token from 0.0.0.0", result.Failure!.Message);
        }
        
        [TestMethod]
        public async Task ShouldNotSucceedWithValidClientIdFromInvalidIpAddressInAuthHeader()
        {
            var context = new DefaultHttpContext()
            {
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse("0.8.1.5")
                },
                Request =
                {

                    Headers =
                    {
                        new(Const.HeaderClientId, "6a3ffb74-b1bc-455c-8628-e9f300d934e3")
                    }
                }
            };
           
            var handler = new ApiKeyAuthenticationHandler(_options.Object, _logger.Object, _encoder.Object,
                _clock.Object, _vault);

            await handler.InitializeAsync(
                new AuthenticationScheme(Const.ApiKeyDefaultAuthScheme, null, typeof(ApiKeyAuthenticationHandler)), 
                context);
            var result = await handler.AuthenticateAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("Invalid token from 0.8.1.5", result.Failure!.Message);
        }
        
        [TestMethod]
        public async Task ShouldOnlySucceedWithValidClientIdFromValidIpAddressInAuthHeader()
        {
            var context = new DefaultHttpContext()
            {
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse("127.0.0.1")
                },
                Request =
                {

                    Headers =
                    {
                        new(Const.HeaderClientId, "043a8b62-5ddc-470e-a5ff-1ee2d1e303df")
                    }
                }
            };
           
            var handler = new ApiKeyAuthenticationHandler(_options.Object, _logger.Object, _encoder.Object,
                _clock.Object, _vault);

            await handler.InitializeAsync(
                new AuthenticationScheme(Const.ApiKeyDefaultAuthScheme, null, typeof(ApiKeyAuthenticationHandler)), 
                context);
            var result = await handler.AuthenticateAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("ApiClientWithValidKey", 
                result.Principal.Claims.FirstOrDefault(c => c.Type == "ClientName")!.Value);
        }
    }
    
    
    
    
    
}