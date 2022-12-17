using System.Globalization;
using Microsoft.AspNetCore.Localization;
using RouteGuardian.Extension;

// ===== References ===========================================================
// - https://stackoverflow.com/questions/39006690/asp-net-core-request-localization-options
// - https://stackoverflow.com/questions/40442444/set-cultureinfo-in-asp-net-core-to-have-a-as-currencydecimalseparator-instead


// ===== Services =============================================================
var builder = WebApplication.CreateBuilder(args);

// ----- Caching --------------------------------------------------------------
//services.AddResponseCaching();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----- Localization ---------------------------------------------------------
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

// ----- Authentication and Authorization -------------------------------------

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

//builder.Services.AddWindowsAuthentication();
builder.Services.AddJwtAuthentication(builder.Configuration);

// ===== Pipeline (Middleware) ================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRouteGuardianJwtAuthorization();
app.UseRouteGuardian("/api");

app.MapControllers();

app.Run();
