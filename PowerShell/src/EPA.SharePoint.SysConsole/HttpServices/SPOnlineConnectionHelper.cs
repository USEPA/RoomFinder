using EPA.SharePoint.SysConsole.PipeBinds;
using EPA.SharePoint.SysConsole.Models;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using System;
using System.Net;

namespace EPA.SharePoint.SysConsole.HttpServices
{
    /// <summary>
    /// Helper class to instantiate the proper authentication manager for onpremise, online
    /// </summary>
    public static class SPOnlineConnectionHelper
    {
        static SPOnlineConnectionHelper()
        {
        }

        /// <summary>
        /// Initiate SP Client Context with AppId/AppSecret
        /// </summary>
        /// <param name="url"></param>
        /// <param name="realm"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="minimalHealthScore"></param>
        /// <param name="retryCount"></param>
        /// <param name="retryWait"></param>
        /// <param name="requestTimeout"></param>
        /// <param name="skipAdminCheck"></param>
        /// <returns></returns>
        public static SPOnlineConnection InstantiateSPOnlineConnection(Uri url, string realm, string clientId, string clientSecret, int minimalHealthScore, int retryCount, int retryWait, int requestTimeout, bool skipAdminCheck = false)
        {
            var authManager = new OfficeDevPnP.Core.AuthenticationManager();
            if (realm == null)
            {
                realm = GetRealmFromTargetUrl(url);
            }

            var spoAddInKeys = new SPOAddInKeys()
            {
                Realm = realm,
                AppId = clientId,
                AppKey = clientSecret
            };

            var context = authManager.GetAppOnlyAuthenticatedContext(url.ToString(), realm, clientId, clientSecret);
            context.ApplicationName = Office365.CoreResources.ApplicationName;
            context.RequestTimeout = requestTimeout;

            var connectionType = ConnectionType.OnPrem;
            if (url.Host.ToUpperInvariant().EndsWith("SHAREPOINT.COM"))
            {
                connectionType = ConnectionType.O365;
            }
            if (skipAdminCheck == false)
            {
                if (IsTenantAdminSite(context))
                {
                    connectionType = ConnectionType.TenantAdmin;
                }
            }
            return new SPOnlineConnection(context, connectionType, minimalHealthScore, retryCount, retryWait, spoAddInKeys, url.ToString());
        }

        /// <summary>
        /// Pulls the stored credentials stored as Username[AppId] and Password[AppSecret]
        /// </summary>
        /// <param name="url"></param>
        /// <param name="credentials"></param>
        /// <param name="minimalHealthScore"></param>
        /// <param name="retryCount"></param>
        /// <param name="retryWait"></param>
        /// <param name="requestTimeout"></param>
        /// <param name="skipAdminCheck"></param>
        /// <returns></returns>
        internal static SPOnlineConnection InstantiateSPOnlineConnection(Uri url, CredentialPipeBind credentials, int minimalHealthScore, int retryCount, int retryWait, int requestTimeout, bool skipAdminCheck = false)
        {
            var appCredentials = credentials.Credential;
            var appId = appCredentials.UserName;
            var appSecret = appCredentials.Password;
            return SPOnlineConnectionHelper.InstantiateSPOnlineConnection(url, null, appId, appSecret, minimalHealthScore, retryCount, retryWait, requestTimeout, skipAdminCheck);
        }

        public static SPOnlineConnection InstantiateSPOnlineConnection(Uri url, NetworkCredential credentials, bool currentCredentials, int minimalHealthScore, int retryCount, int retryWait, int requestTimeout, bool skipAdminCheck = false)
        => InstantiateSPOnlineConnection(url, credentials.UserName, credentials.SecurePassword, currentCredentials, minimalHealthScore, retryCount, retryWait, requestTimeout, skipAdminCheck);

        public static SPOnlineConnection InstantiateSPOnlineConnection(Uri url, string UserName, string Password, bool currentCredentials, int minimalHealthScore, int retryCount, int retryWait, int requestTimeout, bool skipAdminCheck = false)
            => InstantiateSPOnlineConnection(url, UserName, (new NetworkCredential(UserName, Password)).SecurePassword, currentCredentials, minimalHealthScore, retryCount, retryWait, requestTimeout, skipAdminCheck);

        internal static SPOnlineConnection InstantiateSPOnlineConnection(Uri url, string UserName, System.Security.SecureString Password, bool currentCredentials, int minimalHealthScore, int retryCount, int retryWait, int requestTimeout, bool skipAdminCheck = false)
        {
            ClientContext context = new ClientContext(url.AbsoluteUri)
            {
                ApplicationName = Office365.CoreResources.ApplicationName,
                RequestTimeout = requestTimeout
            };
            
            var credentials = new NetworkCredential(UserName, Password);

            if (!currentCredentials)
            {
                try
                {
                    SharePointOnlineCredentials onlineCredentials = new SharePointOnlineCredentials(UserName, Password);
                    context.Credentials = onlineCredentials;
                    try
                    {
                        context.ExecuteQueryRetry();
                    }
                    catch (IdcrlException iex)
                    {
                        System.Diagnostics.Trace.TraceError("Authentication Exception {0}", iex.Message);
                        return null;
                    }
                    catch (WebException wex)
                    {
                        System.Diagnostics.Trace.TraceError("Authentication Exception {0}", wex.Message);
                        return null;
                    }
                    catch (ClientRequestException)
                    {
                        context.Credentials = new NetworkCredential(UserName, Password);
                    }
                    catch (ServerException)
                    {
                        context.Credentials = new NetworkCredential(UserName, Password);
                    }
                }
                catch (ArgumentException)
                {
                    // OnPrem?
                    context.Credentials = new NetworkCredential(UserName, Password);
                    try
                    {
                        context.ExecuteQueryRetry();
                    }
                    catch (ClientRequestException ex)
                    {
                        throw new Exception("Error establishing a connection", ex);
                    }
                    catch (ServerException ex)
                    {
                        throw new Exception("Error establishing a connection", ex);
                    }
                }

            }
            else
            {
                context.Credentials = credentials;
            }

            var connectionType = ConnectionType.OnPrem;
            if (url.Host.ToUpperInvariant().EndsWith("SHAREPOINT.COM"))
            {
                connectionType = ConnectionType.O365;
            }
            if (skipAdminCheck == false)
            {
                if (IsTenantAdminSite(context))
                {
                    connectionType = ConnectionType.TenantAdmin;
                }
            }
            return new SPOnlineConnection(context, connectionType, minimalHealthScore, retryCount, retryWait, credentials, url.ToString());
        }

        public static string GetRealmFromTargetUrl(Uri targetApplicationUri)
        {
            WebRequest request = WebRequest.Create(targetApplicationUri + "/_vti_bin/client.svc");
            request.Headers.Add("Authorization: Bearer ");

            try
            {
                using (request.GetResponse())
                {
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    return null;
                }

                string bearerResponseHeader = e.Response.Headers["WWW-Authenticate"];
                if (string.IsNullOrEmpty(bearerResponseHeader))
                {
                    return null;
                }

                const string bearer = "Bearer realm=\"";
                int bearerIndex = bearerResponseHeader.IndexOf(bearer, StringComparison.Ordinal);
                if (bearerIndex < 0)
                {
                    return null;
                }

                int realmIndex = bearerIndex + bearer.Length;

                if (bearerResponseHeader.Length >= realmIndex + 36)
                {
                    string targetRealm = bearerResponseHeader.Substring(realmIndex, 36);

                    if (Guid.TryParse(targetRealm, out Guid realmGuid))
                    {
                        return targetRealm;
                    }
                }
            }
            return null;
        }

        private static bool IsTenantAdminSite(ClientContext clientContext)
        {
            try
            {
                var tenant = new Tenant(clientContext);
                clientContext.ExecuteQueryRetry();
                return true;
            }
            catch (ClientRequestException)
            {
                return false;
            }
            catch (ServerException)
            {
                return false;
            }
        }

    }
}
