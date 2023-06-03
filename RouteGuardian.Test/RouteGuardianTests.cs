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
                .Allow("*", "/back", "admin|Prod")
                .Deny("*", "/back/users", "PROD");

            // Access granted by default
            Assert.IsTrue(GuardPolicy.Allow == routeGuardian.Policy);
            // Access granted by default
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/blog", "client"));
            // Access to a specific path denied to all
            Assert.IsFalse(routeGuardian.IsGranted("GET", "/back", "CLIENT"));
            // Access to a path granted to a specific subject
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/back", "prod"));
            // Access to a subpath denied to a specific subject
            Assert.IsFalse(routeGuardian.IsGranted("GET", "/back/users", "Prod"));
        }

        [TestMethod]
        public void WorksForDefaultPolicyDeny()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Deny)
                .Allow("*", "/admin", "ADMIN|PROD")
                .Deny("*", "/admin/Part2", "*")
                .Allow("*", "/admin/pArt2", "ADMin");

            // Access denied by default
            Assert.IsTrue(GuardPolicy.Deny == routeGuardian.Policy);
            // Access to a specific path granted to specific subjects
            Assert.IsTrue(!routeGuardian.IsGranted("GET", "/admin", "Client")
                           && routeGuardian.IsGranted("GET", "/admin", "admin")
                           && routeGuardian.IsGranted("GET", "/admin", "ProD"));
            // Access to a subpath granted to a specific subject (subpath precedence)
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/admin/part2", "Admin"));
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
                .Allow("*", "/admin*", "admin")
                .Allow("*", "/admin/part2", "ADMIN");

            // Wildcard suffix
            Assert.IsTrue(!routeGuardian.IsGranted("GET", "/admin")
                           && !routeGuardian.IsGranted("GET", "/admin/foo/bar")
                           && routeGuardian.IsGranted("GET", "/admin", "admin")
                           && routeGuardian.IsGranted("GET", "/admin/foo/bar", "ADMIN"));

            routeGuardian
                .Deny("*", "/*/edit", "*")
                .Allow("*", "/*/edit", "ADMIN");

            // Wildcard prefix
            Assert.IsTrue(!routeGuardian.IsGranted("GET", "/blog/entry/edit")
                           && routeGuardian.IsGranted("GET", "/blog/entry/edit", "admin"));

            routeGuardian
                .Allow("*", "/admin", "*")
                .Allow("*", "/admin/special/path", "*");

            // Wildcard precedence order
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/admin")
                           && !routeGuardian.IsGranted("GET", "/admin/foo/bar")
                           && routeGuardian.IsGranted("GET", "/Admin", "ADMIN")
                           && routeGuardian.IsGranted("GET", "/admin/foo/bar", "ADMin")
                           && routeGuardian.IsGranted("GET", "/admin/special/path")
                           && routeGuardian.IsGranted("GET", "/admin/special/Path", "admin"));

            routeGuardian
                .Clear()
                .Allow("*", "/products/{guid}", "*")
                .Allow("*", "/products/{guid}/load/{dec}", "*")
                .Allow("*", "/products/report/page/{int}", "*")
                .Allow("*", "/products/report/{str}", "*");

            Assert.IsTrue(routeGuardian.IsGranted("GET",
                              $"/products/{new Guid("017e2820-5171-405d-bada-3893a20bb479").ToString()}")
                          && !routeGuardian.IsGranted("GET", "/products/1234-D87914-99")
                          && routeGuardian.IsGranted("GET",
                              $"/products/{new Guid("017e2820-5171-405d-bada-3893a20bb479").ToString()}/load/12.1")
                          && routeGuardian.IsGranted("GET",
                              $"/products/{new Guid("017e2820-5171-405d-bada-3893a20bb479").ToString()}/load/-12.1")
                          && !routeGuardian.IsGranted("GET", "/products/1234-D87914-99/load/12.1")
                          && !routeGuardian.IsGranted("GET", 
                              $"/products/{new Guid("017e2820-5171-405d-bada-3893a20bb479").ToString()}/load/12x"));
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/products/report/page/12")
                          && routeGuardian.IsGranted("GET", "/products/report/page/-12")
                          && routeGuardian.IsGranted("GET", "/products/report/page/+12")
                          && !routeGuardian.IsGranted("GET", "/products/report/page/12.1")
                          && !routeGuardian.IsGranted("GET", "/products/report/page/12x"));
            Assert.IsTrue(routeGuardian.IsGranted("GET", "/products/report/Test-String_123")
                          && routeGuardian.IsGranted("GET", "/products/report/017e2820-5171-405d-bada-3893a20bb479")
                          && routeGuardian.IsGranted("GET", "/products/report/12345")
                          && !routeGuardian.IsGranted("GET", "/products/report/#12345$")
                          && !routeGuardian.IsGranted("GET", "/products/report/123.45"));
        }

        [TestMethod]
        public void WorksForVerbs()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("post|PuT|DELETE", "/blog/Entry", "*")
                .Allow("*", "/blog/entry", "ADMIN");

            Assert.IsTrue(routeGuardian.IsGranted("GET", "/blog/entry", "client")
                           && !routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT")
                           && routeGuardian.IsGranted("PUT", "/blog/entry", "admin"));
        }

        [TestMethod]
        public void WorksForMultipleSubjects()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("POST|PUT|delete", "/BloG/entry", "*")
                .Allow("*", "/blog/entry", "ADMIN");

            Assert.IsTrue(routeGuardian.IsGranted("GET", "/blog/entry", "Client|CUSTOMER")
                           && !routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT|customer")
                           && routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT|admin"));
        }

        [TestMethod]
        public void WorksWithAutorizeMethod()
        {
            var routeGuardian = new RouteGuardian()
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("Post|put|DELETE", "/blog/entry", "*")
                .Allow("*", "/blog/entry", "ADMIN");

            var context = new DefaultHttpContext
            {
                Request =
                {
                    Path = "/blog/Entry",
                    Method = "GET"
                }
            };

            // Authorize an unidentified subject (default policy = allow)
            Assert.IsTrue(routeGuardian.Authorize(context));

            context.Request.Method = "POST";

            // Unauthorize an unidentified subject
            Assert.IsFalse(routeGuardian.Authorize(context));
            // Authorize an identified subject
            Assert.IsTrue(routeGuardian.Authorize(context, "admin"));
            // Unauthorize an identified subject
            Assert.IsFalse(routeGuardian.Authorize(context, "Client"));
            // Authorize a set of identified subjects
            Assert.IsTrue(routeGuardian.Authorize(context, "CLIENT|Admin"));
            // Unauthorize a set of identified subjects'
            Assert.IsFalse(routeGuardian.Authorize(context, "client|Customer"));
        }
    }
}