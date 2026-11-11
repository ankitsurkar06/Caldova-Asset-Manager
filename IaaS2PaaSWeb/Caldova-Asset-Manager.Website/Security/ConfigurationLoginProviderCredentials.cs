namespace CaldovaAssetManager.Security
{
    public class ConfigurationLoginProviderCredentials : ILoginProviderCredentials
    {
        public ConfigurationLoginProviderCredentials(string providerName)
        {
            var prefix = string.Format("Authentication.{0}.", providerName);
            if (!ExternalSecretConfiguration.IsEnabled(prefix + "Enabled"))
                return; // Disabled providers do not read credentials or contact Key Vault.

            Key = ExternalSecretConfiguration.GetRequiredSetting(prefix + "Key");
            Secret = ExternalSecretConfiguration.GetRequiredSecret(prefix + "SecretName");
            Use = true;
        }

        public string Key { get; set; }
        public string Secret { get; set; }
        public bool Use { get; protected set; }
    }
}