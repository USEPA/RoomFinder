namespace EPA.Office365
{
    /// <summary>
    /// Used to establish an oAuth v1 or v2 token
    /// </summary>
    public static class ConstantsAuthentication
    {
        /// <summary>
        /// Common end-point for Microsoft Online Services. You should no longer use https://login.windows.net
        /// </summary>
        public const string CommonAuthority = "https://login.microsoftonline.com/common/";

        public const string ActiveDirectoryAuthenticationServiceUrl = "https://login.microsoftonline.com/common/oauth2/authorize";

        public const string ActiveDirectorySignOutUrl = "https://login.microsoftonline.com/common/oauth2/logout";

        public const string ActiveDirectoryTokenServiceUrl = "https://login.microsoftonline.com/common/oauth2/token";

        public const string NameClaimType = "name";

        public const string IssuerClaim = "iss";

        /// <summary>
        /// Prefixed claim identifier
        /// </summary>
        public static string Issuer = "iss";

        public const string TenantAuthority = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

        public const string Authority = "https://login.microsoftonline.com/common/v2.0/";

        public const string AdminConsentFormat = "https://login.microsoftonline.com/{0}/adminconsent?client_id={1}&state={2}&redirect_uri={3}";
        
        /// <summary>
        /// SAML/AzureAD Claim identifier for the Azure AD Tenant
        /// </summary>
        public static string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// SAML/AzureAD Claim Identifier for the user/group ID
        /// </summary>
        public static string ObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        /// <summary>
        /// Inject into the Authority URI to ensure its a multi-tenant application
        /// </summary>
        public static string Common = "common";

        /// <summary>
        /// Multi-Tenant authentication admin consent enables Azure AD Administrators to accept the app
        /// </summary>
        public static string AdminConsent = "admin_consent";

        /// <summary>
        /// OAuth common endpoint supports Multi-Tenant authentication
        /// </summary>
        public static string AuthorityCommon = "https://login.windows.net/common/oauth2/token";

        /// <summary>
        /// OAuth endpoint for a specific tenant
        /// </summary>
        public static string AuthorityTenantFormat = "https://login.windows.net/{0}/oauth2/token?api-version=1.0";

        /// <summary>
        /// MSA supported endpoint
        /// </summary>
        public static string AuthorityFormat = "https://login.microsoftonline.com/{0}";

        /// <summary>
        /// Call back for Client token services
        /// </summary>
        public static string CallbackPath = "/signin-oidc";

        /// <summary>
        /// MS Graph EndPoint URI
        /// </summary>
        public static string GraphResourceId = "https://graph.microsoft.com";

        /// <summary>
        /// MS Graph API Endpoint
        /// </summary>
        public static string GraphApiVersion = "1.6";

        /// <summary>
        /// Office 365 management endpoint
        /// </summary>
        public static string O365ResourceId = "https://manage.office.com";
    }
}
