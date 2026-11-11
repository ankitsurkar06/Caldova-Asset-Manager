using System.Net;
using System.Xml.Linq;
using CaldovaAssetManager.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Caldova.Hosting.Tests;

public sealed class BrandingTests
{
    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/Store")]
    [InlineData("/ShoppingCart")]
    public async Task PagesUseApplicationBrand(string path)
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Matches("<title>[^<]*Caldova-Asset-Manager</title>", html);
        Assert.Contains("<h1>Caldova-Asset-Manager</h1>", html);
        Assert.Contains("src=\"/Images/caldova-logo.svg\"", html);
        Assert.Contains("alt=\"Caldova-Asset-Manager logo\"", html);
        Assert.Contains("href=\"/favicon.svg\"", html);
        Assert.Contains("<h2>Asset Management</h2>", html);
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Theory]
    [InlineData("/Images/caldova-logo.svg")]
    [InlineData("/favicon.svg")]
    public async Task BrandAssetsAreServed(string path)
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        var svg = XDocument.Parse(await response.Content.ReadAsStringAsync());
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Equal(ns + "svg", svg.Root?.Name);
        Assert.Equal("Caldova-Asset-Manager logo", svg.Root?.Element(ns + "title")?.Value);
        Assert.Equal(0, app.SqlGuard.Attempts);
    }

    [Fact]
    public async Task ConventionalFaviconUsesCurrentBrand()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var client = data.CreateClient(app);
        using var response = await client.GetAsync("/favicon.ico");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/favicon.svg", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public void IdentityChangesPreserveExistingSchemaAndProtectedData()
    {
        using var data = new HostingTestData();
        using var app = data.CreateFactory();
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<CaldovaAssetManagerContext>();

        Assert.Same(context, services.GetRequiredService<ICaldovaAssetManagerContext>());
        Assert.Equal("Caldova-Asset-Manager", typeof(CaldovaAssetManagerContext).Assembly.GetName().Name);
        Assert.Equal("CaldovaAssetManager.Models", typeof(CaldovaAssetManagerContext).Namespace);
        Assert.Equal("Caldova-Asset-Manager", services.GetRequiredService<IOptions<DataProtectionOptions>>()
            .Value.ApplicationDiscriminator);
        Assert.Equal("Products", context.Model.FindEntityType(typeof(Product))?.GetTableName());
        Assert.Equal("Orders", context.Model.FindEntityType(typeof(Order))?.GetTableName());
        Assert.Equal("AspNetUsers", context.Model.FindEntityType(typeof(ApplicationUser))?.GetTableName());
        Assert.Equal("AspNetRoles", context.Model.FindEntityType(typeof(IdentityRole))?.GetTableName());
        Assert.Equal(0, app.SqlGuard.Attempts);
    }
}
