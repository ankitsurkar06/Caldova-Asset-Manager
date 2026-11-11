// Offline execution evidence, not an integration suite. Never opens a connection.
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;
using CaldovaAssetManager.Models;

public sealed class SqlProviderCheck : MarshalByRefObject
{
    public static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Pass the website directory.");
        string website = Path.GetFullPath(args[0]);
        var domain = AppDomain.CreateDomain("OfflineSqlProviderCheck", null, new AppDomainSetup
        {
            ApplicationBase = Path.Combine(website, "bin"),
            ConfigurationFile = Path.Combine(website, "Web.config")
        });
        try
        {
            var check = (SqlProviderCheck)domain.CreateInstanceFromAndUnwrap(
                Assembly.GetExecutingAssembly().Location, typeof(SqlProviderCheck).FullName);
            check.Run(website);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally { AppDomain.Unload(domain); }
    }

    public void Run(string website)
    {
        const string invariant = "Microsoft.Data.SqlClient";
        var setting = ConfigurationManager.ConnectionStrings["DefaultConnectionString"];
        Require(setting != null && setting.ProviderName == invariant, "named connection provider");
        Require(DbProviderFactories.GetFactory(invariant) is SqlClientFactory, "ADO.NET factory");
        Require(DbConfiguration.DependencyResolver.GetService(typeof(DbProviderServices), invariant)
            is MicrosoftSqlProviderServices, "EF provider services");
        var factory = DbConfiguration.DependencyResolver.GetService(typeof(IDbConnectionFactory), null)
            as IDbConnectionFactory;
        Require(factory is MicrosoftLocalDbConnectionFactory, "default LocalDB factory");
        using (var defaultConnection = factory.CreateConnection("OfflineProviderCheck"))
            Require(defaultConnection is SqlConnection, "default connection uses Microsoft driver");

        Database.SetInitializer<CaldovaAssetManagerContext>(null);
        using (var context = new CaldovaAssetManagerContext())
        {
            var connection = context.Database.Connection;
            Require(connection is SqlConnection && connection.State == ConnectionState.Closed,
                "context resolves named connection without opening SQL");
            var expected = new SqlConnectionStringBuilder(setting.ConnectionString);
            var actual = new SqlConnectionStringBuilder(connection.ConnectionString);
            Require(actual.DataSource == expected.DataSource &&
                actual.InitialCatalog == expected.InitialCatalog &&
                actual.IntegratedSecurity == expected.IntegratedSecurity,
                "server/database/integrated security preserved");
            Require(actual.InitialCatalog == "Caldova-Asset-Manager.Website" &&
                actual.DataSource == @"(localdb)\mssqllocaldb", "saved LocalDB destination preserved");
            Require(!context.Configuration.LazyLoadingEnabled, "lazy loading unchanged");

            // Invoke the real mapping method, including Identity's base mappings.
            var builder = new DbModelBuilder();
            typeof(CaldovaAssetManagerContext).GetMethod("OnModelCreating",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, new object[] { builder });
            var model = builder.Build(new DbProviderInfo(invariant, "2012"));
            Require(model.StoreModel.EntityTypes.Any(t => t.Name == "Product"), "product model");
            Require(model.StoreModel.EntityTypes.Any(t => t.Name.Contains("IdentityUser")), "Identity model");
            var compiled = model.Compile();
            Database.SetInitializer<DbContext>(null);
            using (var queryContext = new DbContext(connection, compiled, false))
            {
                string sql = queryContext.Set<Product>().Where(p => p.ProductId == 7).ToString();
                Require(sql.Contains("[dbo].[Products]") && sql.Contains("[ProductId]"),
                    "product query translates with Microsoft EF provider");
                string identitySql = queryContext.Set<ApplicationUser>().Where(u => u.Id == "offline").ToString();
                Require(identitySql.Contains("[dbo].[AspNetUsers]"), "Identity table mapping preserved");
            }
            Require(connection.State == ConnectionState.Closed, "all checks stayed offline");
        }
        Require(File.Exists(Path.Combine(website, "bin", "Microsoft.Data.SqlClient.SNI.x64.dll")) &&
            File.Exists(Path.Combine(website, "bin", "Microsoft.Data.SqlClient.SNI.x86.dll")),
            "native SNI assets copied for both architectures");
        Console.WriteLine("PASS: offline SQL provider/configuration/model/query checks; no SQL connection opened.");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + description);
        Console.WriteLine("PASS: " + description);
    }
}
