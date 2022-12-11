using Microsoft.AspNetCore.Http;
using RouteGuardian.Model;

namespace RouteGuardian
{
    public class RouteGuardianTests
    {
        public void Run()
        {
            var routeGuardian = new RouteGuardian();

            // ----- Default policy: allow ------------------------------------

            routeGuardian
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("*", "/back", "*")
                .Allow("*", "/back", "ADMIN|PROD")
                .Deny("*", "/back/users", "PROD");

            var defaultPolicyAllow = GuardPolicy.Allow == routeGuardian.Policy;
            var grantedByDefault = routeGuardian.isGranted("GET", "/blog", "CLIENT");
            var specificPathDeniedToAll = !routeGuardian.isGranted("GET", "/back", "CLIENT");
            var pathGrantedToSpecificSubject = routeGuardian.isGranted("GET", "/back", "PROD");
            var subPathDeniedToSpecificSubject = !routeGuardian.isGranted("GET", "/back/users", "PROD");

            // ----- Default policy: deny -------------------------------------

            routeGuardian
                .Clear()
                .DefaultPolicy(GuardPolicy.Deny)
                .Allow("*", "/admin", "ADMIN|PROD")
                .Deny("*", "/admin/part2", "*")
                .Allow("*","/admin/part2", "ADMIN");
            
            var defaultPolicyDeny = GuardPolicy.Deny == routeGuardian.Policy;
            var adminPathGrantedToSpecificSubjects = 
                (!routeGuardian.isGranted("GET", "/admin", "CLIENT")
                && routeGuardian.isGranted("GET", "/admin", "ADMIN")
                && routeGuardian.isGranted("GET", "/admin", "PROD"));
            var subPathGrantedToSpecificSubject = routeGuardian.isGranted("GET", "/admin/part2", "ADMIN");
            var subPathDeniedToANONYMOUSubjects = !routeGuardian.isGranted("GET", "/admin/part2", "PROD");

            // ----- Wildcards ------------------------------------------------

            routeGuardian
                .Clear()
                .DefaultPolicy(GuardPolicy.Deny)
                .Deny("*", "/admin*", "*")
                .Allow("*", "/admin*", "ADMIN")
                .Allow("*", "/admin/part2", "ADMIN");

             var wildcardSuffix = 
                (!routeGuardian.isGranted("GET", "/admin")
                && !routeGuardian.isGranted("GET", "/admin/foo/bar")
                && routeGuardian.isGranted("GET", "/admin", "ADMIN")
                && routeGuardian.isGranted("GET", "/admin/foo/bar", "ADMIN"));

            routeGuardian
                .Deny("*", "/*/edit", "*")
                .Allow("*", "/*/edit", "ADMIN");

            var wildcardPrefix = 
                (!routeGuardian.isGranted("GET", "/blog/entry/edit")
                && routeGuardian.isGranted("GET", "/blog/entry/edit", "ADMIN"));

            routeGuardian
                .Allow("*", "/admin", "*")
                .Allow("*", "/admin/special/path", "*");

            var wildcardPrecedenceOrder =
                (routeGuardian.isGranted("GET", "/admin")
                && !routeGuardian.isGranted("GET", "/admin/foo/bar")
                && routeGuardian.isGranted("GET", "/admin", "ADMIN")
                && routeGuardian.isGranted("GET", "/admin/foo/bar", "ADMIN")
                && routeGuardian.isGranted("GET", "/admin/special/path")
                && routeGuardian.isGranted("GET", "/admin/special/path", "ADMIN"));

            // ----- Verbs ----------------------------------------------------

            routeGuardian
                .Clear()
                .DefaultPolicy(GuardPolicy.Allow)
                .Deny("POST|PUT|DELETE", "/blog/entry", "*")
                .Allow("*", "/blog/entry", "ADMIN");

            var verbLevelAccessControl = 
                (routeGuardian.isGranted("GET", "/blog/entry", "CLIENT")
                && !routeGuardian.isGranted("PUT", "/blog/entry", "CLIENT")
                && routeGuardian.isGranted("PUT", "/blog/entry", "ADMIN"));

            // ----- Multiple Subjects ----------------------------------------

            var accessForSetOfSubjects =
                (routeGuardian.isGranted("GET", "/blog/entry", "CLIENT|CUSTOMER")
                && !routeGuardian.isGranted("PUT", "/blog/entry", "CLIENT|CUSTOMER")
                && routeGuardian.isGranted("PUT", "/blog/entry", "CLIENT|ADMIN"));

            // ----- Authorize Method -----------------------------------------

            var context = new DefaultHttpContext
            {
                Request =
                {
                    Path = "/blog/entry",
                    Method = "GET"
                }
            };
            var authorizeAnonymous = routeGuardian.Authorize(context);

            context.Request.Method = "POST";
            var unauthorizeAnonymous = !routeGuardian.Authorize(context);
            var authorizeKnownSubject = routeGuardian.Authorize(context, "ADMIN");
            var unauthorizeKnownSubject = !routeGuardian.Authorize(context, "CLIENT");
            var authorizeSetOfKnowSubjects = routeGuardian.Authorize(context, "CLIENT|ADMIN");
            var unauthorizeSetOfKnowSubjects = !routeGuardian.Authorize(context, "CLIENT|CUSTOMER");
        }
    }
}
