# [Set User identity and IsAuthenticated in ASP.NET MVC Core controller tests](https://hudosvibe.net/post/mock-user-identity-in-asp.net-mvc-core-controller-tests)

Small reminder how to fake **User.Identity.IsAuthenticated** and **User.Identity.Name** inside unit tests, while testing controller code. It’s not so obvious and I often forget this small trick, which is also important when writing user authentication code (login).

Our simple controller returns a model, where one property depends on authenticated user state:

```c
public IActionResult Index()
{
    var model = new HomePageViewModel
    {
        Title = "Welcome!",
        DisplayMode = StoriesDisplayMode.FeaturedList,
        CanWriteStory = User.Identity.IsAuthenticated
    };
    return View(model);
} 
```

In unit test we want to validate model properties, specially CanWriteStory which has to be True if user is authenticated.

Simple unit test for this controller:

```c
public void User_can_write_story_when_authenticated()
{
    var controller = new HomeController();
    controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "username")
            }, "someAuthTypeName"))
        }
    };


    var actionResult = controller.Index();


    var viewResult = Assert.IsType<ViewResult>(actionResult);
    var model = Assert.IsType<HomePageViewModel>(viewResult.Model);
    Assert.True(model.CanWriteStory);
}
```

This test will pass (User.Identity.IsAuthenticated will be True) because we configured **authentication type** “someAuthTypeName” ([here](https://github.com/Microsoft/referencesource/blob/master/mscorlib/system/security/claims/ClaimsIdentity.cs#L271) and [here](https://github.com/Microsoft/referencesource/blob/master/mscorlib/system/security/claims/ClaimsIdentity.cs#L461))! Without looking at MVC source code it would take me a long time to figure that out, and to be honest correlation between IsAuthenticated and AuthenticationType is still little bit confusing. 

The test also shows how simple it is to set HttpContext to controller, without need of using some mocking framework. If I’m brave enough, I would try to write similar test for WebForms and Page class, but I’m not![Smile](SetUserIdentityAndIsAuthenticated.assets/41ec8d5f-3a66-4ac9-8b1a-5c8175bed41b.png)

Same rule, with setting of authentication type applies when writing custom login code, which looks probably something like this:

> var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.Result.Username) };
>
> await _httpContext.HttpContext.Authentication.SignInAsync(Application.AuthScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "form")));

if we forget to set auth type “form” (can be whatever), user will not be authenticated!