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
        public void ShouldGetTokenValidationParametersFromConfig()
        {
            // --- Arrange
            var jwtSettings = _config.GetSection("RouteGuardian:JwtAuthentication");

            // --- Act
            var tvp = _jwtHelper.GetTokenValidationParameters();

            // --- Assert (Secret key not asserted yet)
            Assert.AreEqual(jwtSettings["ValidateIssuer"].ToLower(), tvp.ValidateIssuer ? "true" : "false");
            Assert.AreEqual(jwtSettings["ValidateAudience"].ToLower(), tvp.ValidateAudience ? "true" : "false");
            Assert.AreEqual(jwtSettings["ValidateIssuerSigningKey"].ToLower(), tvp.ValidateIssuerSigningKey ? "true" : "false");
            Assert.AreEqual(jwtSettings["ValidateLifetime"].ToLower(), tvp.ValidateLifetime ? "true" : "false");
            Assert.AreEqual(jwtSettings["ValidIssuer"], tvp.ValidIssuer);
            Assert.AreEqual(jwtSettings["ValidAudience"], tvp.ValidAudience);
        }

        [TestMethod]
        public void ShouldGenerateToken()
        {
            // --- Arrange and Act
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _secretKey);

            // --- Assert
            Assert.AreNotEqual(token, string.Empty);
            Assert.AreEqual(3, token.Split('.').Length);
            Assert.IsTrue(token.StartsWith("ey"));
        }

        [TestMethod]
        public void ShouldValidateToken()
        {
            // --- Arrange
            var jwtSettings = _config.GetSection("RouteGuardian:JwtAuthentication");
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _secretKey, "", "", 
                jwtSettings["ValidIssuer"], jwtSettings["ValidAudience"]);

            // --- Act
            var isValid = _jwtHelper.ValidateToken(token);

            // --- Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void ShouldValidateInvalidToken()
        {
            // --- Arrange
            var jwtSettings = _config.GetSection("RouteGuardian:JwtAuthentication");
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _secretKey, "", "",
                "WrongIssuer", jwtSettings["ValidAudience"]);

            // --- Act
            var isValid = _jwtHelper.ValidateToken(token);

            // --- Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void ShouldReadToken()
        {
            // --- Arrange
            var jwtSettings = _config.GetSection("RouteGuardian:JwtAuthentication");
            var token = _jwtHelper.GenerateToken(new List<Claim>(), _secretKey,
                "TestUser", "0815", jwtSettings["ValidIssuer"], jwtSettings["ValidAudience"]);

            // --- Act
            var secToken = _jwtHelper.ReadToken(token);
            var audiences = secToken!.Audiences.ToArray();

            // --- Assert
            Assert.AreEqual(6, secToken!.Claims.Count());
            Assert.AreEqual(jwtSettings["ValidIssuer"], secToken!.Issuer);
            Assert.AreEqual(jwtSettings["ValidAudience"], audiences[0]);
            Assert.AreEqual("0815", secToken!.GetUserId());
            Assert.AreEqual("TestUser", secToken!.GetUserName());
            Assert.AreEqual(secToken!.IssuedAt.AddMinutes(1440), secToken!.ValidTo);
        }
    }
}