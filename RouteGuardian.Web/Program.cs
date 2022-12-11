using System.Globalization;
using Microsoft.AspNetCore.Localization;
using RouteGuardian.Extension;

// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

// ----- Caching --------------------------------------------------------------
//services.AddResponseCaching();
// ----------------------------------------------------------------------------

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// ----- Localization ---------------------------------------------------------
// https://stackoverflow.com/questions/39006690/asp-net-core-request-localization-options
// https://stackoverflow.com/questions/40442444/set-cultureinfo-in-asp-net-core-to-have-a-as-currencydecimalseparator-instead

var supportedCultures = new List<CultureInfo>
{
    new CultureInfo("de-DE")
    // ,new CultureInfo("en-US")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("de-DE");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.FallBackToParentCultures = true;
    options.FallBackToParentUICultures = true;
});

builder.Services.AddLocalization();
// ----------------------------------------------------------------------------

// ----- Windows Authentication -----------------------------------------------
//builder.Services.AddWindowsAuthentication();
// ----------------------------------------------------------------------------

// ----- JWT Authentication ---------------------------------------------------
// Token for testing
// eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIwODE1IiwibmFtZSI6Ik51bGxBY2h0RnVmZnplaG4iLCJyb2wiOiJBRE1JTnxQUk9EIiwiaWF0IjoxNjcwNzgwNDI4LCJleHAiOjE3MzU2ODU5OTksImlzcyI6ImRvdE5FVC1SUEciLCJhdWQiOiJkb3RORVQtUlBHQ2xpZW50In0._cY3pJVzF0Z6GINc2I57Gos8RyVI23Om4KqfdOnAhjs
// JwtDevSecret=J64CU9UJTp@4@8dejeCiFok8IXyJ2A18sFqRIZC35Y5qDHwCeZ

// Info: https://www.infoworld.com/article/3669188/how-to-implement-jwt-authentication-in-aspnet-core-6.html
builder.Services.AddJwtAuthentication(builder.Configuration);
// ----------------------------------------------------------------------------


// ===== Pipeline (Middleware) ================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.AuthorizeRouteGuardian();
app.UseRouteGuardian("/api");

app.MapControllers();

app.Run();
