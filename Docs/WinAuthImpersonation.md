## WinAuth Impersonation

ASP.NET Core doesn't implement impersonation. Apps run with the app's identity for all requests, using app pool or process identity. If the app should perform an action on behalf of a user, use [WindowsIdentity.RunImpersonated](https://learn.microsoft.com/en-us/dotnet/api/system.security.principal.windowsidentity.runimpersonated) or [RunImpersonatedAsync](https://learn.microsoft.com/en-us/dotnet/api/system.security.principal.windowsidentity.runimpersonatedasync) in a [terminal inline middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-6.0#create-a-middleware-pipeline-with-iapplicationbuilder) in `Startup.Configure`. Run a single action in this context and then close the context.

```csharp
app.Run(async (context) =>
{
    try
    {
        var user = (WindowsIdentity)context.User.Identity!;

        await context.Response
            .WriteAsync($"User: {user.Name}\tState: {user.ImpersonationLevel}\n");

        await WindowsIdentity.RunImpersonatedAsync(user.AccessToken, async () =>
        {
            var impersonatedUser = WindowsIdentity.GetCurrent();
            var message =
                $"User: {impersonatedUser.Name}\t" +
                $"State: {impersonatedUser.ImpersonationLevel}";

            var bytes = Encoding.UTF8.GetBytes(message);
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        });
    }
    catch (Exception e)
    {
        await context.Response.WriteAsync(e.ToString());
    }
});
```

While the [Microsoft.AspNetCore.Authentication.Negotiate](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.Negotiate) package enables authentication on Windows, Linux, and macOS, impersonation is only supported on Windows.