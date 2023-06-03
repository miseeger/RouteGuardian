# ![RouteGuardianLogo_xxs](Assets/RouteGuardianLogo_xxs.png) RouteGuardian

[![NuGet](https://img.shields.io/nuget/v/RouteGuardian.svg)](https://www.nuget.org/packages/RouteGuardian) [![lic](https://img.shields.io/badge/license-MIT-blue)](https://github.com/miseeger/RouteGuardian/blob/main/LICENSE)


RouteGuardian protects API routes with RouteGuardian middleware or RouteGuardian policy - heavily inspired by [F3-Access](https://github.com/xfra35/f3-access).

RouteGuardian checks rules that are set up for resource-based authorization and, depending on the authorization, releases access for the requesting user. Generally, this can be done after the programmatic initialization of a RouteGuardian instance. Authorization is then done either in a base controller, or in the case of a minimal API, directly in each endpoint. This approach may be very repititive and therefore two ways are offered to include the RouteGuardian in the ASP.NET pipeline:

1) as Middleware - `RouteGuardianMiddleware`
2) as Authorization-Policy - `RouteGuardianPolicy`

The RouteGuardian middleware and policy are fundamentally designed for a group-based authorization scenario that supports both JWT authentication and authorization and Windows authentication and authorization (via Windows user groups). When using the basic functionality of RouteGuardian, the verification policies can be implemented as required.

In addition, RouteGuardian provides a JwtHelper for Webtoken processing and a WinHelper for processing AD group authorizations from Windows Authentication. The latter also implements a GroupsCache for WinAuth.

## General Use

The RouteGuardian has a general policy that either enables (`deny`) or allows (`allow`) access to API endpoints (resources) by default. When the RouteGuardian is instantiated, the default policy is `deny`. This means that all routes are generally blocked unless explicitly enabled: Need-To-Know principle.

```c#
var routeGuardian = new RouteGuardian();
```

In turn, you can reverse the policy accordingly if you generally want all but a few resources to be freely accessible:

```c#
var routeGuardian = new RouteGuardian()
    .DefaultPolicy(GuardPolicy.Allow);
```

### Definition of access rules

Access to a route is either allowed or denied. The HTTP method(s) can be specified. A wildcard (`*`) is specified for all methods. Furthermore, it is defined for which users (or groups, etc.) the rule is set up.

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/admin", "ADMIN|PROD")   // (1)
    .Deny("*", "/admin/part2", "*")       // (2)
    .Allow("*", "/admin/part2", "ADMIN"); // (3)
```

The rules defined above (Default Policy = `deny`), have the following effect:

1) Users in the `ADMIN` or `PROD` roles have access to the `/admin` route (only up to there!) All HTTP methods are allowed.
2) For all roles, the `/admin/part2` route is blocked, as well as for all HTTP methods. This is implicitly the case with the default policy `deny`.
3) The rule set in 2. is cancelled again for the role `ADMIN` and thus here the route `/admin/part2` is released for all HTTP methods.

> **Important!** All access rule checks are case-insensitive. Internally, all routes are converted to lowercase. Verbs and the roles/subjects are treated as upper case. This is to achieve a minimum of error tolerance. Of course, the corresponding information (Verbs, Routes and Subjects) must be written correctly.

#### Wildcards

The routes for which the access rules are set can contain wildcards. The following wildcards are supported:

- `*` - all route fragments that are in the place of the asterisk.
- `{int}` - an integer, signed (RegEx Pattern: `[+-]?(?<!\.)\b[0-9]+\b(?!\.[0-9]`)
- `{dec}` - a decimal number, with sign and dot as decimal separator (RegEx Pattern: `[+-]?(?:\d*\.)?\d+`)
- `{str} ` - a sequence of any alphanumeric characters. The hyphen, the underscore and also the space character are allowed as special characters (RegEx Pattern: `[a-zA-Z0-9_-]+`).
- `{guid}` - a GUID (RegEx Pattern: `[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?`)

An example of using `*` as a wildcard might look like this:

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/admin*", "ADMIN")               // (1)
    .Allow("*", "/public*, "*")                   // (2)    
    .Allow("*", "/*/edit", "ADMIN");              // (3)
```

Here, too, the default policy is set to `deny` and thus the defined rules have the following effect:

1) All routes starting with `/admin` are enabled for the `ADMIN` group only.
2) All routes starting with `/public` are shared with all users.
3) Routes whose path ends in `edit` are allowed only for the `ADMIN` group.

Here are some examples of the use of the "concrete" wildcards, which also includes a check for the correctness of the values given for the wildcard

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/products/{guid}", "*")            
    .Allow("*", "/products/{guid}/load/{dec}", "*")
    .Allow("*", "/products/report/page/{int}", "*")
    .Allow("*", "/products/report/{str}", "*"); 
```

### Priority of paths/routes

Die Priorität der defnierten und zu prüfenden Routen erfolgt von der spezifischsten Route zur am wenigsten spezifischen Route. Das bedeutet: Routen mit Wildcards werden nach den spezifischen Routen behandelt:

These routes ...

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/admin*", "ADMIN")        
    .Allow("*", "/admin/blog/foo", "ADMIN")
    .Allow("*", "/admin/blog", "ADMIN")        
    .Allow("*", "/admin/blog/foo/bar","ADMIN")
    .Allow("*", "/admin/blog/*/bar","ADMIN")
```

... would be treated with the following priority:

```c#
var routeGuardian = new RouteGuardian()
    .Allow("*", "/admin/blog/foo/bar","ADMIN")
    .Allow("*", "/admin/blog/*/bar","ADMIN")    
    .Allow("*", "/admin/blog/foo", "ADMIN")
    .Allow("*", "/admin/blog", "ADMIN")        
    .Allow("*", "/admin*", "ADMIN")        
```

> **Wichtig:** Es greift die erste Regel, zu der der angefragte Pfad passt.

### Multiple HTTP verbs

Selected HTTP verbs for a route can be assigned a rule. These do not necessarily have to be defined as one rule. By separating them with the pipe (`|`) as a separator, multiple verbs are stored for one rule:

```c#
var routeGuardian = new RouteGuardian()
    .Clear()
    .DefaultPolicy(GuardPolicy.Allow)
    .Deny("POST|PUT|DELETE", "/blog/Entry", "*") // (1)
    .Allow("*", "/blog/entry", "ADMIN");         // (2)
```

1. For all subjects HTTP `POST`, `PUT` and `DELETE` is denied.
2. Only the (role) `ADMIN` has access to all HTTP verbs of the `/blog/entry` path.

### Multiple subjects (authorized objects, such as roles)

Just as it is possible to store multiple HTTP verbs for a rule, it is also possible to include multiple subjects for a rule:

```c#
var routeGuardian = new RouteGuardian()
    .Clear()
    .DefaultPolicy(GuardPolicy.Allow)
    .Deny("POST|PUT|DELETE", "/blog/entry", "*") // (1)
    .Allow("*", "/blog/entry", "ADMIN");         // (2)
```

​	Explanation: see above.

In the use case of checking access to a route (with `IsGranted()`, it is also possible to use multiple subjects for checking, for example, if the requesting user has multiple permission roles. Here is an example from the RouteGuardian tests that should make this application clear*:

```c#
Assert.IsTrue(
    routeGuardian.IsGranted("GET", "/blog/entry", "Client|CUSTOMER")
    && !routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT|customer")
    && routeGuardian.IsGranted("PUT", "/blog/entry", "CLIENT|admin")
);
```

​	* All information here is case-insensitive.

### Reading the rules from a configuration file

A programmatically defined rule set for access to certain routes may not be the best solution because changes require recompiling and re-publishing the project. A workaround is to store a configuration file with the default policy and rules for the endpoints. This file must have the name `access.json` and be located in the main directory of the application. It has the following specification as a minimum configuration:

```json
{
  "default": "deny", // (1)
  "rules": []        // (2)
}
```

1. All routes are prohibited (Need-To-Know principle)
2. No rules are defined.

The rules are specified, similar to programmatic configuration. The segments (Policy, HTTP-Verb, Route, Subjects) are separated by **one(!)** space.

```json
{
  "default": "deny",
  "rules": [
    "allow GET /foo/bar/admin ADMIN|PROD",
    "deny POST /foo/bar/admin *",
    "allow * /admin ADMIN|PROD",
    "deny * /admin/part2 *",
    "allow GET /api/test/test ADMIN",
    "deny GET /api/test/xyz ADMIN"
  ] 
}
```

Again, all entries are case-insensitive.

> The presence of the `access.json` file does not exclude the possibility of programmatically adding fixed rules to the RouteGuardian after all, depending on requirements.

## Helper

### JwtHelper

#### Configuration

When instantiating the `JwtHelper` a configuration is required, which is either controlled in the `appsettings.json` globally or in the environment settings (per environment). The required information is needed for handling JSON web tokens and is to be specified for the JwtAuthentication as follows:

```json
/// appsettings[.Development|.Production].json
{
    ...
    "RouteGuardian": {
        "JwtAuthentication": {
            "ApiSecretEnVarName": "JwtDevSecret",
            "ValidateIssuer": "true",
            "ValidateAudience": "true",
            "ValidateIssuerSigningKey": "true",
            "ValidateLifetime": "false",
            "ValidIssuer": "RouteGuardian",
            "ValidAudience": "RouteGuardianTests"
        }
    },
    ...
}
```

| Property                   | Values         | Bedeutung                                                    |
| -------------------------- | -------------- | ------------------------------------------------------------ |
| `ApiSecretEnvarName`       | any (`string`) | The name of the environment variable under which the Secret Key is used to encrypt the JWT. |
| `ValidateIssuer`           | true / false   | Controls whether the publisher is checked when validating the token. |
| `ValidateAudience`         | true / false   | Determines whether the requester is checked when validating the token. |
| `ValidateIssuerSigningKey` | true / false   | Controls whether the secret key is checked when validating the token. |
| `ValidateLifetime`         | true / false   | Defines whether the validity period (default: 1440 minutes) is checked when validating the token. |
| `ValidIssuer`              | any (String)   | The name of the issuer for which the token is valid.         |
| `ValidAudience`            | any            | The name of the requester for which the token is valid.      |

#### Interface

| Property   | Type                    | Result                                                       |
| ---------- | ----------------------- | ------------------------------------------------------------ |
| `Settings` | `IConfigurationSection` | Contains the values of the previously described JWT configuration. |
| `Secret`   | `string`                | The encryption password for JWTs stored in an environment variable in the system. |

| Method                                                       | Result                      | Function                                                     |
| ------------------------------------------------------------ | --------------------------- | ------------------------------------------------------------ |
| `GetTokenValidationParameters()`                             | `TokenValidationParameters` | Returns the values specified in the configuration as an object of type `TokenValidationParemeters`. |
| `GenerateToken(claims, key, userName, userId, issuer, audience, validForMinutes, algorithm)` | `string`                    | Generates a JWT (encrypted with `HmacSha256`). The method must be given a list of claims which has at least the claim `rol`, which carries the roles of the authenticating user. It is also mandatory to specify the password for encryption. The following parameters for the method are predefined as follows and do not need to be specified and are by default set to an empty string: `username`, `userId`, `issuer` , `audience` . `validForMinutes` is preset with 1440 (= 24 hours). As `algorithm` `HmacSha256` is preset. |
| `ValidateToken(authToken)`                                   | true / false                | Checks the token specified as a string and returns whether it is valid according to the validation settings (see above). |
| `ReadToken(authToken)`                                       | `JwtSecurityToken?`         | Reads the token specified as a string, has it checked and converts it to a `JwtSecurityToken`. If the token is invalid, `null` is returned here. |
| `GetSubjectsFromJwtToken(authToken)`                         | `string`                    | `Gets the role claims from the passed and checked token and returns them as a composite (CSV) string. |
| GetTokenFromContext(context)                                 | `string`                    | Reads the JWT token from the authorization header of the request (in `HttpContext`). |
| GetTokenClaimsFromContext(context)                           | `List<Claim>?`              | Reads all claims of the JWT token from the authorization header of the request (in `HttpContext`). If an invalid token was detected, `null` is returned. |
| GetTokenClaimValueFromContext(context, claimType)            | `string?`                   | Returns the value of a claim (determined via the specified `claimType`) from the claims determined from the JWT token in the Authorization header of the request. If the token is invalid or the claimType is not set/present, then `null` is returned. |

### WinHelper

The WinHelper takes care of the determination of the groups from the Active Directory assigned to the user authenticated via Windows Single-Sign-On. These can be converted into plain text via auxiliary methods and returned as RouteGuardian subjects. This methodology is used by the RouteGuardian middleware and the RouteGuardian policy for API endpoints.

Since the process of determining AD user groups and converting them to plaintext takes valuable time, the user groups, once determined, are hashed per user and given a hash code. This hash code is calculated from the GUIDs of the AD groups even before the translation into plain text takes place. This way, a new translation is then only necessary when changes are made to a user's AD groups.

#### Configuration

No configuration is required for the Windows Helper. It works out-of-the-box.

#### Interface

| Method                                | Result   | Function                                                     |
| ------------------------------------- | -------- | ------------------------------------------------------------ |
| GetWinUserGroupsHash(identity)        | `string` | Gets the hash value of all AD grups of the authenticated user (`WindowsIdentity`). The hash is supplied as an MD5 hash. |
| GetSubjectsFromWinUserGroups(context) | `string` | Returns all RouteGuardian subjects (plain text of AD groups) of an authenticated user from the passed `HttpContext`. This method uses the Windows User Groups cache already described. |
| ClearWinUserGroupsCache()             | none     | Clears the `WinUserGroupsCache`.                             |

## JWT-Authentication

RouteGuardian provides a pre-built extension method for the `IServiceCollection`, which during service configuration in `Program.cs` ensures that the application can use JWT authentication and the JWTHelpter (as `IJwtHelper`) is provided for depencency injection.

The configuration is as follows:

`Program.cs`

```c#
using RouteGuardian.Extension;

// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

...
    
// ----- Authentication and Authorization -------------------------------------
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// ----- Register and configure JWT-Authenticaiton ----------------------------
builder.Services.AddJwtAuthentication(builder.Configuration);

...
```

It is important here that both the Authentication and Authorization are included and for the JwtAuthentication in particular the Configuration (`appsettings.json`) is also passed, from which the necessary settings for the JWT authentication are taken (see above).

## Windows-Authentication

The Windows authentication is similar to the JWT authentication. It is also provided via an extension method, which provides the WinHelper (as `IWinHelper`) for the dependency injection.

The configuration in `Program.cs` is as follows:

```c#
using RouteGuardian.Extension;

// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

...
    
// ----- Authentication and Authorization -------------------------------------
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// ----- Register and configure Windows-Authenticaiton ------------------------
builder.Services.AddWindowsAuthentication(builder.Configuration);

...
```

## RouteGuardianPolicy

The RouteGuardianPolicy is a policy named `RougeGuardian` which is used to check authorized access to an API endpoint, checking the RouteGuardian rules.

The policy is also registered in `Program.cs`, like this:

```c#
using RouteGuardian.Extension;

// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

...
    
// ----- Register and configure JWT-Authenticaiton ----------------------------
builder.Services.AddWindowsAuthentication(builder.Configuration);
builder.Services.AddRouteGuarianPolicy("access.json");
...
```

The only information the policy needs for configuration is the path of the file with the access rules to be used.

> If the RouteGuardianPolicy is used in an application, no RouteGuardianMiddleware is needed.

#### Using the RouteGuardianPolicy

This short code example shows the use of the RouteGuardianPolicy on a very simple minimal API endpoint, where in fact only the `RequireAuthorization` method is appended with the specification of the registered "RouteGuardian" policy.

```c#
app.MapGet("/helloworld", () => "Hello World!")
    .RequireAuthorization("RouteGuardian");
```

In an MVC-style controller, the endpoint is secured via the `[Authorize]` attribute:

```c#
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyApi.Controllers;

// [Authorize(Policy = "RouteGuardian")]                    // (1)
public class MyController : Controller
{
    [Authorize(Policy = "RouteGuardian")]                   // (2) 
    public IActionResult Hello() => "Hello secret World!";
}
```

1. With the attribute either all controller endpoints ...
2. ... or only targeted endpoints can be protected.

## RouteGuardianMiddleware

The RouteGuardian middleware is placed in the pipeline between the authentication and authorization middleware and before the mapping middleware for the controllers. This configuration is done in the `Program.cs` as for the policy.

The integration is done, for example, as follows:

```c#
using RouteGuardian.Extension;
// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

... 

// ===== Pipeline (Middleware) ================================================
var app = builder.Build();

...

app.UseAuthentication();
app.UseAuthorization();

// Integration of the RouteGuardian middleware:
app.UseRouteGuardian("/api"); 

app.MapControllers();

app.Run();
```

The only information that the RouteGuardian middleware needs for the configuration is the base path of the endpoints to be protected, here `api`. The rules to be considered are read from the definition file named `access.json` when the application is started (see above).

> If the RouteGuardianMiddleware is used in an application, no RouteGuardianPolicy is needed.

## Extras

The RouteGuardian library comes with a few goodies that don't necessarily belong in the context of securing API routes, but could be useful for a (web) application.

### GlobalExceptionHandlerMiddleware

The `GlobalExceptionHandlerMiddleware` is placed at the very beginning of the request pipeline and catches global unhandled exceptions, logs them and returns a 500 HTTP response with the text of the exception message if this is desired. If not, then a generic message is returned.

```c#
...

// ===== Pipeline (Middleware) ================================================
var app = builder.Build();

app.UseGlobalExceptionHandler(true); // (1)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

...

app.Run();
```

1. The global exception handler is included as the first link in the request pipeline and the qualified error message is switched on. Without specification/parameter the return of error messages is prevented (as default).

### StringExtension(s)

The only string extension in this library provides the computation of a MD5 hash string. It extends the string type with the method `ComputeMd5(string)`. It is used in RouteGuardian when hashing the AD permission groups for the `WinUserGroupsCache`.

## Lizenzbedingungen

Diese Software wird von [Michael Seeger](https://github.com/miseeger), in Deutschland, mit :heart: entwickelt und betreut. Lizensiert unter [MIT](https://github.com/miseeger/RouteGuardian/blob/main/LICENSE).
