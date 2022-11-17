using System.Management.Automation;
using System.Net;

namespace EPA.SharePoint.PowerShell.Commands
{
    internal static class PSCredentialExtensions
    {
        public static PSCredential GetPSCredentials(this NetworkCredential credentials)
        {
            var psCredential = new PSCredential(credentials.UserName, credentials.SecurePassword);
            return psCredential;
        }


        /// <summary>
        /// Returns a SharePoint Online Credential given a certain name. Add the credential in the Windows Credential Manager and create a new Windows Credential. Then add a new GENERIC Credential. The name parameter in the method maps to the Internet or network address field.
        /// </summary>
        /// <param name="name">Name maps to internet or network address fields</param>
        /// <returns>Microsoft.SharePoint.Client.SharePointOnlineCredentials</returns>
        public static NetworkCredential GetSharePointOnlineCredential(string name)
        {
            var networkCredential = CredentialManager.GetCredential(name);
            if (networkCredential == null)
            {
                return null;
            }
            return new NetworkCredential(networkCredential.UserName, networkCredential.SecurePassword);
        }
    }
}
