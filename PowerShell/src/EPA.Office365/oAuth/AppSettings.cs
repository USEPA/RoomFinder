using Newtonsoft.Json;

namespace EPA.Office365.oAuth
{
    /// <summary>
    /// Domain Model for the Application Config
    /// </summary>
    public class AppSettings : IAppSettings
    {
        public AppSettingsAzureAd AzureAd { get; set; }

        public AppSettingsExchange Exchange { get; set; }

        public AppSettingsGraph Graph { get; set; }

        public AppSettingsKeyVault KeyVault { get; set; }

        public AppSettingsInsights ApplicationInsights { get; set; }

        public ConnectionStrings ConnectionStrings { get; set; }

        public AppSettingsLogging Logging { get; set; }

        public string EnvironmentName { get; set; }
        public string DeployedVersion { get; set; }

        [JsonProperty(PropertyName = "spoEPAAdalGovernance")]
        public AppSettingsGraph SpoEPAAdalGovernance { get; set; }

        [JsonProperty(PropertyName = "spoADALepaReporting")]
        public AppSettingsGraph SpoADALepaReporting { get; set; }

        [JsonProperty(PropertyName = "spoAddInEZFormAdmin")]
        public AppSettingsGraph SpoAddInEZFormAdmin { get; set; }

        [JsonProperty(PropertyName = "spoAddInMakeEPASite")]
        public AppSettingsGraph SpoAddInMakeEPASite { get; set; }

        [JsonProperty(PropertyName = "spoEpaCredentials")]
        public AppSettingsUser SpoEpaCredentials { get; set; }

        public AppSettingsCommands Commands { get; set; }
    }
}
