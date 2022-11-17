namespace OutlookRoomFinder.Core.Models.MSAL
{
    public class MsalConfiguration
    {
        public ConfigEnvironment Env { get; set; }

        public ConfigAuthOptions Auth { get; set; }
    }

    public class ConfigAuthOptions
    {
        public string ClientId { get; set; }

        public string Authority { get; set; }

        public bool? ValidateAuthority { get; set; }

        public string RedirectUri { get; set; }

        public string PostLogoutRedirectUri { get; set; }

        public bool? NavigateToLoginRequestUrl { get; set; }

        public string BaseWebApiUrl { get; set; }

        public string Audience { get; set; }

        public string AzureDomain { get; set; }
    }

    public class ConfigEnvironment
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public bool ReferrerIsIE { get; set; }
    }
}
