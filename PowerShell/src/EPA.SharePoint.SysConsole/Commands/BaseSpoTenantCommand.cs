using EPA.Office365;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.Online.SharePoint.TenantManagement;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Commands
{
    public abstract class BaseSpoTenantCommand<T> : BaseSpoCommand<T> where T : ITenantCommandOptions
    {
        protected BaseSpoTenantCommand(T opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        internal Tenant BaseTenant { get; private set; }
        /// <summary>
        /// Create client context to tenant admin
        /// </summary>
        public Tenant TenantContext
        {
            get
            {
                if (BaseTenant == null)
                {
                    BaseTenant = new Tenant(this.ClientContext);
                }
                return BaseTenant;
            }
        }

        internal Office365Tenant _officeTenant { get; private set; }

        /// <summary>
        /// Initializes a Office365 Tenant Context
        /// </summary>
        public Office365Tenant OfficeTenantContext
        {
            get
            {
                if (_officeTenant == null)
                {
                    _officeTenant = new Office365Tenant(this.ClientContext);
                }
                return _officeTenant;
            }
        }

        public override void OnBeforeRun()
        {
            var currentRuntimeDateLog = DateTime.UtcNow;

            if (!Opts.LogDateTime.HasValue)
            {
                Opts.LogDateTime = currentRuntimeDateLog;
            }

            SPOnlineConnection.CurrentConnection.CacheContext();

            Uri uri = new Uri(this.ClientContext.Url);
            var urlParts = uri.Authority.Split(new[] { '.' });
            if (!urlParts[0].EndsWith("-admin"))
            {
                var adminUrl = string.Format("https://{0}-admin.{1}.{2}", urlParts[0], urlParts[1], urlParts[2]);

                SPOnlineConnection.CurrentConnection.Context = this.ClientContext.Clone(adminUrl);
            }


            if (TenantContext.Context == null)
            {
                this.ClientContext.Load(TenantContext);
                this.ClientContext.ExecuteQueryRetry();
            }
        }

        public override void OnEnd()
        {
            SPOnlineConnection.CurrentConnection.RestoreCachedContext();
            if (TenantContext.Context == null)
            {
                TenantContext.Context.Dispose();
            }
        }


        /// <summary>
        /// Sets the site collection administrator for the activity
        /// </summary>
        /// <param name="_siteUrl">The relative url to the site collection</param>
        /// <param name="userNameWithoutClaims">Provide the username without the claim prefix</param>
        /// <param name="isSiteAdmin">(OPTIONAL) true to set the user as a site collection administrator</param>
        protected virtual void SetSiteAdmin(string _siteUrl, string userNameWithoutClaims, bool isSiteAdmin = false)
        {
            var claimProviderUserName = userNameWithoutClaims;
            if (claimProviderUserName.IndexOf(ClaimIdentifier, StringComparison.CurrentCultureIgnoreCase) <= -1)
            {
                claimProviderUserName = string.Format("{0}|{1}", ClaimIdentifier, userNameWithoutClaims);
            }

            if (isSiteAdmin)
            {
                LogVerbose("Granting access to {0} for {1}", _siteUrl, claimProviderUserName);
            }
            else
            {
                LogVerbose("Removing access to {0} for {1}", _siteUrl, claimProviderUserName);
            }

            try
            {
                TenantContext.SetSiteAdmin(_siteUrl, claimProviderUserName, isSiteAdmin);
                TenantContext.Context.ExecuteQueryRetry();
            }
            catch (Exception e)
            {
                LogError(e, "Failed to set {0} site collection administrator permissions for site:{1}", userNameWithoutClaims, _siteUrl);
            }
        }

        /// <summary>
        /// Returns all site collections in the tenant
        /// </summary>
        /// <param name="includeProperties">Include all Site Collection properties</param>
        /// <returns></returns>
        public List<SPOSiteCollectionModel> GetSiteCollections(bool includeProperties = false)
        {
            var urls = TenantContext.GetSPOSiteCollections(includeProperties);
            LogVerbose($"Found URLs {urls.Count}");
            return urls;
        }

        /// <summary>
        /// Returns all onedrive for business profile collections in the tenant
        /// </summary>
        /// <param name="MySiteUrl"></param>
        /// <param name="includeProperties"></param>
        /// <returns></returns>
        public List<OD4BSiteCollectionModel> GetOneDriveSiteCollections(string MySiteUrl, bool includeProperties = false)
        {
            var ilogger = new DefaultUsageLogger(LogVerbose, LogWarning, LogError);
            LogWarning($"Querying OneDrive for Business profiles.  This may take up to 30 mins per 10K users.");
            var urls = this.ClientContext.GetOneDriveSiteCollections(ilogger, MySiteUrl, includeProperties);
            ilogger.LogInformation($"Found URLs {urls.Count}");
            return urls;
        }
    }
}