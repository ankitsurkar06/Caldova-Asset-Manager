// Focused offline evidence against actual built assemblies; never opens SQL.
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
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using CaldovaAssetManager.Models;
using CaldovaAssetManager.Utils;

public sealed class ManagedInstanceCheck : MarshalByRefObject
{
    private const string Variable = "SQLCONNSTR_DefaultConnectionString";
    private const string Valid = "Server=tcp:offline-mi.invalid,1433;Database=ExistingParts;" +
        "Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;";

    public static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Pass the website directory.");
        string website = Path.GetFullPath(args[0]);
        string original = Environment.GetEnvironmentVariable(Variable);
        string temporary = Path.Combine(Path.GetTempPath(), "mi-offline-" + Guid.NewGuid() + ".config");
        try
        {
            Environment.SetEnvironmentVariable(Variable, null);
            RunDomain(website, Path.Combine(website, "Web.config"), "actual");

            var document = XDocument.Load(Path.Combine(website, "Web.config"));
            var setting = document.Root.Element("connectionStrings").Elements("add")
                .Single(e => (string)e.Attribute("name") == "DefaultConnectionString");
            setting.SetAttributeValue("connectionString", Valid);
            document.Save(temporary);
            RunDomain(website, temporary, "fallback");

            setting.SetAttributeValue("providerName", "System.Data.SqlClient");
            document.Save(temporary);
            RunDomain(website, temporary, "wrong-provider");
            Console.WriteLine("PASS: all focused MI checks stayed offline; no SQL connection/token request.");
            return 0;
        }
        catch (Exception exception)
        {
            // No settings or supplied values in failure diagnostics.
            Console.Error.WriteLine("FAIL: offline check: " + exception.GetType().Name);
            return 1;
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, original);
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void RunDomain(string website, string config, string mode)
    {
        var domain = AppDomain.CreateDomain("MI-" + mode, null, new AppDomainSetup
        {
            ApplicationBase = Path.Combine(website, "bin"), ConfigurationFile = config
        });
        try
        {
            var check = (ManagedInstanceCheck)domain.CreateInstanceFromAndUnwrap(
                Assembly.GetExecutingAssembly().Location, typeof(ManagedInstanceCheck).FullName);
            check.Run(website, mode);
        }
        finally { AppDomain.Unload(domain); }
    }

    public void Run(string website, string mode)
    {
        var setting = ConfigurationManager.ConnectionStrings["DefaultConnectionString"];
        if (mode == "fallback")
        {
            using (var connection = SqlDatabaseConfiguration.CreateConnection())
                Require(connection.Database == "ExistingParts" && connection.State == ConnectionState.Closed,
                    "externally injected named connection works without an environment override");
            Environment.SetEnvironmentVariable(Variable, Valid.Replace("ExistingParts", "OverrideParts"));
            using (var connection = SqlDatabaseConfiguration.CreateConnection())
                Require(connection.Database == "OverrideParts", "process environment takes precedence over named connection");
            Environment.SetEnvironmentVariable(Variable, " ");
            RejectCurrent("blank override never falls back to valid named connection");
            Environment.SetEnvironmentVariable(Variable, null);
            return;
        }
        if (mode == "wrong-provider")
        {
            RejectCurrent("wrong named provider rejected");
            return;
        }

        Require(setting != null && setting.ProviderName == "Microsoft.Data.SqlClient" &&
            setting.ConnectionString == "", "checked-in named setting is empty and uses Microsoft provider");
        RejectCurrent("missing external configuration rejected");
        Reject(" ", "blank configuration rejected");
        Reject("not-a-connection-string", "malformed configuration rejected");
        Reject("Server=tcp:x;Authentication=unrecognized;", "invalid authentication value sanitized");
        Reject(Valid.Replace("Encrypt=True", "Encrypt=False"), "unencrypted transport rejected");
        Reject(Valid.Replace("TrustServerCertificate=False", "TrustServerCertificate=True"),
            "certificate bypass rejected");
        Reject(Valid.Replace("Authentication=Active Directory Managed Identity", "Integrated Security=True"),
            "Windows authentication rejected");
        Reject(Valid.Replace("Authentication=Active Directory Managed Identity", "User ID=user;Password=offline-sentinel"),
            "SQL password authentication rejected");
        Reject(Valid + "Password=offline-sentinel;", "password alongside managed identity rejected");
        Reject(Valid.Replace("Database=ExistingParts;", ""), "missing database rejected");
        Reject(Valid.Replace("Server=tcp:offline-mi.invalid,1433;", ""), "missing server rejected");
        Reject(Valid.Replace("tcp:offline-mi.invalid,1433", @"(localdb)\mssqllocaldb"), "LocalDB rejected");
        Reject(Valid.Replace("tcp:offline-mi.invalid,1433", "np:offline"), "non-TCP transport rejected");
        Reject(Valid + "User ID=not-a-client-guid;", "non-client-ID user rejected");
        Reject(Valid + @"AttachDbFilename=C:\offline.mdf;", "database attachment rejected");
        Reject(Valid + "User Instance=True;", "user instance rejected");
        Reject(Valid + "Persist Security Info=True;", "persisted security info rejected");
        Reject(Valid.Replace("Active Directory Managed Identity", "Active Directory Default"),
            "developer credential-chain fallback rejected");
        Reject(Valid.Replace("Active Directory Managed Identity", "Active Directory Integrated"),
            "integrated Entra authentication rejected");

        Environment.SetEnvironmentVariable(Variable, Valid);
        SqlDatabaseConfiguration.Validate();
        using (var connection = SqlDatabaseConfiguration.CreateConnection())
        {
            var options = new SqlConnectionStringBuilder(connection.ConnectionString);
            Require(options.Authentication == SqlAuthenticationMethod.ActiveDirectoryManagedIdentity &&
                options.UserID == "", "system-assigned managed identity");
            Require(options.Encrypt.Equals(SqlConnectionEncryptOption.Mandatory) &&
                !options.TrustServerCertificate, "mandatory encryption with certificate verification");
            Require(connection.State == ConnectionState.Closed, "validation does not open a connection");
        }
        Environment.SetEnvironmentVariable(Variable, Valid + "User ID=11111111-2222-3333-4444-555555555555;");
        using (var connection = SqlDatabaseConfiguration.CreateConnection())
            Require(new SqlConnectionStringBuilder(connection.ConnectionString).UserID ==
                "11111111-2222-3333-4444-555555555555", "user-assigned client ID preserved");
        Environment.SetEnvironmentVariable(Variable, Valid.Replace("Encrypt=True", "Encrypt=Strict"));
        using (var connection = SqlDatabaseConfiguration.CreateConnection())
            Require(new SqlConnectionStringBuilder(connection.ConnectionString).Encrypt.Equals(
                SqlConnectionEncryptOption.Strict), "strict encryption accepted");
        Environment.SetEnvironmentVariable(Variable, Valid);

        const string invariant = "Microsoft.Data.SqlClient";
        Require(DbProviderFactories.GetFactory(invariant) is SqlClientFactory, "actual ADO.NET provider registration");
        Require(DbConfiguration.DependencyResolver.GetService(typeof(DbProviderServices), invariant)
            is MicrosoftSqlProviderServices, "actual EF6 Microsoft SQL provider registration");
        using (var context = new CaldovaAssetManagerContext())
        {
            Require(context.Database.Connection is SqlConnection &&
                context.Database.Connection.Database == "ExistingParts", "actual shared context uses external MI connection");
            Require(!context.Configuration.LazyLoadingEnabled, "lazy loading policy unchanged");
            var initializer = DbConfiguration.DependencyResolver.GetService(
                typeof(IDatabaseInitializer<CaldovaAssetManagerContext>), null) as IDatabaseInitializer<CaldovaAssetManagerContext>;
            Require(initializer == null || initializer.GetType().Name.StartsWith("NullDatabaseInitializer"),
                "actual context initializer disabled");
            if (initializer != null) initializer.InitializeDatabase(context);
            Require(context.Database.Connection.State == ConnectionState.Closed,
                "disabled initializer does not access SQL");

            var builder = new DbModelBuilder();
            typeof(CaldovaAssetManagerContext).GetMethod("OnModelCreating",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, new object[] { builder });
            var model = builder.Build(new DbProviderInfo(invariant, "2012"));
            Require(model.StoreModel.EntityTypes.Any(t => t.Name == "Product"), "product mapping present");
            Require(model.StoreModel.EntityTypes.Any(t => t.Name.Contains("IdentityUser")), "Identity mapping present");
            Database.SetInitializer<DbContext>(null);
            using (var queryContext = new DbContext(context.Database.Connection, model.Compile(), false))
            {
                var sql = queryContext.Set<Product>().Where(p => p.ProductId == 7).ToString();
                Require(sql.Contains("[dbo].[Products]") && sql.Contains("[ProductId]"), "product SQL/key semantics");
                var identitySql = queryContext.Set<ApplicationUser>().Where(u => u.Id == "offline").ToString();
                Require(identitySql.Contains("[dbo].[AspNetUsers]"), "existing Identity SQL table semantics");
            }
            Require(context.Database.Connection.State == ConnectionState.Closed, "query translation stayed offline");
        }
        Require(typeof(CaldovaAssetManager.Startup).GetMethod("CreateAdminUser",
            BindingFlags.Static | BindingFlags.NonPublic) == null, "startup admin bootstrap removed from compiled assembly");
        Require(!File.ReadAllText(Path.Combine(website, "Global.asax.cs")).Contains("new CaldovaAssetManagerDbInitializer"),
            "sample initializer not registered at web startup");
        var xml = XDocument.Load(Path.Combine(website, "Web.config"));
        Require(xml.Root.Element("entityFramework").Element("defaultConnectionFactory") == null,
            "no checked-in LocalDB default factory");
        Require(File.Exists(Path.Combine(website, "bin", "Microsoft.Data.SqlClient.SNI.x64.dll")) &&
            File.Exists(Path.Combine(website, "bin", "Microsoft.Data.SqlClient.SNI.x86.dll")), "native SNI assets present");
        Environment.SetEnvironmentVariable(Variable, null);
    }

    private static void Reject(string value, string description)
    {
        Environment.SetEnvironmentVariable(Variable, value);
        RejectCurrent(description);
    }

    private static void RejectCurrent(string description)
    {
        try
        {
            using (var connection = SqlDatabaseConfiguration.CreateConnection()) { }
        }
        catch (ConfigurationErrorsException exception)
        {
            Require(exception.InnerException == null && !exception.Message.Contains("offline-sentinel") &&
                !exception.Message.Contains("not-a-connection-string"), description + " (sanitized)");
            return;
        }
        throw new InvalidOperationException(description);
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        Console.WriteLine("PASS: " + description);
    }
}
