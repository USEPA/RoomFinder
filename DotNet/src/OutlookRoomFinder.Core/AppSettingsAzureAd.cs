namespace OutlookRoomFinder.Core
{
    public class AppSettingsAzureAd
    {
        /// <summary>
        /// Graph audience for web api
        /// </summary>
        public string Audience { get; set; }


        public string Authority
        {
            get
            {
                return $"{Instance}{TenantId}";
            }
        }

        public string CallbackPath { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Domain { get; set; }

        public string Instance { get; set; }

        public string TenantId { get; set; }

        public string OAuthSignin
        {
            get
            {
                return $"{Authority}/oauth2/token";
            }
        }
    }
}