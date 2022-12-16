using RouteGuardian.Helper;

namespace RouteGuardian.Test
{
    [TestClass]
    public class WinHelperTests
    {
        private static WinHelper _winHelper;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _winHelper = new WinHelper();
        }


        [TestMethod]
        public void ShouldRegisterGroupsAsRoles()
        {
            // --- Arrange

            // --- Act

            // --- Assert (Secret key not asserted yet)
            Assert.IsTrue(false);
        }
    }
}