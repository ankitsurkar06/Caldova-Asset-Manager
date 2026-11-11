// Focused offline checks of the compiled implementation; no Azure/issuer/SQL requests.
using System;
using System.Configuration;
using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CaldovaAssetManager.Security;
using CaldovaAssetManager.Utils;

internal static class VerifySecrets
{
    private static int count;
    private const string Marker = "offline-sensitive-marker";
    private static void Check(bool result, string label)
    {
        if (!result) throw new Exception("FAILED: " + label);
        count++;
        Console.WriteLine("PASS: " + label);
    }
    private static void Set(string name, string value)
    {
        Environment.SetEnvironmentVariable("CALDOVA_" + name.Replace('.', '_').ToUpperInvariant(), value);
    }
    private static Exception Unwrap(Exception e)
    {
        while (e is TargetInvocationException) e = e.InnerException;
        return e;
    }
    private static void Reject(Action action, string label)
    {
        try { action(); }
        catch (Exception e)
        {
            e = Unwrap(e);
            Check(e is ConfigurationErrorsException && e.InnerException == null &&
                !e.ToString().Contains(Marker) &&
                !e.ToString().Contains("unit-vault") &&
                !e.ToString().Contains("unit-secret") &&
                e.Message.Contains("KEY-VAULT-SECRETS.md"), label);
            return;
        }
        throw new Exception("FAILED (no rejection): " + label);
    }
    private static string Resolve(Func<Uri, string, string, string> read)
    {
        return (string)typeof(ExternalSecretConfiguration).GetMethod("ResolveSecret",
            BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                new object[] { "Authentication.Facebook.SecretName", read });
    }
    public static int Main()
    {
        try
        {
            // Discard only this child process's inherited CALDOVA_* overrides.
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
                if (((string)entry.Key).StartsWith("CALDOVA_", StringComparison.OrdinalIgnoreCase))
                    Environment.SetEnvironmentVariable((string)entry.Key, null);
            var providers = new ConfigurationLoginProviders();
            Check(!providers.Facebook.Use && !providers.Google.Use &&
                !providers.Microsoft.Use && !providers.Twitter.Use, "all checked-in providers remain disabled");
            Check(!ConfigurationHelpers.GetBool("ShowRecommendations"), "recommendations stay disabled");
            using (var client = new HttpClientWrapper())
                Check(client.DefaultRequestHeaders.Authorization == null, "disabled API has no credential header");
            Check(ConfigurationManager.AppSettings["Authentication.Administrator.Password"] == null &&
                ConfigurationManager.AppSettings["Authentication.Administrator.UserName"] == null,
                "bootstrap settings absent");
            Check(ConfigurationManager.AppSettings["MachineLearning.AccountKey"] == null &&
                ConfigurationManager.AppSettings["Authentication.Facebook.Secret"] == null,
                "inline secret settings absent");

            Set("KeyVault.Uri", Marker);
            Set("Authentication.Facebook.SecretName", Marker);
            Check(!new ConfigurationLoginProviderCredentials("Facebook").Use,
                "disabled provider ignores unusable vault references");
            using (var client = new HttpClientWrapper())
                Check(client.DefaultRequestHeaders.Authorization == null,
                    "disabled recommendation ignores unusable vault settings");
            Set("Authentication.Facebook.Enabled", Marker);
            Reject(() => new ConfigurationLoginProviderCredentials("Facebook"), "malformed provider switch sanitized");
            Set("Authentication.Facebook.Enabled", "true");
            Reject(() => new ConfigurationLoginProviderCredentials("Facebook"), "enabled provider requires public client ID");
            Set("Authentication.Facebook.Key", "public-client");
            Set("Authentication.Facebook.SecretName", null);
            Reject(() => new ConfigurationLoginProviderCredentials("Facebook"), "enabled provider requires secret reference");
            Set("ShowRecommendations", Marker);
            Reject(() => ConfigurationHelpers.GetBool("ShowRecommendations"), "controller switch error sanitized");
            Reject(() => new HttpClientWrapper(), "client switch error sanitized");
            Set("ShowRecommendations", "true");
            Check(ConfigurationHelpers.GetBool("ShowRecommendations") &&
                ExternalSecretConfiguration.IsEnabled("ShowRecommendations"), "UI controller and client share host switch");
            Reject(() => new HttpClientWrapper(), "enabled API requires vault reference");

            Set("Authentication.Facebook.SecretName", "unit-secret");
            Set("KeyVault.Uri", "https://unit-vault.vault.azure.net/");
            Set("KeyVault.ManagedIdentityClientId", null);
            int calls = 0;
            Func<Uri, string, string, string> read = (uri, id, name) =>
            {
                Check(uri.Host == "unit-vault.vault.azure.net" &&
                    string.IsNullOrEmpty(id) && name == "unit-secret", "validated system-assigned lookup inputs");
                calls++;
                return Marker;
            };
            Check(Resolve(read) == Marker && calls == 1, "versionless lookup result preserved without plaintext fallback");
            string idValue = "11111111-2222-3333-4444-555555555555";
            Set("KeyVault.ManagedIdentityClientId", idValue);
            Check(Resolve((uri, id, name) => id) == idValue, "user-assigned client GUID retained");
            foreach (string badId in new[] { Marker, Guid.Empty.ToString(), "/subscriptions/resource-id" })
            {
                Set("KeyVault.ManagedIdentityClientId", badId);
                Reject(() => Resolve(read), "invalid user identity rejected");
            }
            Set("KeyVault.ManagedIdentityClientId", null);
            foreach (string address in new[] {
                Marker, "http://unit-vault.vault.azure.net", "https://unit-vault.vault.azure.net:444/",
                "https://unit-vault.vault.azure.net/secrets/name", "https://unit-vault.vault.azure.net/?x=" + Marker,
                "https://unit-vault.vault.azure.net/#" + Marker, "https://" + Marker + "@unit-vault.vault.azure.net/",
                "https://unit-vault.vault.azure.net.attacker.example/", "https://localhost/" })
            {
                Set("KeyVault.Uri", address);
                Reject(() => Resolve(read), "invalid vault destination rejected");
            }
            foreach (string suffix in new[] { "azure.net", "azure.cn", "usgovcloudapi.net" })
            {
                Set("KeyVault.Uri", "https://unit-vault.vault." + suffix + "/");
                Check(Resolve((uri, id, name) => "value") == "value", "supported Azure cloud vault root");
            }
            foreach (string name in new[] { "name/path", "name?query", "name_" + Marker, new string('a', 128) })
            {
                Set("Authentication.Facebook.SecretName", name);
                Reject(() => Resolve(read), "invalid secret name rejected");
            }
            Set("Authentication.Facebook.SecretName", "unit-secret");
            Reject(() => Resolve((uri, id, name) => { throw new Exception(Marker); }),
                "SDK failure stripped of message and inner exception");
            Reject(() => Resolve((uri, id, name) => " "), "empty resolved secret fails visibly");
            Reject(() => Resolve((uri, id, name) => null), "null resolved secret fails visibly");

            var activeReader = typeof(ExternalSecretConfiguration).GetMethod("ReadActiveSecret",
                BindingFlags.Static | BindingFlags.NonPublic);
            var active = new KeyVaultSecret("unit-secret", Marker);
            Check((string)activeReader.Invoke(null, new object[] { active }) == Marker,
                "active vault secret value preserved");
            active.Properties.Enabled = false;
            Reject(() => activeReader.Invoke(null, new object[] { active }), "disabled secret state rejected");
            active.Properties.Enabled = true;
            active.Properties.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(-1);
            Reject(() => activeReader.Invoke(null, new object[] { active }), "expired secret state rejected");
            active.Properties.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
            active.Properties.NotBefore = DateTimeOffset.UtcNow.AddHours(1);
            Reject(() => activeReader.Invoke(null, new object[] { active }), "future secret state rejected");
            Reject(() => activeReader.Invoke(null, new object[] { null }), "missing vault object rejected");

            // Construct real SDK objects but perform no token acquisition/network requests.
            var sdk = new SecretClient(new Uri("https://unit-vault.vault.azure.net/"),
                new ManagedIdentityCredential());
            Check(sdk != null && sdk.VaultUri.Host == "unit-vault.vault.azure.net",
                "real SDK assemblies and managed identity constructor load");
            Console.WriteLine("SUCCESS: " + count + " offline assertions; no live service checks.");
            return 0;
        }
        catch (Exception)
        {
            // Never dump exception data from secret boundary checks.
            Console.Error.WriteLine("FAILED: offline secret verification; inspect assertion labels.");
            return 1;
        }
    }
}
