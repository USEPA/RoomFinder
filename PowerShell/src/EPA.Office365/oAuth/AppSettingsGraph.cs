using System;
using System.Collections.Generic;
using System.Text;

namespace EPA.Office365.oAuth
{
    public class AppSettingsGraph
    {
        /// <summary>
        /// Endpoint (v1.0 | Beta)
        /// </summary>
        public string Endpoint { get; set; } = "beta";

        /// <summary>
        /// Graph Client Id from AzureAD
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Graph Secret from AzureAD
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Endpoint for the Microsoft Azure AD endpoint
        /// </summary>
        public string DefaultEndpoint { get; set; }

        /// <summary>
        /// Default scope for MSAL, depends on Endpoint
        /// </summary>
        public string DefaultScope { get; set; }
    }
}
