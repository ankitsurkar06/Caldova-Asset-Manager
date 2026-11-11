using System;
using System.Configuration;
using Microsoft.Data.SqlClient;

namespace CaldovaAssetManager.Utils
{
    /// <summary>Secretless runtime connectivity; never creates or opens a database.</summary>
    public static class SqlDatabaseConfiguration
    {
        public const string ConnectionName = "DefaultConnectionString";
        public const string EnvironmentVariableName = "SQLCONNSTR_" + ConnectionName;

        public static SqlConnection CreateConnection()
        {
            // A present but empty override must not fall back to another destination.
            var value = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (value == null)
            {
                var setting = ConfigurationManager.ConnectionStrings[ConnectionName];
                if (setting == null || setting.ProviderName != "Microsoft.Data.SqlClient")
                    throw InvalidConfiguration();
                value = setting.ConnectionString;
            }

            SqlConnectionStringBuilder builder;
            try
            {
                builder = new SqlConnectionStringBuilder(value ?? string.Empty);
            }
            catch (ArgumentException)
            {
                // Do not include supplied values or the original exception in diagnostics.
                throw InvalidConfiguration();
            }

            Guid clientId;
            if (string.IsNullOrWhiteSpace(builder.DataSource) ||
                !builder.DataSource.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(builder.DataSource.Substring(4)) ||
                string.IsNullOrWhiteSpace(builder.InitialCatalog) ||
                builder.Authentication != SqlAuthenticationMethod.ActiveDirectoryManagedIdentity ||
                builder.IntegratedSecurity || !string.IsNullOrEmpty(builder.Password) ||
                (!string.IsNullOrEmpty(builder.UserID) && !Guid.TryParse(builder.UserID, out clientId)) ||
                builder.Encrypt.Equals(SqlConnectionEncryptOption.Optional) ||
                builder.TrustServerCertificate || builder.PersistSecurityInfo ||
                !string.IsNullOrEmpty(builder.AttachDBFilename) || builder.UserInstance)
                throw InvalidConfiguration();

            return new SqlConnection(builder.ConnectionString);
        }

        public static void Validate()
        {
            using (var connection = CreateConnection())
            {
                // Validate settings at startup, without token acquisition or network access.
            }
        }

        private static ConfigurationErrorsException InvalidConfiguration()
        {
            return new ConfigurationErrorsException(
                "Configure DefaultConnectionString externally for Azure SQL Managed Instance: " +
                "explicit tcp: server and database, Authentication=Active Directory Managed Identity, " +
                "Encrypt=True, TrustServerCertificate=False, no password or integrated security. " +
                "For a user-assigned identity, User ID must be its client ID.");
        }
    }
}
