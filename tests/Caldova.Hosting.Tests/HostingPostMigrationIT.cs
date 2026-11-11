using System.Data.Common;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using CaldovaAssetManager.Models;
using Xunit;

namespace Caldova.Hosting.Tests;

public sealed class HostingPostMigrationIT
{
    [Fact]
    public async Task TC_WEB_001_Health()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var response = await client.GetAsync("/health");
        await AssertHealth(response, data);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public void TC_WEB_002_MissingSql()
    {
        using var data = new HostingTestData();
        foreach (var value in new[] { null, "", "   " })
        {
            using var app = data.CreateFactory(new()
            {
                ["ConnectionStrings:DefaultConnectionString"] = value,
                ["SQLCONNSTR_DefaultConnectionString"] = null
            });
            AssertStartupFailure(app, data.Expected("sqlConfigurationError"));
        }
    }

    [Fact]
    public async Task TC_WEB_003_AppServiceHome()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory(new()
        {
            ["DataProtection:KeyPath"] = null,
            ["WEBSITE_INSTANCE_ID"] = Guid.NewGuid().ToString(),
            ["HOME"] = data.DirectoryPath
        });
        using var client = data.CreateClient(app);
        await AssertHealth(await client.GetAsync("/health"), data);
        app.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("hosting-test").Protect("exercise-key-storage");
        var path = Path.Combine(data.DirectoryPath, "data", "Caldova-Asset-Manager", "keys");
        var keys = Directory.GetFiles(path, "key-*.xml");
        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.Equal("key", XDocument.Load(key).Root?.Name.LocalName));
        var environment = app.Services.GetRequiredService<IWebHostEnvironment>();
        Assert.Empty(Directory.GetFiles(environment.ContentRootPath, "key-*.xml"));
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public void TC_WEB_004_MissingHome()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory(new()
        {
            ["DataProtection:KeyPath"] = null,
            ["WEBSITE_INSTANCE_ID"] = Guid.NewGuid().ToString(),
            ["HOME"] = ""
        });
        AssertStartupFailure(app, data.Expected("homeConfigurationError"));
    }

    [Fact]
    public void TC_WEB_005_MissingProductionKeys()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory(new() { ["DataProtection:KeyPath"] = null });
        AssertStartupFailure(app, data.Expected("keyConfigurationError"));
    }

    [Fact]
    public void TC_WEB_006_RelativeKeyPath()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory(new() { ["DataProtection:KeyPath"] = "relative-directory" });
        AssertStartupFailure(app, data.Expected("relativeKeyError"));
        Assert.False(Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "relative-directory")));
    }

    [Fact]
    public async Task TC_WEB_007_HttpsAndUntrustedForwarding()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app, https: false);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-Host", "attacker.example.test");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal(data.Expected("origin") + "/health", response.Headers.Location?.AbsoluteUri);
        using var secureClient = data.CreateClient(app);
        using var secureResponse = await secureClient.GetAsync("/health");
        await AssertHealth(secureResponse, data);
        Assert.Contains("max-age=", Assert.Single(secureResponse.Headers.GetValues("Strict-Transport-Security")));
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public async Task TC_WEB_008_HostFiltering()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = "attacker.example.test";
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("\"running\"", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public async Task TC_WEB_009_StaticAssets()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var response = await client.GetAsync(data.Expected("assetPath"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(data.Expected("assetSha256"), Convert.ToHexString(SHA256.HashData(bytes)));
        using var missing = await client.GetAsync("/Content/missing-hosting-test.css");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public async Task TC_WEB_010_AdminChallenge()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var response = await client.GetAsync("/Admin/Orders");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirect = Assert.IsType<Uri>(response.Headers.Location);
        Assert.Equal(data.Expected("origin") + "/Account/Login", redirect.GetLeftPart(UriPartial.Path));
        Assert.Equal("/Admin/Orders", QueryHelpers.ParseQuery(redirect.Query)["ReturnUrl"].ToString());
        Assert.DoesNotContain("Order History", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public async Task TC_WEB_011_LoginAcrossRestart()
    {
        using var data = new HostingTestData();
        string cookie;
        string[] originalKeyIds;
        using (var first = data.CreateFactory(new()
        {
            ["WEBSITE_INSTANCE_ID"] = Guid.NewGuid().ToString(),
            ["HOME"] = Path.Combine(data.DirectoryPath, "unused-home")
        }))
        {
            data.SeedUser(first);
            Assert.Equal(CookieSecurePolicy.Always, first.Services
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(IdentityConstants.ApplicationScheme).Cookie.SecurePolicy);
            Assert.Equal(CookieSecurePolicy.Always, first.Services
                .GetRequiredService<IOptions<AntiforgeryOptions>>().Value.Cookie.SecurePolicy);
            using var client = data.CreateClient(first);
            using var login = await Login(client, data, data.User("password"));
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            Assert.Equal("/Orders", login.Headers.Location?.OriginalString);
            cookie = FindCookie(login, data.Expected("identityCookie"));
            AssertSecureCookie(cookie);
            originalKeyIds = ReadKeyIds(data.KeyPath);
            Assert.NotEmpty(originalKeyIds);
            Assert.False(Directory.Exists(Path.Combine(data.DirectoryPath, "unused-home", "data")));
            AssertUser(first, data);
            Assert.Equal(0, first.SqlGuard.Attempts);
        }
        using var restarted = data.CreateFactory();
        data.SeedUser(restarted);
        using var restartedClient = data.CreateClient(restarted);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Orders");
        request.Headers.Add("Cookie", cookie.Split(';')[0]);
        using var response = await restartedClient.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(data.Expected("ordersMarker"), await response.Content.ReadAsStringAsync());
        Assert.All(originalKeyIds, id => Assert.Contains(id, ReadKeyIds(data.KeyPath)));
        AssertUser(restarted, data);
        Assert.Equal(0, restarted.SqlGuard.Attempts);
    }

    [Fact]
    public async Task TC_WEB_012_InvalidLogin()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        data.SeedUser(app);
        using var client = data.CreateClient(app);
        using var response = await Login(client, data, data.User("wrongPassword"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(data.Expected("loginFailure"), await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(GetCookies(response), value =>
            value.StartsWith(data.Expected("identityCookie") + "=", StringComparison.Ordinal));
        using var orders = await client.GetAsync("/Orders");
        Assert.Equal(HttpStatusCode.Redirect, orders.StatusCode);
        AssertUser(app, data);
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public async Task TC_WEB_013_SecureSession()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var response = await client.GetAsync("/ShoppingCart");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(data.Expected("cartMarker"), await response.Content.ReadAsStringAsync());
        Assert.Equal(CookieSecurePolicy.Always, app.Services
            .GetRequiredService<IOptions<SessionOptions>>().Value.Cookie.SecurePolicy);
        AssertSecureCookie(FindCookie(response, data.Expected("sessionCookie")));
        AssertEmptyBusinessState(app);
    }

    [Fact]
    public async Task TC_WEB_014_SignalRNegotiation()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var response = await client.PostAsync("/signalr/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;
        Assert.Equal(1, body.GetProperty("negotiateVersion").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("connectionId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("connectionToken").GetString()));
        Assert.Contains(body.GetProperty("availableTransports").EnumerateArray(),
            transport => transport.GetProperty("transport").GetString() == "WebSockets");
        using var invalid = await client.PostAsync("/signalr/negotiate?negotiateVersion=invalid", null);
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        using var invalidDocument = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        Assert.Single(invalidDocument.RootElement.EnumerateObject());
        Assert.Equal(data.Expected("negotiateError"), invalidDocument.RootElement.GetProperty("error").GetString());
        Assert.False(invalidDocument.RootElement.TryGetProperty("connectionToken", out _));
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public async Task TC_WEB_015_ProductionFailure()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory(failSql: true);
        using var client = data.CreateClient(app);
        using var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(text);
        Assert.Equal(500, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(data.Expected("problemTitle"), document.RootElement.GetProperty("title").GetString());
        Assert.Equal(data.Expected("problemDetail"), document.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain(SqlConnectionGuard.FailureMessage, text);
        Assert.DoesNotContain(data.Configuration["ConnectionStrings:DefaultConnectionString"]!, text);
        Assert.Equal(1, app.SqlGuard.Attempts);
    }

    [Fact]
    public async Task TC_WEB_016_DevelopmentHttp()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory(new() { ["DataProtection:KeyPath"] = null }, environment: "Development");
        using var client = data.CreateClient(app, https: false);
        using var response = await client.GetAsync("/ShoppingCart");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(data.Expected("cartMarker"), await response.Content.ReadAsStringAsync());
        Assert.Equal(CookieSecurePolicy.SameAsRequest, app.Services
            .GetRequiredService<IOptions<SessionOptions>>().Value.Cookie.SecurePolicy);
        var cookie = FindCookie(response, data.Expected("sessionCookie"));
        Assert.Contains("; httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("; secure", cookie, StringComparison.OrdinalIgnoreCase);
        AssertEmptyBusinessState(app);
    }

    private static async Task AssertHealth(HttpResponseMessage response, HostingTestData data)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal(data.HealthStatus, body.RootElement.GetProperty("status").GetString());
    }

    private static void AssertStartupFailure(HostingFactory app, string message)
    {
        var exception = Assert.ThrowsAny<Exception>(() => app.CreateClient());
        Assert.Contains(message, Assert.IsType<InvalidOperationException>(exception.GetBaseException()).Message);
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    private static async Task<HttpResponseMessage> Login(HttpClient client, HostingTestData data, string password)
    {
        using var page = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Login must emit an antiforgery token.");
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = data.User("email"),
            ["Password"] = password,
            ["RememberMe"] = "true",
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value)
        });
        return await client.PostAsync("/Account/Login?returnUrl=%2FOrders", form);
    }

    private static IEnumerable<string> GetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [];

    private static string FindCookie(HttpResponseMessage response, string name) =>
        Assert.Single(GetCookies(response), value => value.StartsWith(name + "=", StringComparison.Ordinal));

    private static void AssertSecureCookie(string cookie)
    {
        Assert.Contains("; secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] ReadKeyIds(string path) => Directory.GetFiles(path, "key-*.xml")
        .Select(file => XDocument.Load(file).Root?.Attribute("id")?.Value
            ?? throw new InvalidOperationException("Key XML must contain a key ID.")).ToArray();

    private static void AssertUser(HostingFactory app, HostingTestData data)
    {
        using var scope = app.Services.CreateScope();
        var user = Assert.Single(scope.ServiceProvider.GetRequiredService<CaldovaAssetManagerContext>().Users);
        Assert.Equal(data.User("id"), user.Id);
        Assert.Equal(data.User("email"), user.Email);
    }

    private static void AssertEmptyBusinessState(HostingFactory app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaldovaAssetManagerContext>();
        Assert.Empty(db.CartItems);
        Assert.Empty(db.Orders);
        Assert.Empty(db.Users);
        Assert.Equal(0, app.SqlGuard.Attempts);
    }
}

internal sealed class HostingTestData : IDisposable
{
    private readonly JsonDocument expectations = ReadJson("expectations", "hosting.json");
    private JsonDocument? user;
    public Dictionary<string, string?> Configuration { get; } =
        JsonSerializer.Deserialize<Dictionary<string, string?>>(
            File.ReadAllText(FixturePath("configuration", "hosting.json")))
        ?? throw new InvalidOperationException("Missing hosting configuration fixture.");
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "Caldova-Hosting-" + Guid.NewGuid().ToString("N"));
    public string KeyPath => Path.Combine(DirectoryPath, "keys");
    public string HealthStatus => expectations.RootElement.GetProperty("health").GetProperty("status").GetString()!;
    public string Expected(string name) => expectations.RootElement.GetProperty(name).GetString()!;
    public string User(string name) => (user ??= ReadJson("seed-data", "user.json")).RootElement.GetProperty(name).GetString()!;

    public HostingFactory CreateFactory(Dictionary<string, string?>? overrides = null,
        string environment = "Production", bool failSql = false)
    {
        var settings = new Dictionary<string, string?>(Configuration)
        {
            ["DataProtection:KeyPath"] = KeyPath,
            ["WEBSITE_INSTANCE_ID"] = "",
            ["HOME"] = "",
            ["SQLCONNSTR_DefaultConnectionString"] = null
        };
        if (overrides != null)
            foreach (var setting in overrides)
                settings[setting.Key] = setting.Value;
        return new HostingFactory(settings, environment, failSql);
    }

    public HttpClient CreateClient(HostingFactory app, bool https = true) => app.CreateClient(new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri(https ? Expected("origin") : Expected("origin").Replace("https://", "http://", StringComparison.Ordinal))
    });

    public void SeedUser(HostingFactory app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaldovaAssetManagerContext>();
        var entity = new ApplicationUser
        {
            Id = User("id"),
            UserName = User("email"),
            Email = User("email"),
            Name = User("name"),
            SecurityStamp = User("securityStamp"),
            EmailConfirmed = true
        };
        entity.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(entity, User("password"));
        db.Users.Add(entity);
        db.SaveChanges();
    }

    private static string FixturePath(string directory, string name) =>
        Path.Combine(AppContext.BaseDirectory, "test-cases", "testdata", directory, name);

    private static JsonDocument ReadJson(string directory, string name) =>
        JsonDocument.Parse(File.ReadAllText(FixturePath(directory, name)));

    public void Dispose()
    {
        expectations.Dispose();
        user?.Dispose();
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal sealed class HostingFactory(
    Dictionary<string, string?> settings,
    string environment,
    bool failSql) : WebApplicationFactory<Program>
{
    private readonly string databaseName = Guid.NewGuid().ToString("N");
    public SqlConnectionGuard SqlGuard { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        foreach (var setting in settings)
            builder.UseSetting(setting.Key, setting.Value);
        builder.ConfigureServices(services =>
        {
            if (failSql)
            {
                services.AddDbContext<CaldovaAssetManagerContext>(options => options.AddInterceptors(SqlGuard));
            }
            else
            {
                services.RemoveAll<DbContextOptions<CaldovaAssetManagerContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<CaldovaAssetManagerContext>>();
                services.AddDbContext<CaldovaAssetManagerContext>(options => options.UseInMemoryDatabase(databaseName));
            }
            if (environment == "Development")
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }
}

internal sealed class SqlConnectionGuard : DbConnectionInterceptor
{
    public const string FailureMessage = "Mock SQL must not be contacted.";
    public int Attempts { get; private set; }

    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        Attempts++;
        throw new InvalidOperationException(FailureMessage);
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
        ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        Attempts++;
        throw new InvalidOperationException(FailureMessage);
    }
}
