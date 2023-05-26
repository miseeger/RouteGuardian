using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using RouteGuardian.Helper;
using RouteGuardian.Extension;

namespace RouteGuardian.Test
{
    [TestClass]
    public class JwtHelperTests
    {
        private static IConfiguration _config;
        private static IJwtHelper _jwtHelper;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _jwtHelper = new JwtHelper(_config);
        }


        [TestMethod]
        public void ShouldGetTokenValidationParametersFromConfig()
        {
            // --- Arrange

            // --- Act
            var tvp = _jwtHelper.GetTokenValidationParameters();

            // --- Assert (Secret key not asserted yet)
            Assert.AreEqual(_jwtHelper.Settings["ValidateIssuer"].ToLower(), tvp.ValidateIssuer ? "true" : "false");
            Assert.AreEqual(_jwtHelper.Settings["ValidateAudience"].ToLower(), tvp.ValidateAudience ? "true" : "false");
            Assert.AreEqual(_jwtHelper.Settings["ValidateIssuerSigningKey"].ToLower(), tvp.ValidateIssuerSigningKey ? "true" : "false");
            Assert.AreEqual(_jwtHelper.Settings["ValidateLifetime"].ToLower(), tvp.ValidateLifetime ? "true" : "false");
            Assert.AreEqual(_jwtHelper.Settings["ValidIssuer"], tvp.ValidIssuer);
            Assert.AreEqual(_jwtHelper.Settings["ValidAudience"], tvp.ValidAudience);
        }

        [TestMethod]
        public void ShouldGenerateToken()
        {
            // --- Arrange and Act
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _jwtHelper.Secret);

            // --- Assert
            Assert.AreNotEqual(token, string.Empty);
            Assert.AreEqual(3, token.Split('.').Length);
            Assert.IsTrue(token.StartsWith("ey"));
        }

        [TestMethod]
        public void ShouldValidateToken()
        {
            // --- Arrange
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _jwtHelper.Secret, "", "", 
                _jwtHelper.Settings["ValidIssuer"], _jwtHelper.Settings["ValidAudience"]);

            // --- Act
            var isValid = _jwtHelper.ValidateToken(token);

            // --- Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void ShouldValidateInvalidToken()
        {
            // --- Arrange
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _jwtHelper.Secret, "", "",
                "WrongIssuer", _jwtHelper.Settings["ValidAudience"]);

            // --- Act
            var isValid = _jwtHelper.ValidateToken(token);

            // --- Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void ShouldReadToken()
        {
            // --- Arrange
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _jwtHelper.Secret,
                "TestUser", "0815", _jwtHelper.Settings["ValidIssuer"], 
                _jwtHelper.Settings["ValidAudience"]);

            // --- Act
            var secToken = _jwtHelper.ParseToken(token);
            var audiences = secToken!.Audiences.ToArray();

            // --- Assert
            Assert.AreEqual(6, secToken!.Claims.Count());
            Assert.AreEqual(_jwtHelper.Settings["ValidIssuer"], secToken!.Issuer);
            Assert.AreEqual(_jwtHelper.Settings["ValidAudience"], audiences[0]);
            Assert.AreEqual("0815", secToken!.GetUserId());
            Assert.AreEqual("TestUser", secToken!.GetUserName());
            Assert.AreEqual(secToken!.IssuedAt.AddMinutes(1440), secToken!.ValidTo);
        }
    }
}