# Test ASP.NET Core middleware

Middleware can be tested in isolation with [TestServer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver). It allows you to:

- Instantiate an app pipeline containing only the components that you need to test.
- Send custom requests to verify middleware behavior.

Advantages:

- Requests are sent in-memory rather than being serialized over the network.
- This avoids additional concerns, such as port management and HTTPS certificates.
- Exceptions in the middleware can flow directly back to the calling test.
- It's possible to customize server data structures, such as [HttpContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext), directly in the test.

## Set up the TestServer

In the test project, create a test:

- Build and start a host that uses [TestServer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver).

- Add any required services that the middleware uses.

- Add a package reference to the project for the [`Microsoft.AspNetCore.TestHost`](https://www.nuget.org/packages/Microsoft.AspNetCore.TestHost/) NuGet package.

- Configure the processing pipeline to use the middleware for the test.

  C#Copy

  ```csharp
  [Fact]
  public async Task MiddlewareTest_ReturnsNotFoundForRequest()
  {
      using var host = await new HostBuilder()
          .ConfigureWebHost(webBuilder =>
          {
              webBuilder
                  .UseTestServer()
                  .ConfigureServices(services =>
                  {
                      services.AddMyServices();
                  })
                  .Configure(app =>
                  {
                      app.UseMiddleware<MyMiddleware>();
                  });
          })
          .StartAsync();
  
      ...
  }
  ```

> :information_source: Note
> For guidance on adding packages to .NET apps, see the articles under *Install and manage packages* at [Package consumption workflow (NuGet documentation)](https://learn.microsoft.com/en-us/nuget/consume-packages/overview-and-workflow). Confirm correct package versions at [NuGet.org](https://www.nuget.org/).

## Send requests with HttpClient

Send a request using [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient):

```csharp
[Fact]
public async Task MiddlewareTest_ReturnsNotFoundForRequest()
{
    using var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddMyServices();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<MyMiddleware>();
                });
        })
        .StartAsync();

    var response = await host.GetTestClient().GetAsync("/");

    ...
}
```

Assert the result. First, make an assertion the opposite of the result that you expect. An initial run with a false positive assertion confirms that the test fails when the middleware is performing correctly. Run the test and confirm that the test fails.

In the following example, the middleware should return a 404 status code (*Not Found*) when the root endpoint is requested. Make the first test run with `Assert.NotEqual( ... );`, which should fail:

```csharp
[Fact]
public async Task MiddlewareTest_ReturnsNotFoundForRequest()
{
    using var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddMyServices();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<MyMiddleware>();
                });
        })
        .StartAsync();

    var response = await host.GetTestClient().GetAsync("/");

    Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
}
```

Change the assertion to test the middleware under normal operating conditions. The final test uses `Assert.Equal( ... );`. Run the test again to confirm that it passes.

```csharp
[Fact]
public async Task MiddlewareTest_ReturnsNotFoundForRequest()
{
    using var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddMyServices();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<MyMiddleware>();
                });
        })
        .StartAsync();

    var response = await host.GetTestClient().GetAsync("/");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

## Send requests with HttpContext

A test app can also send a request using [SendAsync(Action, CancellationToken)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver.sendasync). In the following example, several checks are made when `https://example.com/A/Path/?and=query` is processed by the middleware:

```csharp
[Fact]
public async Task TestMiddleware_ExpectedResponse()
{
    using var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddMyServices();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<MyMiddleware>();
                });
        })
        .StartAsync();

    var server = host.GetTestServer();
    server.BaseAddress = new Uri("https://example.com/A/Path/");

    var context = await server.SendAsync(c =>
    {
        c.Request.Method = HttpMethods.Post;
        c.Request.Path = "/and/file.txt";
        c.Request.QueryString = new QueryString("?and=query");
    });

    Assert.True(context.RequestAborted.CanBeCanceled);
    Assert.Equal(HttpProtocol.Http11, context.Request.Protocol);
    Assert.Equal("POST", context.Request.Method);
    Assert.Equal("https", context.Request.Scheme);
    Assert.Equal("example.com", context.Request.Host.Value);
    Assert.Equal("/A/Path", context.Request.PathBase.Value);
    Assert.Equal("/and/file.txt", context.Request.Path.Value);
    Assert.Equal("?and=query", context.Request.QueryString.Value);
    Assert.NotNull(context.Request.Body);
    Assert.NotNull(context.Request.Headers);
    Assert.NotNull(context.Response.Headers);
    Assert.NotNull(context.Response.Body);
    Assert.Equal(404, context.Response.StatusCode);
    Assert.Null(context.Features.Get<IHttpResponseFeature>().ReasonPhrase);
}
```

[SendAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver.sendasync) permits direct configuration of an [HttpContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext) object rather than using the [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) abstractions. Use [SendAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver.sendasync) to manipulate structures only available on the server, such as [HttpContext.Items](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.items#microsoft-aspnetcore-http-httpcontext-items) or [HttpContext.Features](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.features#microsoft-aspnetcore-http-httpcontext-features).

As with the earlier example that tested for a *404 - Not Found* response, check the opposite for each `Assert` statement in the preceding test. The check confirms that the test fails correctly when the middleware is operating normally. After you've confirmed that the false positive test works, set the final `Assert` statements for the expected conditions and values of the test. Run it again to confirm that the test passes.

## TestServer limitations

TestServer:

- Was created to replicate server behaviors to test middleware.
- Does ***not*** try to replicate all [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) behaviors.
- Attempts to give the client access to as much control over the server as possible, and with as much visibility into what's happening on the server as possible. For example it may throw exceptions not normally thrown by `HttpClient` in order to directly communicate server state.
- Doesn't set some transport specific headers by default as those aren't usually relevant to middleware. For more information, see the next section.
- Ignores the `Stream` position passed through [StreamContent](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.streamcontent). [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) sends the entire stream from the start position, even when positioning is set. For more information, see [this GitHub issue](https://github.com/dotnet/aspnetcore/issues/33780).

### Content-Length and Transfer-Encoding headers

TestServer does ***not*** set transport related request or response headers such as [Content-Length](https://developer.mozilla.org/docs/Web/HTTP/Headers/Content-Length) or [Transfer-Encoding](https://developer.mozilla.org/docs/Web/HTTP/Headers/Transfer-Encoding). Applications should avoid depending on these headers because their usage varies by client, scenario, and protocol. If `Content-Length` and `Transfer-Encoding` are necessary to test a specific scenario, they can be specified in the test when composing the [HttpRequestMessage](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage) or [HttpContext](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext). For more information, see the following GitHub issues:

- [dotnet/aspnetcore#21677](https://github.com/dotnet/aspnetcore/issues/21677)
- [dotnet/aspnetcore#18463](https://github.com/dotnet/aspnetcore/issues/18463)
- [dotnet/aspnetcore#13273](https://github.com/dotnet/aspnetcore/issues/13273)