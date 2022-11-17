using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace OutlookRoomFinder.Web.Extensions
{
    public static class AzureKeyVaultBuilderExtension
    {
        private static string AzureKeyVaultKey { get; } = "KeyVault";
        private static string AzureKeyVaultUrlKey { get; } = "Vault";

        public static IConfigurationBuilder AddAzureKeyVaultIfAvailable(this IConfigurationBuilder builder)
        {
            var configurationRoot = builder.Build();
            var keyVaultConfigurationSection = configurationRoot.GetSection(AzureKeyVaultKey);

            string clientId = keyVaultConfigurationSection["ClientId"];
            string vaultUrl = keyVaultConfigurationSection[AzureKeyVaultUrlKey];

            if (string.IsNullOrEmpty(vaultUrl))
            {
                return builder;
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                // Try to access the Key Vault utilizing the Managed Service Identity of the running resource/process   
                builder.AddAzureKeyVault(vaultUrl, AzureKeyVaultExtensions.GetKeyVaultClientFromManagedIdentity(), new DefaultKeyVaultSecretManager());
            }
            else
            {
                // Allow to override the MSI or for local dev
                builder.AddAzureKeyVault(vaultUrl, clientId, keyVaultConfigurationSection["ClientSecret"]);
            }

            return builder;
        }
    }
}
