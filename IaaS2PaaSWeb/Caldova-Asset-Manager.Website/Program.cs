using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using CaldovaAssetManager.Hubs;
using CaldovaAssetManager.Models;
using CaldovaAssetManager.ProductSearch;
using CaldovaAssetManager.Recommendations;
using CaldovaAssetManager.Security;
using CaldovaAssetManager.Utils;

var builder = WebApplication.CreateBuilder(args);
ConfigurationHelpers.Initialize(builder.Configuration);

var connection = builder.Configuration.GetConnectionString("DefaultConnectionString")
    ?? builder.Configuration["SQLCONNSTR_DefaultConnectionString"];
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnectionString for the existing SQL database. SQL Managed Instance is not required.");
builder.Services.AddDbContext<CaldovaAssetManagerContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<ICaldovaAssetManagerContext>(sp => sp.GetRequiredService<CaldovaAssetManagerContext>());
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddUserStore<LegacyUserStore>()
    .AddRoleStore<LegacyRoleStore>()
    .AddDefaultTokenProviders();
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = cookieSecurePolicy);
var protection = builder.Services.AddDataProtection().SetApplicationName("Caldova-Asset-Manager");
var keyPath = builder.Configuration["DataProtection:KeyPath"];
if (string.IsNullOrWhiteSpace(keyPath)
    && !string.IsNullOrWhiteSpace(builder.Configuration["WEBSITE_INSTANCE_ID"]))
{
    var home = builder.Configuration["HOME"];
    if (string.IsNullOrWhiteSpace(home))
        throw new InvalidOperationException("Azure App Service requires HOME or an explicit DataProtection:KeyPath for durable key storage.");
    keyPath = Path.Combine(home, "data", "Caldova-Asset-Manager", "keys");
}
if (!string.IsNullOrWhiteSpace(keyPath))
{
    if (!Path.IsPathFullyQualified(keyPath))
        throw new InvalidOperationException("DataProtection:KeyPath must be an absolute path on durable, access-controlled storage.");
    protection.PersistKeysToFileSystem(Directory.CreateDirectory(keyPath));
}
else if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Configure DataProtection:KeyPath for durable key storage when running outside Azure App Service in production.");
}

var authentication = builder.Services.AddAuthentication();
foreach (var provider in new[] { "Facebook", "Google", "Microsoft", "Twitter" })
{
    var section = builder.Configuration.GetSection("Authentication:" + provider);
    if (!section.GetValue<bool>("Enabled"))
        continue;
    var key = section["ClientId"];
    var secret = section["ClientSecret"];
    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
        throw new InvalidOperationException($"Enabled authentication provider {provider} requires externally configured ClientId and ClientSecret.");
    switch (provider)
    {
        case "Facebook":
            authentication.AddFacebook(o => { o.AppId = key; o.AppSecret = secret; });
            break;
        case "Google":
            authentication.AddGoogle(o => { o.ClientId = key; o.ClientSecret = secret; });
            break;
        case "Microsoft":
            authentication.AddMicrosoftAccount("Microsoft", o => { o.ClientId = key; o.ClientSecret = secret; });
            break;
        case "Twitter":
            authentication.AddTwitter(o => { o.ConsumerKey = key; o.ConsumerSecret = secret; });
            break;
    }
}
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});
builder.Services.AddScoped<LayoutDataAttribute>();
builder.Services.AddControllersWithViews(options => options.Filters.AddService<LayoutDataAttribute>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddSignalR().AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddScoped<IOrdersQuery, OrdersQuery>();
builder.Services.AddScoped<IRaincheckQuery, RaincheckQuery>();
builder.Services.AddScoped<IProductSearch, StringContainsProductSearch>();
builder.Services.AddScoped<IShippingTaxCalculator, DefaultShippingTaxCalculator>();
builder.Services.AddScoped<IRecommendationEngine, AzureMLFrequentlyBoughtTogetherRecommendationEngine>();
builder.Services.AddScoped<AccountMessageSender>();
builder.Services.AddSingleton<ITelemetryProvider, TelemetryProvider>();
builder.Services.AddHttpClient<IHttpClient, HttpClientWrapper>(client => client.Timeout = TimeSpan.FromSeconds(30));

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(handler => handler.Run(context =>
        Results.Problem("The request could not be completed. See server logs for details.").ExecuteAsync(context)));
    app.UseHsts();
    app.UseHttpsRedirection();
}
foreach (var directory in new[] { "Content", "Images", "Scripts", "fonts" })
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, directory)),
        RequestPath = "/" + directory
    });
}
app.MapGet("/favicon.svg", () => Results.File(Path.Combine(app.Environment.ContentRootPath, "favicon.svg"), "image/svg+xml"));
app.MapGet("/favicon.ico", () => Results.Redirect("/favicon.svg"));
app.MapGet("/health", () => Results.Ok(new { status = "running" })).AllowAnonymous();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<AnnouncementHub>("/signalr");
app.MapControllers();
app.MapControllerRoute("areas", "{area:exists}/{controller=Orders}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
