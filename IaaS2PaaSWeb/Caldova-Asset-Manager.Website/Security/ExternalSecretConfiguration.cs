using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace CaldovaAssetManager.Security
{
    /// <summary>Host-supplied settings and managed-identity-only secret access.
    /// No configuration builders, developer credential chain or plaintext secret fallback.</summary>
    public static class ExternalSecretConfiguration
    {
        public static string GetSetting(string name)
        {
            try
            {
                // An explicitly empty host override must not fall back to a stale file value.
                return Environment.GetEnvironmentVariable("CALDOVA_" + name.Replace('.', '_').ToUpperInvariant())
                    ?? ConfigurationManager.AppSettings[name];
            }
            catch (Exception)
            {
                throw Error("External setting could not be read.");
            }
        }

        public static bool IsEnabled(string name)
        {
            var value = GetSetting(name);
            if (string.IsNullOrWhiteSpace(value))
                return false;
            bool enabled;
            if (!bool.TryParse(value, out enabled))
                throw Error("Feature switch must be true or false.");
            return enabled;
        }

        public static string GetRequiredSetting(string name)
        {
            var value = GetSetting(name);
            if (string.IsNullOrWhiteSpace(value))
                throw Error("An enabled feature is missing a required public setting.");
            return value;
        }

        public static string GetRequiredSecret(string referenceSetting)
        {
            return ResolveSecret(referenceSetting, FetchSecret);
        }

        private static string ResolveSecret(string referenceSetting, Func<Uri, string, string, string> fetch)
        {
            var name = GetSetting(referenceSetting);
            if (string.IsNullOrWhiteSpace(name) || !Regex.IsMatch(name, @"\A[a-zA-Z0-9-]{1,127}\z"))
                throw Error("An enabled feature requires a valid Key Vault secret name reference.");

            Uri vault;
            var address = GetSetting("KeyVault.Uri");
            if (!Uri.TryCreate(address, UriKind.Absolute, out vault) ||
                vault.Scheme != Uri.UriSchemeHttps || vault.Port != 443 ||
                !string.IsNullOrEmpty(vault.UserInfo) || !string.IsNullOrEmpty(vault.Query) ||
                !string.IsNullOrEmpty(vault.Fragment) || vault.AbsolutePath != "/" ||
                !Regex.IsMatch(vault.DnsSafeHost,
                    @"\A[a-zA-Z][a-zA-Z0-9-]{1,22}[a-zA-Z0-9]\.vault\.(azure\.net|azure\.cn|usgovcloudapi\.net)\z",
                    RegexOptions.IgnoreCase))
                throw Error("An enabled feature requires an HTTPS Azure Key Vault URI.");

            var clientId = GetSetting("KeyVault.ManagedIdentityClientId");
            Guid parsed;
            if (!string.IsNullOrEmpty(clientId) && (!Guid.TryParse(clientId, out parsed) || parsed == Guid.Empty))
                throw Error("Key Vault managed identity client ID must be a nonempty GUID or omitted.");

            try
            {
                // Versionless lookup: rotation takes effect at application restart.
                var value = fetch(vault, clientId, name);
                if (string.IsNullOrWhiteSpace(value))
                    throw Error("Key Vault returned an empty required secret.");
                return value;
            }
            catch (Exception)
            {
                // SDK/identity exceptions can contain endpoints, response bodies or sensitive data.
                // Never retain their message, inner exception or response in application telemetry.
                throw Error("Required Key Vault secret unavailable. Check identity, access, network and secret state externally.");
            }
        }

        private static string FetchSecret(Uri vault, string clientId, string name)
        {
            var identityOptions = new TokenCredentialOptions();
            identityOptions.Diagnostics.IsLoggingEnabled = false;
            identityOptions.Diagnostics.IsLoggingContentEnabled = false;
            var credential = new ManagedIdentityCredential(clientId, identityOptions);
            var options = new SecretClientOptions();
            options.Diagnostics.IsLoggingEnabled = false;
            options.Diagnostics.IsLoggingContentEnabled = false;
            options.Retry.MaxRetries = 1;
            options.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);
            var client = new SecretClient(vault, credential, options);
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                return ReadActiveSecret(client.GetSecret(name, cancellationToken: timeout.Token).Value);
            }
        }

        private static string ReadActiveSecret(KeyVaultSecret secret)
        {
            var now = DateTimeOffset.UtcNow;
            if (secret == null || secret.Properties.Enabled == false ||
                (secret.Properties.NotBefore.HasValue && secret.Properties.NotBefore.Value > now) ||
                (secret.Properties.ExpiresOn.HasValue && secret.Properties.ExpiresOn.Value <= now))
                throw Error("Required Key Vault secret is not active.");
            return secret.Value;
        }

        private static ConfigurationErrorsException Error(string message)
        {
            return new ConfigurationErrorsException(message + " See docs/KEY-VAULT-SECRETS.md. No setting values are included.");
        }
    }
}
