using System;
using System.Collections.Generic;
using System.Text;

namespace EPA.Office365.oAuth
{
    public class AppSettingsAzureAd
    {
        /// <summary>
        /// Gets or Sets the Login endpoint for Azure identities
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Gets or sets the Tenant Id
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the Tenant Domain
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the application ID for Active Directory authentication. The Client ID is used by the application to uniquely identify itself to Azure AD.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret for Active Directory authentication. The ClientSecret is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        /// </summary>
        public string ClientSecret { get; set; }

        public string CallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the PostLogoutRedirectURI for Active Directory authentication. The Post Logout Redirect Uri is the URL where the user will be redirected after they have signed out
        /// </summary>
        public string PostLogoutRedirectURI { get; set; } = ConstantsAuthentication.GraphResourceId;

        /// <summary>
        /// Graph audience for web api
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Returns the full URI endpoint for Azure Authentication
        /// </summary>
        public string Authority
        {
            get
            {
                if (string.IsNullOrEmpty(Instance))
                    return string.Empty;
                var AADLogin = new Uri(Instance);
                var AuthorityUri = new Uri(AADLogin, TenantId).AbsoluteUri;
                return AuthorityUri;
            }
        }

        public string OAuthSignin => $"{Authority}/oauth2/token";

        /// <summary>
        /// SharePoint App ID
        /// </summary>
        public string SPClientID { get; set; }

        /// <summary>
        /// SharePoint App Secret/Key
        /// </summary>
        public string SPClientSecret { get; set; }

        /// <summary>
        /// v2 ADAL Client ID
        /// </summary>
        public string MSALClientID { get; set; }

        /// <summary>
        /// v2 ADAL Scopes 
        /// </summary>
        /// <example>
        /// openid profile email offline_access user.readbasic.all
        /// </example>
        public string[] MSALScopes { get; set; }
    }
}
