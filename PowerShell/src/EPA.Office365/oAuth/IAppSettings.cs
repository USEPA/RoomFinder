namespace EPA.Office365.oAuth
{
    /// <summary>
    /// Represents the Application Config AppSettings
    /// </summary>
    public interface IAppSettings
    {
        AppSettingsAzureAd AzureAd { get; set; }

        AppSettingsExchange Exchange { get; set; }

        AppSettingsGraph Graph { get; set; }

        AppSettingsKeyVault KeyVault { get; set; }

        AppSettingsInsights ApplicationInsights { get; set; }

        ConnectionStrings ConnectionStrings { get; set; }

        AppSettingsLogging Logging { get; set; }

        string EnvironmentName { get; set; }
        string DeployedVersion { get; set; }

        AppSettingsGraph SpoEPAAdalGovernance { get; set; }

        AppSettingsGraph SpoADALepaReporting { get; set; }

        AppSettingsGraph SpoAddInEZFormAdmin { get; set; }

        AppSettingsGraph SpoAddInMakeEPASite { get; set; }

        AppSettingsUser SpoEpaCredentials { get; set; }

        AppSettingsCommands Commands { get; set; }
    }
}
