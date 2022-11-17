using Microsoft.SharePoint.Client;
using System;
using System.Security;

namespace EPA.SharePoint.SysConsole.Extensions
{
    /// <summary>
    /// Assist in creating SharePoint Credentials
    /// </summary>
    public static class CredentialExtensions
    {

        /// <summary>
        /// Read plain text password into Secure Credentials
        /// </summary>
        /// <param name="_username"></param>
        /// <param name="_password"></param>
        /// <returns></returns>
        public static SharePointOnlineCredentials GetCredentials(string _username, string _password)
        {
            SecureString passWord = new SecureString();
            foreach (char c in _password.ToCharArray()) passWord.AppendChar(c);
            var siteCredentials = new SharePointOnlineCredentials(_username, passWord);
            return siteCredentials;
        }

        /// <summary>
        /// Builds out a cookie container to issue oAuth connections
        /// </summary>
        /// <param name="webUri"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static System.Net.CookieContainer GetAuthCookies(this Uri webUri, string userName, string password)
        {
            var credentials = GetCredentials(userName, password);
            var authCookie = credentials.GetAuthenticationCookie(webUri);
            var cookieContainer = new System.Net.CookieContainer();
            cookieContainer.SetCookies(webUri, authCookie);
            return cookieContainer;
        }





    

    }
}
