using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System;

namespace EPA.SharePoint.SysConsole.Extensions
{
    public static class AzureKeyVaultExtensions
    {
        private static string AzureKeyVaultKey { get; } = "KeyVault";
        private static string AzureKeyVaultUrlKey { get; } = "Vault";

        public static IConfigurationBuilder AddAzureKeyVaultIfAvailable(this IConfigurationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var Configuration = builder.Build();

            var keyVaultSection = Configuration.GetSection(AzureKeyVaultKey);

            var vaultAddress = keyVaultSection[AzureKeyVaultUrlKey];
            if (string.IsNullOrEmpty(vaultAddress))
            {
                return builder;
            }

            var clientId = keyVaultSection["ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                // Try to access the Key Vault utilizing the Managed Service Identity of the running resource/process
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var vaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                builder.AddAzureKeyVault(vaultAddress, vaultClient, new DefaultKeyVaultSecretManager());
            }
            else
            {
                // Allow to override the MSI or for local dev
                builder.AddAzureKeyVault(vaultAddress, clientId, keyVaultSection["ClientSecret"]);
            }

            return builder;
        }
    }
}
