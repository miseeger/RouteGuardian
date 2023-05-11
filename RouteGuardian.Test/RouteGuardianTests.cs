using Microsoft.AspNetCore.Http;
using RouteGuardian.Model;

namespace RouteGuardian.Test
{
    [TestClass]
    public class RouteGuardianTests
    {
        [TestMethod]
        public void WorksForDefaultPolicyAllow()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("*", "/back", "*")
                .Allow("*", "/back", "ADMIN|PROD")
                .Deny("*", "/back/users", "PROD");

            // Access granted by default
            Assert.IsTrue(GuardPolicy.Allow == routeGuardian.Policy);
            // Access granted by default
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/blog", "CLIENT"));
            // Access to a specific path denied to all
            Assert.IsFalse(routeGuardian.IsGranted("GET", "/back", "CLIENT"));
            // Access to a path granted to a specific subject
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/back", "PROD"));
            // Access to a subpath denied to a specific subject
            Assert.IsFalse(routeGuardian.IsGranted("GET", "/back/users", "PROD"));
        }

        [TestMethod]
        public void WorksForDefaultPolicyDeny()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Deny)
                .Allow("*", "/admin", "ADMIN|PROD")
                .Deny("*", "/admin/part2", "*")
                .Allow("*", "/admin/part2", "ADMIN");

            // Access denied by default
            Assert.IsTrue(GuardPolicy.Deny == routeGuardian.Policy);
            // Access to a specific path granted to specific subjects
            Assert.IsTrue(!routeGuardian.IsGranted("GET", "/admin", "CLIENT")
                           && routeGuardian.IsGranted("GET", "/admin", "ADMIN")
                           && routeGuardian.IsGranted("GET", "/admin", "PROD"));
            // Access to a subpath granted to a specific subject (subpath precedence)
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/admin/part2", "ADMIN"));
            // Access to a subpath denied to others (subpath precedence)
            Assert.IsFalse(routeGuardian.IsGranted("GET", "/admin/part2", "PROD"));
        }

        [TestMethod]
        public void WorksForWildcards()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Deny)
                .Deny("*", "/admin*", "*")
                .Allow("*", "/admin*", "ADMIN")
                .Allow("*", "/admin/part2", "ADMIN");

            // Wildcard suffix
            Assert.IsTrue(!routeGuardian.IsGranted("GET", "/admin")
                           && !routeGuardian.IsGranted("GET", "/admin/foo/bar")
                           && routeGuardian.IsGranted("GET", "/admin", "ADMIN")
                           && routeGuardian.IsGranted("GET", "/admin/foo/bar", "ADMIN"));

            routeGuardian
                .Deny("*", "/*/edit", "*")
                .Allow("*", "/*/edit", "ADMIN");

            // Wildcard prefix
            Assert.IsTrue(!routeGuardian.IsGranted("GET", "/blog/entry/edit")
                           && routeGuardian.IsGranted("GET", "/blog/entry/edit", "ADMIN"));

            routeGuardian
                .Allow("*", "/admin", "*")
                .Allow("*", "/admin/special/path", "*");

            // Wildcard precedence order
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/admin")
                           && !routeGuardian.IsGranted("GET", "/admin/foo/bar")
                           && routeGuardian.IsGranted("GET", "/admin", "ADMIN")
                           && routeGuardian.IsGranted("GET", "/admin/foo/bar", "ADMIN")
                           && routeGuardian.IsGranted("GET", "/admin/special/path")
                           && routeGuardian.IsGranted("GET", "/admin/special/path", "ADMIN"));
        }

        [TestMethod]
        public void WorksForVerbs()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("POST|PUT|DELETE", "/blog/entry", "*")
                .Allow("*", "/blog/entry", "ADMIN");

            Assert.IsTrue(routeGuardian.IsGranted("GET", "/blog/entry", "CLIENT")
                           && !routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT")
                           && routeGuardian.IsGranted("PUT", "/blog/entry", "ADMIN"));
        }

        [TestMethod]
        public void WorksForMultipleSubjects()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("POST|PUT|DELETE", "/blog/entry", "*")
                .Allow("*", "/blog/entry", "ADMIN");

            Assert.IsTrue(routeGuardian.IsGranted("GET", "/blog/entry", "CLIENT|CUSTOMER")
                           && !routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT|CUSTOMER")
                           && routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT|ADMIN"));
        }

        [TestMethod]
        public void WorksWithAutorizeMethod()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("POST|PUT|DELETE", "/blog/entry", "*")
                .Allow("*", "/blog/entry", "ADMIN");

            var context = new DefaultHttpContext
            {
                Request =
                {
                    Path = "/blog/entry",
                    Method = "GET"
                }
            };

            // Authorize an unidentified subject (default policy = allow)
            Assert.IsTrue(routeGuardian.Authorize(context));

            context.Request.Method = "POST";

            // Unauthorize an unidentified subject
            Assert.IsFalse(routeGuardian.Authorize(context));
            // Authorize an identified subject
            Assert.IsTrue(routeGuardian.Authorize(context, "ADMIN"));
            // Unauthorize an identified subject
            Assert.IsFalse(routeGuardian.Authorize(context, "CLIENT"));
            // Authorize a set of identified subjects
            Assert.IsTrue(routeGuardian.Authorize(context, "CLIENT|ADMIN"));
            // Unauthorize a set of identified subjects'
            Assert.IsFalse(routeGuardian.Authorize(context, "CLIENT|CUSTOMER"));
        }
    }
}