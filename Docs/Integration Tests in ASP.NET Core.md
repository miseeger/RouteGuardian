# Integration tests in ASP.NET Core

Integration tests ensure that an app's components function correctly at a level that includes the app's supporting infrastructure, such as the database, file system, and network. ASP.NET Core supports integration tests using a unit test framework with a test web host and an in-memory test server.

This article assumes a basic understanding of unit tests. If unfamiliar with test concepts, see the [Unit Testing in .NET Core and .NET Standard](https://learn.microsoft.com/en-us/dotnet/core/testing/) article and its linked content.

[View or download sample code](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/test/integration-tests/IntegrationTestsSample) ([how to download](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core?view=aspnetcore-7.0#how-to-download-a-sample))

The sample app is a Razor Pages app and assumes a basic understanding of Razor Pages. If you're unfamiliar with Razor Pages, see the following articles:

- [Introduction to Razor Pages](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/?view=aspnetcore-7.0)
- [Get started with Razor Pages](https://learn.microsoft.com/en-us/aspnet/core/tutorials/razor-pages/razor-pages-start?view=aspnetcore-7.0)
- [Razor Pages unit tests](https://learn.microsoft.com/en-us/aspnet/core/test/razor-pages-tests?view=aspnetcore-7.0)

**For testing SPAs**, we recommend a tool such as [Playwright for .NET](https://playwright.dev/dotnet/), which can automate a browser.

## Introduction to integration tests

Integration tests evaluate an app's components on a broader level than [unit tests](https://learn.microsoft.com/en-us/dotnet/core/testing/). Unit tests are used to test isolated software components, such as individual class methods. Integration tests confirm that two or more app components work together to produce an expected result, possibly including every component required to fully process a request.

These broader tests are used to test the app's infrastructure and whole framework, often including the following components:

- Database
- File system
- Network appliances
- Request-response pipeline

Unit tests use fabricated components, known as *fakes* or *mock objects*, in place of infrastructure components.

In contrast to unit tests, integration tests:

- Use the actual components that the app uses in production.
- Require more code and data processing.
- Take longer to run.

Therefore, limit the use of integration tests to the most important infrastructure scenarios. If a behavior can be tested using either a unit test or an integration test, choose the unit test.

In discussions of integration tests, the tested project is frequently called the ***System Under Test***, or "**SUT**" for short. "SUT" is used throughout this article to refer to the ASP.NET Core app being tested.

***Don't write integration tests for every permutation*** of data and file access with databases and file systems. Regardless of how many places across an app interact with databases and file systems, a focused set of read, write, update, and delete integration tests are usually capable of adequately testing database and file system components. Use unit tests for routine tests of method logic that interact with these components. In unit tests, the use of infrastructure fakes or mocks result in faster test execution.

## ASP.NET Core integration tests

Integration tests in ASP.NET Core require the following:

- A test project is used to contain and execute the tests. The test project has a reference to the SUT.
- The test project creates a test web host for the SUT and uses a test server client to handle requests and responses with the SUT.
- A test runner is used to execute the tests and report the test results.

Integration tests follow a sequence of events that include the usual *Arrange*, *Act*, and *Assert* test steps:

1. The SUT's web host is configured.
2. A test server client is created to submit requests to the app.
3. The *Arrange* test step is executed: The test app prepares a request.
4. The *Act* test step is executed: The client submits the request and receives the response.
5. The *Assert* test step is executed: The *actual* response is validated as a *pass* or *fail* based on an *expected* response.
6. The process continues until all of the tests are executed.
7. The test results are reported.

Usually, the test web host is configured differently than the app's normal web host for the test runs. For example, a different database or different app settings might be used for the tests.

Infrastructure components, such as the test web host and in-memory test server ([TestServer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver)), are provided or managed by the [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) package. Use of this package streamlines test creation and execution.

The `Microsoft.AspNetCore.Mvc.Testing` package handles the following tasks:

- Copies the dependencies file (`.deps`) from the SUT into the test project's `bin` directory.
- Sets the [content root](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-7.0#content-root) to the SUT's project root so that static files and pages/views are found when the tests are executed.
- Provides the [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1) class to streamline bootstrapping the SUT with `TestServer`.

The [unit tests](https://learn.microsoft.com/en-us/dotnet/articles/core/testing/unit-testing-with-dotnet-test) documentation describes how to set up a test project and test runner, along with detailed instructions on how to run tests and recommendations for how to name tests and test classes.

**Separate unit tests from integration tests into different projects**. Separating the tests:

- Helps ensure that infrastructure testing components aren't accidentally included in the unit tests.
- Allows control over which set of tests are run.

There's virtually no difference between the configuration for tests of Razor Pages apps and MVC apps. The only difference is in how the tests are named. In a Razor Pages app, tests of page endpoints are usually named after the page model class (for example, `IndexPageTests` to test component integration for the Index page). In an MVC app, tests are usually organized by controller classes and named after the controllers they test (for example, `HomeControllerTests` to test component integration for the Home controller).

## Test app prerequisites

The test project must:

- Reference the [`Microsoft.AspNetCore.Mvc.Testing`](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) package.
- Specify the Web SDK in the project file (`<Project Sdk="Microsoft.NET.Sdk.Web">`).

These prerequisites can be seen in the [sample app](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/test/integration-tests/IntegrationTestsSample). Inspect the `tests/RazorPagesProject.Tests/RazorPagesProject.Tests.csproj` file. The sample app uses the [xUnit](https://xunit.net/) test framework and the [AngleSharp](https://anglesharp.github.io/) parser library, so the sample app also references:

- [`AngleSharp`](https://www.nuget.org/packages/AngleSharp)
- [`xunit`](https://www.nuget.org/packages/xunit)
- [`xunit.runner.visualstudio`](https://www.nuget.org/packages/xunit.runner.visualstudio)

In apps that use [`xunit.runner.visualstudio`](https://www.nuget.org/packages/xunit.runner.visualstudio) version 2.4.2 or later, the test project must reference the [`Microsoft.NET.Test.Sdk`](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk) package.

Entity Framework Core is also used in the tests. See the [project file in GitHub](https://github.com/dotnet/AspNetCore.Docs.Samples/blob/main/test/integration-tests/IntegrationTestsSample/src/RazorPagesProject/RazorPagesProject.csproj).

## SUT environment

If the SUT's [environment](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-7.0) isn't set, the environment defaults to Development.

## Basic tests with the default WebApplicationFactory

Expose the implicitly defined `Program` class to the test project by doing one of the following:

- Expose internal types from the web app to the test project. This can be done in the SUT project's file (`.csproj`):

  XMLCopy

  ```xml
  <ItemGroup>
       <InternalsVisibleTo Include="MyTestProject" />
  </ItemGroup>
  ```

- Make the [`Program` class public using a partial class](https://github.com/dotnet/AspNetCore.Docs.Samples/blob/main/test/integration-tests/IntegrationTestsSample/src/RazorPagesProject/Program.cs) declaration:

  diffCopy

  ```diff
  var builder = WebApplication.CreateBuilder(args);
  // ... Configure services, routes, etc.
  app.Run();
  + public partial class Program { }
  ```

  C#Copy

  ```csharp
  public class BasicTests 
      : IClassFixture<WebApplicationFactory<Program>>
  {
      private readonly WebApplicationFactory<Program> _factory;
  
      public BasicTests(WebApplicationFactory<Program> factory)
      {
          _factory = factory;
      }
  
      [Theory]
      [InlineData("/")]
      [InlineData("/Index")]
      [InlineData("/About")]
      [InlineData("/Privacy")]
      [InlineData("/Contact")]
      public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
      {
          // Arrange
          var client = _factory.CreateClient();
  
          // Act
          var response = await client.GetAsync(url);
  
          // Assert
          response.EnsureSuccessStatusCode(); // Status Code 200-299
          Assert.Equal("text/html; charset=utf-8", 
              response.Content.Headers.ContentType.ToString());
      }
  }
  ```

  The [sample app](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/test/integration-tests/IntegrationTestsSample) uses the `Program` partial class approach.

[WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1) is used to create a [TestServer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver) for the integration tests. `TEntryPoint` is the entry point class of the SUT, usually `Program.cs`.

Test classes implement a *class fixture* interface ([`IClassFixture`](https://xunit.net/docs/shared-context#class-fixture)) to indicate the class contains tests and provide shared object instances across the tests in the class.

The following test class, `BasicTests`, uses the `WebApplicationFactory` to bootstrap the SUT and provide an [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) to a test method, `Get_EndpointsReturnSuccessAndCorrectContentType`. The method verifies the response status code is successful (200-299) and the `Content-Type` header is `text/html; charset=utf-8` for several app pages.

[CreateClient()](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.createclient#microsoft-aspnetcore-mvc-testing-webapplicationfactory-1-createclient) creates an instance of `HttpClient` that automatically follows redirects and handles cookies.

```csharp
public class BasicTests 
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Index")]
    [InlineData("/About")]
    [InlineData("/Privacy")]
    [InlineData("/Contact")]
    public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("text/html; charset=utf-8", 
            response.Content.Headers.ContentType.ToString());
    }
}
```

By default, non-essential cookies aren't preserved across requests when the [General Data Protection Regulation consent policy](https://learn.microsoft.com/en-us/aspnet/core/security/gdpr?view=aspnetcore-7.0) is enabled. To preserve non-essential cookies, such as those used by the TempData provider, mark them as essential in your tests. For instructions on marking a cookie as essential, see [Essential cookies](https://learn.microsoft.com/en-us/aspnet/core/security/gdpr?view=aspnetcore-7.0#essential-cookies).

## AngleSharp vs `Application Parts` for antiforgery checks

This article uses the [AngleSharp](https://anglesharp.github.io/) parser to handle the antiforgery checks by loading pages and parsing the HTML. For testing the endpoints of controller and Razor Pages views at a lower-level, without caring about how they render in the browser, consider using `Application Parts`. The [Application Parts](https://learn.microsoft.com/en-us/aspnet/core/mvc/advanced/app-parts?view=aspnetcore-7.0) approach injects a controller or Razor Page into the app that can be used to make JSON requests to get the required values. For more information, see the blog [Integration Testing ASP.NET Core Resources Protected with Antiforgery Using Application Parts](https://blog.martincostello.com/integration-testing-antiforgery-with-application-parts/) and [associated GitHub repo](https://github.com/martincostello/antiforgery-testing-application-part) by [Martin Costello](https://github.com/martincostello).

## Customize WebApplicationFactory

Web host configuration can be created independently of the test classes by inheriting from [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1) to create one or more custom factories:

1. Inherit from `WebApplicationFactory` and override [ConfigureWebHost](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.configurewebhost). The [IWebHostBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.iwebhostbuilder) allows the configuration of the service collection with [`IWebHostBuilder.ConfigureServices`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.iwebhostbuilder.configureservices)

   ```csharp
   public class CustomWebApplicationFactory<TProgram>
       : WebApplicationFactory<TProgram> where TProgram : class
   {
       protected override void ConfigureWebHost(IWebHostBuilder builder)
       {
           builder.ConfigureServices(services =>
           {
               var dbContextDescriptor = services.SingleOrDefault(
                   d => d.ServiceType ==
                       typeof(DbContextOptions<ApplicationDbContext>));
   
               services.Remove(dbContextDescriptor);
   
               var dbConnectionDescriptor = services.SingleOrDefault(
                   d => d.ServiceType ==
                       typeof(DbConnection));
   
               services.Remove(dbConnectionDescriptor);
   
               // Create open SqliteConnection so EF won't automatically close it.
               services.AddSingleton<DbConnection>(container =>
               {
                   var connection = new SqliteConnection("DataSource=:memory:");
                   connection.Open();
   
                   return connection;
               });
   
               services.AddDbContext<ApplicationDbContext>((container, options) =>
               {
                   var connection = container.GetRequiredService<DbConnection>();
                   options.UseSqlite(connection);
               });
           });
   
           builder.UseEnvironment("Development");
       }
   }
   ```

   Database seeding in the [sample app](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/test/integration-tests/IntegrationTestsSample) is performed by the `InitializeDbForTests` method. The method is described in the [Integration tests sample: Test app organization](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#test-app-organization) section.

   The SUT's database context is registered in `Program.cs`. The test app's `builder.ConfigureServices` callback is executed *after* the app's `Program.cs` code is executed. To use a different database for the tests than the app's database, the app's database context must be replaced in `builder.ConfigureServices`.

   The sample app finds the service descriptor for the database context and uses the descriptor to remove the service registration. The factory then adds a new `ApplicationDbContext` that uses an in-memory database for the tests..

   To connect to a different database than the in-memory database, change the `UseInMemoryDatabase` call to connect the context to a different database. To use a SQL Server test database:

   - Reference the [`Microsoft.EntityFrameworkCore.SqlServer`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/) NuGet package in the project file.
   - Call `UseInMemoryDatabase`:

   ```csharp
   public class CustomWebApplicationFactory<TProgram>
       : WebApplicationFactory<TProgram> where TProgram : class
   {
       protected override void ConfigureWebHost(IWebHostBuilder builder)
       {
           builder.ConfigureServices(services =>
           {
               var dbContextDescriptor = services.SingleOrDefault(
                   d => d.ServiceType ==
                       typeof(DbContextOptions<ApplicationDbContext>));
   
               services.Remove(dbContextDescriptor);
   
               var dbConnectionDescriptor = services.SingleOrDefault(
                   d => d.ServiceType ==
                       typeof(DbConnection));
   
               services.Remove(dbConnectionDescriptor);
   
               // Create open SqliteConnection so EF won't automatically close it.
               services.AddSingleton<DbConnection>(container =>
               {
                   var connection = new SqliteConnection("DataSource=:memory:");
                   connection.Open();
   
                   return connection;
               });
   
               services.AddDbContext<ApplicationDbContext>((container, options) =>
               {
                   var connection = container.GetRequiredService<DbConnection>();
                   options.UseSqlite(connection);
               });
           });
   
           builder.UseEnvironment("Development");
       }
   }
   ```

2. Use the custom `CustomWebApplicationFactory` in test classes. The following example uses the factory in the `IndexPageTests` class:

   ```csharp
   public class IndexPageTests :
       IClassFixture<CustomWebApplicationFactory<Program>>
   {
       private readonly HttpClient _client;
       private readonly CustomWebApplicationFactory<Program>
           _factory;
   
       public IndexPageTests(
           CustomWebApplicationFactory<Program> factory)
       {
           _factory = factory;
           _client = factory.CreateClient(new WebApplicationFactoryClientOptions
           {
               AllowAutoRedirect = false
           });
       }
   ```

   The sample app's client is configured to prevent the `HttpClient` from following redirects. As explained later in the [Mock authentication](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#mock-authentication) section, this permits tests to check the result of the app's first response. The first response is a redirect in many of these tests with a `Location` header.

3. A typical test uses the `HttpClient` and helper methods to process the request and the response:

   ```csharp
   [Fact]
   public async Task Post_DeleteAllMessagesHandler_ReturnsRedirectToRoot()
   {
       // Arrange
       var defaultPage = await _client.GetAsync("/");
       var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
   
       //Act
       var response = await _client.SendAsync(
           (IHtmlFormElement)content.QuerySelector("form[id='messages']"),
           (IHtmlButtonElement)content.QuerySelector("button[id='deleteAllBtn']"));
   
       // Assert
       Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
       Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
       Assert.Equal("/", response.Headers.Location.OriginalString);
   }
   ```

Any POST request to the SUT must satisfy the antiforgery check that's automatically made by the app's [data protection antiforgery system](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction?view=aspnetcore-7.0). In order to arrange for a test's POST request, the test app must:

1. Make a request for the page.
2. Parse the antiforgery cookie and request validation token from the response.
3. Make the POST request with the antiforgery cookie and request validation token in place.

The `SendAsync` helper extension methods (`Helpers/HttpClientExtensions.cs`) and the `GetDocumentAsync` helper method (`Helpers/HtmlHelpers.cs`) in the [sample app](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/test/integration-tests/IntegrationTestsSample/) use the [AngleSharp](https://anglesharp.github.io/) parser to handle the antiforgery check with the following methods:

- `GetDocumentAsync`: Receives the [HttpResponseMessage](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage) and returns an `IHtmlDocument`. `GetDocumentAsync` uses a factory that prepares a *virtual response* based on the original `HttpResponseMessage`. For more information, see the [AngleSharp documentation](https://github.com/AngleSharp/AngleSharp#documentation).
- `SendAsync` extension methods for the `HttpClient` compose an `HttpRequestMessage` and call `SendAsync(HttpRequestMessage)` to submit requests to the SUT. Overloads for `SendAsync` accept the HTML form (`IHtmlFormElement`) and the following:
  - Submit button of the form (`IHtmlElement`)
  - Form values collection (`IEnumerable<KeyValuePair<string, string>>`)
  - Submit button (`IHtmlElement`) and form values (`IEnumerable<KeyValuePair<string, string>>`)

[AngleSharp](https://anglesharp.github.io/) is a **third-party parsing library used for demonstration purposes** in this article and the sample app. AngleSharp isn't supported or required for integration testing of ASP.NET Core apps. Other parsers can be used, such as the [Html Agility Pack (HAP)](https://html-agility-pack.net/). Another approach is to write code to handle the antiforgery system's request verification token and antiforgery cookie directly. See [AngleSharp vs `Application Parts` for antiforgery checks](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#asap7) in this article for more information.

The [EF-Core in-memory database provider](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy#in-memory-as-a-database-fake) can be used for limited and basic testing, however the ***[SQLite provider](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy#sqlite-as-a-database-fake) is the recommended choice for in-memory testing***.

## Customize the client with WithWebHostBuilder

When additional configuration is required within a test method, [WithWebHostBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.withwebhostbuilder) creates a new `WebApplicationFactory` with an [IWebHostBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.iwebhostbuilder) that is further customized by configuration.

The `Post_DeleteMessageHandler_ReturnsRedirectToRoot` test method of the [sample app](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/test/integration-tests/IntegrationTestsSample) demonstrates the use of `WithWebHostBuilder`. This test performs a record delete in the database by triggering a form submission in the SUT.

Because another test in the `IndexPageTests` class performs an operation that deletes all of the records in the database and may run before the `Post_DeleteMessageHandler_ReturnsRedirectToRoot` method, the database is reseeded in this test method to ensure that a record is present for the SUT to delete. Selecting the first delete button of the `messages` form in the SUT is simulated in the request to the SUT:

```csharp
[Fact]
public async Task Post_DeleteMessageHandler_ReturnsRedirectToRoot()
{
    // Arrange
    using (var scope = _factory.Services.CreateScope())
    {
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<ApplicationDbContext>();

        Utilities.ReinitializeDbForTests(db);
    }

    var defaultPage = await _client.GetAsync("/");
    var content = await HtmlHelpers.GetDocumentAsync(defaultPage);

    //Act
    var response = await _client.SendAsync(
        (IHtmlFormElement)content.QuerySelector("form[id='messages']"),
        (IHtmlButtonElement)content.QuerySelector("form[id='messages']")
            .QuerySelector("div[class='panel-body']")
            .QuerySelector("button"));

    // Assert
    Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Equal("/", response.Headers.Location.OriginalString);
}
```

## Client options

See the [WebApplicationFactoryClientOptions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactoryclientoptions) page for defaults and available options when creating `HttpClient` instances.

Create the `WebApplicationFactoryClientOptions` class and pass it to the [CreateClient()](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.createclient#microsoft-aspnetcore-mvc-testing-webapplicationfactory-1-createclient) method:

```csharp
public class IndexPageTests :
    IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program>
        _factory;

    public IndexPageTests(
        CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
```

## Inject mock services

Services can be overridden in a test with a call to [ConfigureTestServices](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.webhostbuilderextensions.configuretestservices) on the host builder.

The sample SUT includes a scoped service that returns a quote. The quote is embedded in a hidden field on the Index page when the Index page is requested.

`Services/IQuoteService.cs`:

```csharp
public interface IQuoteService
{
    Task<string> GenerateQuote();
}
```

`Services/QuoteService.cs`:

```csharp
// Quote ©1975 BBC: The Doctor (Tom Baker); Dr. Who: Planet of Evil
// https://www.bbc.co.uk/programmes/p00pyrx6
public class QuoteService : IQuoteService
{
    public Task<string> GenerateQuote()
    {
        return Task.FromResult<string>(
            "Come on, Sarah. We've an appointment in London, " +
            "and we're already 30,000 years late.");
    }
}
```

`Program.cs`:

```csharp
services.AddScoped<IQuoteService, QuoteService>();
```

`Pages/Index.cshtml.cs`:

```csharp
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IQuoteService _quoteService;

    public IndexModel(ApplicationDbContext db, IQuoteService quoteService)
    {
        _db = db;
        _quoteService = quoteService;
    }

    [BindProperty]
    public Message Message { get; set; }

    public IList<Message> Messages { get; private set; }

    [TempData]
    public string MessageAnalysisResult { get; set; }

    public string Quote { get; private set; }

    public async Task OnGetAsync()
    {
        Messages = await _db.GetMessagesAsync();

        Quote = await _quoteService.GenerateQuote();
    }
```

`Pages/Index.cs`:

```cshtml
<input id="quote" type="hidden" value="@Model.Quote">
```

The following markup is generated when the SUT app is run:

```html
<input id="quote" type="hidden" value="Come on, Sarah. We&#x27;ve an appointment in 
    London, and we&#x27;re already 30,000 years late.">
```

To test the service and quote injection in an integration test, a mock service is injected into the SUT by the test. The mock service replaces the app's `QuoteService` with a service provided by the test app, called `TestQuoteService`:

`IntegrationTests.IndexPageTests.cs`:

```csharp
// Quote ©1975 BBC: The Doctor (Tom Baker); Pyramids of Mars
// https://www.bbc.co.uk/programmes/p00pys55
public class TestQuoteService : IQuoteService
{
    public Task<string> GenerateQuote()
    {
        return Task.FromResult(
            "Something's interfering with time, Mr. Scarman, " +
            "and time is my business.");
    }
}
```

`ConfigureTestServices` is called, and the scoped service is registered:

```csharp
[Fact]
public async Task Get_QuoteService_ProvidesQuoteInPage()
{
    // Arrange
    var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IQuoteService, TestQuoteService>();
            });
        })
        .CreateClient();

    //Act
    var defaultPage = await client.GetAsync("/");
    var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
    var quoteElement = content.QuerySelector("#quote");

    // Assert
    Assert.Equal("Something's interfering with time, Mr. Scarman, " +
        "and time is my business.", quoteElement.Attributes["value"].Value);
}
```

The markup produced during the test's execution reflects the quote text supplied by `TestQuoteService`, thus the assertion passes:

```html
<input id="quote" type="hidden" value="Something&#x27;s interfering with time, 
    Mr. Scarman, and time is my business.">
```

## Mock authentication

Tests in the `AuthTests` class check that a secure endpoint:

- Redirects an unauthenticated user to the app's sign in page.
- Returns content for an authenticated user.

In the SUT, the `/SecurePage` page uses an [AuthorizePage](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.pageconventioncollectionextensions.authorizepage) convention to apply an [AuthorizeFilter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.authorization.authorizefilter) to the page. For more information, see [Razor Pages authorization conventions](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/razor-pages-authorization?view=aspnetcore-7.0#require-authorization-to-access-a-page).

```csharp
services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/SecurePage");
});
```

In the `Get_SecurePageRedirectsAnUnauthenticatedUser` test, a [WebApplicationFactoryClientOptions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactoryclientoptions) is set to disallow redirects by setting [AllowAutoRedirect](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactoryclientoptions.allowautoredirect#microsoft-aspnetcore-mvc-testing-webapplicationfactoryclientoptions-allowautoredirect) to `false`:

```csharp
[Fact]
public async Task Get_SecurePageRedirectsAnUnauthenticatedUser()
{
    // Arrange
    var client = _factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    // Act
    var response = await client.GetAsync("/SecurePage");

    // Assert
    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.StartsWith("http://localhost/Identity/Account/Login",
        response.Headers.Location.OriginalString);
}
```

By disallowing the client to follow the redirect, the following checks can be made:

- The status code returned by the SUT can be checked against the expected [HttpStatusCode.Redirect](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode#system-net-httpstatuscode-redirect) result, not the final status code after the redirect to the sign in page, which would be [HttpStatusCode.OK](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode#system-net-httpstatuscode-ok).
- The `Location` header value in the response headers is checked to confirm that it starts with `http://localhost/Identity/Account/Login`, not the final sign in page response, where the `Location` header wouldn't be present.

The test app can mock an [AuthenticationHandler](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1) in [ConfigureTestServices](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.webhostbuilderextensions.configuretestservices) in order to test aspects of authentication and authorization. A minimal scenario returns an [AuthenticateResult.Success](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticateresult.success):

```csharp
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "Test user") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}
```

The `TestAuthHandler` is called to authenticate a user when the authentication scheme is set to `TestScheme` where `AddAuthentication` is registered for `ConfigureTestServices`. It's important for the `TestScheme` scheme to match the scheme your app expects. Otherwise, authentication won't work.

```csharp
[Fact]
public async Task Get_SecurePageIsReturnedForAnAuthenticatedUser()
{
    // Arrange
    var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "TestScheme", options => { });
            });
        })
        .CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(scheme: "TestScheme");

    //Act
    var response = await client.GetAsync("/SecurePage");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

For more information on `WebApplicationFactoryClientOptions`, see the [Client options](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#client-options) section.

### Basic tests for authentication middleware

See [this GitHub repository](https://github.com/blowdart/idunno.Authentication/tree/dev/test/idunno.Authentication.Basic.Test) for basic tests of authentication middleware. It contains a [test server](https://github.com/blowdart/idunno.Authentication/blob/dev/test/idunno.Authentication.Basic.Test/BasicAuthenticationTests.cs#L331) that’s specific to the test scenario.

## Set the environment

Set the [environment](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-7.0) in the custom application factory:

```csharp
public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbContextOptions<ApplicationDbContext>));

            services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbConnection));

            services.Remove(dbConnectionDescriptor);

            // Create open SqliteConnection so EF won't automatically close it.
            services.AddSingleton<DbConnection>(container =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                return connection;
            });

            services.AddDbContext<ApplicationDbContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
        });

        builder.UseEnvironment("Development");
    }
}
```

## How the test infrastructure infers the app content root path

The `WebApplicationFactory` constructor infers the app [content root](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-7.0#content-root) path by searching for a [WebApplicationFactoryContentRootAttribute](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactorycontentrootattribute) on the assembly containing the integration tests with a key equal to the `TEntryPoint` assembly `System.Reflection.Assembly.FullName`. In case an attribute with the correct key isn't found, `WebApplicationFactory` falls back to searching for a solution file (*.sln*) and appends the `TEntryPoint` assembly name to the solution directory. The app root directory (the content root path) is used to discover views and content files.

## Disable shadow copying

Shadow copying causes the tests to execute in a different directory than the output directory. If your tests rely on loading files relative to `Assembly.Location` and you encounter issues, you might have to disable shadow copying.

To disable shadow copying when using xUnit, create a `xunit.runner.json` file in your test project directory, with the [correct configuration setting](https://xunit.net/docs/configuration-files#shadowCopy):

```json
{
  "shadowCopy": false
}
```

## Disposal of objects

After the tests of the `IClassFixture` implementation are executed, [TestServer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver) and [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) are disposed when xUnit disposes of the [`WebApplicationFactory`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1). If objects instantiated by the developer require disposal, dispose of them in the `IClassFixture` implementation. For more information, see [Implementing a Dispose method](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose).

## Integration tests sample

The [sample app](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/test/integration-tests/IntegrationTestsSample) is composed of two apps:

| App                   | Project directory               | Description                                                  |
| :-------------------- | :------------------------------ | :----------------------------------------------------------- |
| Message app (the SUT) | `src/RazorPagesProject`         | Allows a user to add, delete one, delete all, and analyze messages. |
| Test app              | `tests/RazorPagesProject.Tests` | Used to integration test the SUT.                            |

The tests can be run using the built-in test features of an IDE, such as [Visual Studio](https://visualstudio.microsoft.com/). If using [Visual Studio Code](https://code.visualstudio.com/) or the command line, execute the following command at a command prompt in the `tests/RazorPagesProject.Tests` directory:

```console
dotnet test
```

### Message app (SUT) organization

The SUT is a Razor Pages message system with the following characteristics:

- The Index page of the app (`Pages/Index.cshtml` and `Pages/Index.cshtml.cs`) provides a UI and page model methods to control the addition, deletion, and analysis of messages (average words per message).
- A message is described by the `Message` class (`Data/Message.cs`) with two properties: `Id` (key) and `Text` (message). The `Text` property is required and limited to 200 characters.
- Messages are stored using [Entity Framework's in-memory database](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)†.
- The app contains a data access layer (DAL) in its database context class, `AppDbContext` (`Data/AppDbContext.cs`).
- If the database is empty on app startup, the message store is initialized with three messages.
- The app includes a `/SecurePage` that can only be accessed by an authenticated user.

†The EF article, [Test with InMemory](https://learn.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory), explains how to use an in-memory database for tests with MSTest. This topic uses the [xUnit](https://xunit.net/) test framework. Test concepts and test implementations across different test frameworks are similar but not identical.

Although the app doesn't use the repository pattern and isn't an effective example of the [Unit of Work (UoW) pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html), Razor Pages supports these patterns of development. For more information, see [Designing the infrastructure persistence layer](https://learn.microsoft.com/en-us/dotnet/standard/microservices-architecture/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design) and [Test controller logic](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/testing?view=aspnetcore-7.0) (the sample implements the repository pattern).

### Test app organization

The test app is a console app inside the `tests/RazorPagesProject.Tests` directory.

| Test app directory  | Description                                                  |
| :------------------ | :----------------------------------------------------------- |
| `AuthTests`         | Contains test methods for:Accessing a secure page by an unauthenticated user.Accessing a secure page by an authenticated user with a mock [AuthenticationHandler](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1).Obtaining a GitHub user profile and checking the profile's user login. |
| `BasicTests`        | Contains a test method for routing and content type.         |
| `IntegrationTests`  | Contains the integration tests for the Index page using custom `WebApplicationFactory` class. |
| `Helpers/Utilities` | `Utilities.cs` contains the `InitializeDbForTests` method used to seed the database with test data.`HtmlHelpers.cs` provides a method to return an AngleSharp `IHtmlDocument` for use by the test methods.`HttpClientExtensions.cs` provide overloads for `SendAsync` to submit requests to the SUT. |

The test framework is [xUnit](https://xunit.net/). Integration tests are conducted using the [Microsoft.AspNetCore.TestHost](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost), which includes the [TestServer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver). Because the [`Microsoft.AspNetCore.Mvc.Testing`](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) package is used to configure the test host and test server, the `TestHost` and `TestServer` packages don't require direct package references in the test app's project file or developer configuration in the test app.

Integration tests usually require a small dataset in the database prior to the test execution. For example, a delete test calls for a database record deletion, so the database must have at least one record for the delete request to succeed.

The sample app seeds the database with three messages in `Utilities.cs` that tests can use when they execute:

```csharp
public static void InitializeDbForTests(ApplicationDbContext db)
{
    db.Messages.AddRange(GetSeedingMessages());
    db.SaveChanges();
}

public static void ReinitializeDbForTests(ApplicationDbContext db)
{
    db.Messages.RemoveRange(db.Messages);
    InitializeDbForTests(db);
}

public static List<Message> GetSeedingMessages()
{
    return new List<Message>()
    {
        new Message(){ Text = "TEST RECORD: You're standing on my scarf." },
        new Message(){ Text = "TEST RECORD: Would you like a jelly baby?" },
        new Message(){ Text = "TEST RECORD: To the rational mind, " +
            "nothing is inexplicable; only unexplained." }
    };
}
```

The SUT's database context is registered in `Program.cs`. The test app's `builder.ConfigureServices` callback is executed *after* the app's `Program.cs` code is executed. To use a different database for the tests, the app's database context must be replaced in `builder.ConfigureServices`. For more information, see the [Customize WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#customize-webapplicationfactory) section.

## Additional resources

- [Unit tests](https://learn.microsoft.com/en-us/dotnet/articles/core/testing/unit-testing-with-dotnet-test)
- [Razor Pages unit tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/razor-pages-tests?view=aspnetcore-7.0)
- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-7.0)
- [Test controller logic in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/testing?view=aspnetcore-7.0)
- [Basic tests for authentication middleware](https://github.com/blowdart/idunno.Authentication/tree/dev/test/idunno.Authentication.Basic.Test)