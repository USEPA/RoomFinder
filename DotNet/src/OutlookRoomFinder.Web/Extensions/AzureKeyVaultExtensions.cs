using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using OutlookRoomFinder.Core;
using Serilog;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace OutlookRoomFinder.Web.Extensions
{
    public class AzureKeyVaultExtensions : IKeyVaultHelper
    {
        private readonly string vault;
        private readonly string clientId;
        private readonly string clientSecret;
        public ILogger Logger { get; }

        public AzureKeyVaultExtensions(IAppSettings appSettings, ILogger logger)
        {
            if (appSettings == null)
            {
                throw new ArgumentException("Configuration is missing Application Settings", nameof(appSettings));
            }

            vault = appSettings.KeyVault?.Vault;
            clientId = appSettings.KeyVault?.ClientId;
            clientSecret = appSettings.KeyVault?.ClientSecret;
            Logger = logger;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Rare usage.")]
        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                var secret = await GetKeyVaultClient().GetSecretAsync(vault, secretName);

                return secret.Value;
            }
            catch (Exception ex)
            {
                Logger.Logging(LogEventLevel.Error, $"GetKeyVaultClient error message {ex.Message}");
                return null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Rare usage.")]
        public async Task SetSecretAsync(string secretName, string secretValue)
        {
            try
            {
                await GetKeyVaultClient().SetSecretAsync(vault, secretName, secretValue);
            }
            catch (Exception ex)
            {
                Logger.Logging(LogEventLevel.Error, $"SetSecretAsync error message {ex.Message}");
            }
        }

        public static KeyVaultClient GetKeyVaultClientFromManagedIdentity()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            return new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        }

        private KeyVaultClient GetKeyVaultClient()
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                return new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync));
            }

            return GetKeyVaultClientFromManagedIdentity();
        }

        private async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var clientCredential = new ClientCredential(clientId, clientSecret);
            var authenticationContext = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var result = await authenticationContext.AcquireTokenAsync(resource, clientCredential);

            return result.AccessToken;
        }
    }
}
