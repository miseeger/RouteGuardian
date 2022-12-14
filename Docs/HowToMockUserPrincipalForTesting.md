The controller’s [`User`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.user?view=aspnetcore-2.2) [is accessed](https://github.com/aspnet/AspNetCore/blob/v2.2.5/src/Mvc/Mvc.Core/src/ControllerBase.cs#L196) through the [`HttpContext` of the controller](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.httpcontext?view=aspnetcore-2.2). The latter [is stored](https://github.com/aspnet/AspNetCore/blob/v2.2.5/src/Mvc/Mvc.Core/src/ControllerBase.cs#L39) within the [`ControllerContext`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllercontext?view=aspnetcore-2.2).

The easiest way to set the user is by assigning a different HttpContext with a constructed user. We can use [`DefaultHttpContext`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.defaulthttpcontext?view=aspnetcore-2.2) for this purpose, that way we don’t have to mock everything. Then we just use that HttpContext within a controller context and pass that to the controller instance:

```cs
var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
{
    new Claim(ClaimTypes.Name, "example name"),
    new Claim(ClaimTypes.NameIdentifier, "1"),
    new Claim("custom-claim", "example claim value"),
}, "mock"));

var controller = new SomeController(dependencies…);
controller.ControllerContext = new ControllerContext()
{
    HttpContext = new DefaultHttpContext() { User = user }
};
```

When creating your own [`ClaimsIdentity`](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimsidentity?view=netstandard-2.0), make sure to pass an explicit [`authenticationType`](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimsidentity.authenticationtype?view=netstandard-2.0) to the constructor. This makes sure that [`IsAuthenticated`](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimsidentity.isauthenticated?view=netstandard-2.0) will work correctly (in case you use that in your code to determine whether a user is authenticated).

If you're using Razor pages and want to override the claims:

```cs
[SetUp]
public void Setup()
{
    var user = new ClaimsPrincipal(new ClaimsIdentity(
    new Claim[] { 
        new("dateofbirth", "2000-10-10"),
        new("surname", "Smith") },
    "mock"));

    _razorModel = new RazorModel()
    {
        PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext() { User = user }
        }
    };
}
```

Another Solution when actually mocking the Identiy:

```cs
var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, "John Doe"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("name", "John Doe"),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockPrincipal = new Mock<IPrincipal>();
            mockPrincipal.Setup(x => x.Identity).Returns(identity);
            mockPrincipal.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(m => m.User).Returns(claimsPrincipal);
```

The `User.Identity.Name` is set properly now, but the line below still returns a `user = null`