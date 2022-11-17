namespace OutlookRoomFinder.Core
{
    public class AppSettings : IAppSettings
    {
        public AppSettingsAzureAd AzureAd { get; set; }

        public AppSettingsExchange Exchange { get; set; }

        public AppSettingsGraph Graph { get; set; }

        public AppSettingsKeyVault KeyVault { get; set; }

        public string WebRootPath { get; set; }

        public string EnvironmentName { get; set; }

        public string DeployedVersion { get; set; }
    }
}
