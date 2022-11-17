namespace OutlookRoomFinder.Core
{
    /// <summary>
    /// Provides application level settings
    /// </summary>
    public interface IAppSettings
    {
        AppSettingsAzureAd AzureAd { get; set; }

        AppSettingsExchange Exchange { get; set; }

        AppSettingsGraph Graph { get; set; }

        AppSettingsKeyVault KeyVault { get; set; }

        string WebRootPath { get; set; }

        string EnvironmentName { get; set; }

        string DeployedVersion { get; set; }
    }
}
