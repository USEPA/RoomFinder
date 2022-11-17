using EPA.Office365.Diagnostics;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Linq;
using System.Threading;

namespace EPA.SharePoint.SysConsole.Commands
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CSOM Context exceptions are COM level.")]
    public abstract class BaseSpoCommand<T> where T : ICommonOptions
    {
        public IAppSettings Settings { get; }
        public virtual T Opts { get; }

        protected BaseSpoCommand(T opts, IAppSettings settings)
        {
            Opts = opts ?? throw new ArgumentNullException(nameof(opts), "CommonOptions object is required.");
            Settings = settings ?? throw new ArgumentNullException(nameof(settings), "IAppSettings is required.");
            LoggerAvailable = true;
        }

        public int Run()
        {
            var result = -1;
            OnInit();

            if (SPConnection == null)
            {
                throw new InvalidOperationException(Office365.CoreResources.NoConnection);
            }

            if (ClientContext == null)
            {
                throw new InvalidOperationException(Office365.CoreResources.NoConnection);
            }


            Uri uri = new Uri(this.ClientContext.Url);
            var urlParts = uri.Authority.Split(new[] { '.' });
            BaseUri = string.Format("https://{0}.{1}.{2}", urlParts[0], urlParts[1], urlParts[2]);


            Settings.Commands.TenantShortName = Settings.Commands.TenantShortName.ToLower();
            Settings.AzureAd.SPClientID = this.SPConnection.IsAddInCredentials ? this.SPConnection.AddInCredentials.AppId : string.Empty;
            Settings.AzureAd.SPClientSecret = this.SPConnection.IsAddInCredentials ? this.SPConnection.AddInCredentials.AppKey : string.Empty;

            OnBeforeRun();

            try
            {
                if (SPOnlineConnection.CurrentConnection.MinimalHealthScore != -1)
                {
                    int healthScore = Utility.GetHealthScore(SPOnlineConnection.CurrentConnection.Url);
                    if (healthScore <= SPOnlineConnection.CurrentConnection.MinimalHealthScore)
                    {
                        result = OnRun();
                    }
                    else
                    {
                        if (SPOnlineConnection.CurrentConnection.RetryCount != -1)
                        {
                            int retry = 1;
                            while (retry <= SPOnlineConnection.CurrentConnection.RetryCount)
                            {
                                LogWarning(string.Format(Office365.CoreResources.Retry0ServerNotHealthyWaiting1seconds, retry, SPOnlineConnection.CurrentConnection.RetryWait, healthScore));
                                Thread.Sleep(SPOnlineConnection.CurrentConnection.RetryWait * 1000);
                                healthScore = Utility.GetHealthScore(SPOnlineConnection.CurrentConnection.Url);
                                if (healthScore <= SPOnlineConnection.CurrentConnection.MinimalHealthScore)
                                {
                                    result = OnRun();
                                    break;
                                }
                                retry++;
                            }
                            if (retry > SPOnlineConnection.CurrentConnection.RetryCount)
                            {
                                LogError(new Exception(Office365.CoreResources.HealthScoreNotSufficient), $"OpenError with SPConnection");
                            }
                        }
                        else
                        {
                            LogError(new Exception(Office365.CoreResources.HealthScoreNotSufficient), $"OpenError with no retrycount");
                        }
                    }
                }
                else
                {
                    result = OnRun();
                }
            }
            catch (Exception ex)
            {
                SPOnlineConnection.CurrentConnection.RestoreCachedContext();
                System.Diagnostics.Trace.TraceError("Cmdlet Exception {0}", ex.Message);
                LogError(new Exception(Office365.CoreResources.HealthScoreNotSufficient), $"OpenError");
            }

            OnEnd();

            return result;
        }

        public abstract void OnInit();

        public virtual void OnBeforeRun() { }

        public abstract int OnRun();

        public virtual void OnEnd() { }

        /// <summary>
        /// If WhatIf in arguments we should evaulate if it should run
        /// </summary>
        /// <param name="message"></param>
        /// <returns>
        /// (true) if WhatIf is null or false
        /// (false) if WhatIf is not null
        /// </returns>
        public bool ShouldProcess(string message)
        {
            var process = !(Opts.WhatIf ??= false);
            LogWarning(message);
            return process;
        }


        public SPOnlineConnection SPConnection
        {
            get { return SPOnlineConnection.CurrentConnection; }
        }

        public ClientContext ClientContext
        {
            get { return SPOnlineConnection.CurrentConnection.Context; }
        }

        internal static bool DisconnectCurrentService()
        {
            if (SPOnlineConnection.CurrentConnection == null)
                return false;
            SPOnlineConnection.CurrentConnection = null;
            return true;
        }

        #region Private Variables

        /// <summary>
        /// The base URI for the SP Site or Tenant
        /// </summary>
        internal string BaseUri { get; private set; }

        /// <summary>
        /// Represents the claim identifier prefix
        /// </summary>
        internal const string ClaimIdentifier = "i:0#.f|membership";

        /// <summary>
        /// the logger is available
        /// </summary>
        internal bool LoggerAvailable { get; private set; }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Take the URL and clean it
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal string UrlPattern(string url)
        {
            var surl = url.EnsureTrailingSlashLowered();
            return surl;
        }

        /// <summary>
        /// removes claim prefix from the user logon
        /// </summary>
        /// <param name="_user"></param>
        /// <returns></returns>
        internal string RemoveClaimIdentifier(string _user)
        {
            var _cleanedUser = _user;

            var _tmp = _user.Split(new char[] { '|' });
            if (_tmp.Length > 0)
            {
                _cleanedUser = _tmp.Last(); // remove claim identity
            }

            return _cleanedUser;
        }

        /// <summary>
        /// Parse the Site URL to return the region/sitetype object
        /// </summary>
        /// <param name="_siteUrl"></param>
        /// <returns></returns>
        protected virtual SPOSiteTemplate GetRegionSiteType(string _siteUrl)
        {
            var sitetemplate = new SPOSiteTemplate();
            sitetemplate.SeparateFormat(_siteUrl);
            return sitetemplate;
        }

        /// <summary>
        /// Will configure the encoded claim for the user  i:0#.f|membership|username@tenant.com
        /// </summary>
        /// <param name="emailOrUserName"></param>
        /// <returns></returns>
        protected virtual string EncodeUsername(string emailOrUserName)
        {
            return string.Format("{0}|{1}", ClaimIdentifier, emailOrUserName);
        }

        private string ClientsiteUrl { get; set; }

        /// <summary>
        /// Returns true if the URL is prefixed with the production tenant name
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <returns></returns>
        protected virtual bool IsProductionTenant(string siteUrl)
        {
            if (string.IsNullOrEmpty(siteUrl))
            {
                throw new ArgumentNullException(nameof(siteUrl));
            }

            if (string.IsNullOrEmpty(ClientsiteUrl))
            {
                ClientsiteUrl = Settings.Commands.TenantUrl;
            }

            return (siteUrl.ToLower().IndexOf(ClientsiteUrl) > -1);
        }

        private string TenantShortName { get; set; }

        /// <summary>
        /// parse the encoded claim and validate if if the string is an external user
        /// </summary>
        /// <param name="encodedUserName"></param>
        /// <returns></returns>
        internal bool IsExternalUserFilter(string encodedUserName)
        {
            if (string.IsNullOrEmpty(encodedUserName))
            {
                throw new ArgumentNullException(nameof(encodedUserName));
            }

            if (string.IsNullOrEmpty(TenantShortName))
            {
                TenantShortName = Settings.Commands.TenantShortName;
            }

            bool _filterFlag = true;

            // no email alias
            if (!(encodedUserName.ToLower().IndexOf("@") > -1))
            {
                return false;
            }
            // EXT with Tenant Identifier
            if (encodedUserName.ToLower().IndexOf($"#ext#@{TenantShortName}.onmicrosoft.com") > -1)
            {
                return true;
            }
            // Tenant Identifyer (Cloud Id)
            if (encodedUserName.ToLower().IndexOf($"{TenantShortName}.onmicrosoft.com") > -1)
            {
                return false;
            }
            // has EPA gov alias
            if (encodedUserName.ToLower().IndexOf("epa.gov") > -1)
            {
                return false;
            }
            // Internal Claim
            if (encodedUserName.ToLower().IndexOf("|rolemanager|") > -1)
            {
                return false;
            }
            // Everyone - Except
            if (encodedUserName.ToLower().IndexOf("\\_spo") > -1)
            {
                return false;
            }

            if (encodedUserName.ToLower().IndexOf("sharepoint\\system") > -1)
            {
                return false;
            }

            if (encodedUserName.ToLower().IndexOf("app@sharepoint") > -1)
            {
                return false;
            }

            if (encodedUserName.ToLower().IndexOf("c:0(.s|true") > -1)
            {
                return false;
            }

            if (encodedUserName.ToLower().IndexOf("c:0!.s|windows") > -1)
            {
                return false;
            }

            return _filterFlag;
        }

        /// <summary>
        /// internal member to hold the current user
        /// </summary>
        private string _currentUserInProcess = string.Empty;

        /// <summary>
        /// this should be valid based on pre authentication checks
        /// </summary>
        protected virtual string CurrentUserName
        {
            get
            {
                if (string.IsNullOrEmpty(_currentUserInProcess))
                {
                    _currentUserInProcess = SPOnlineConnection.CurrentConnection.GetActiveUsername();
                }
                return _currentUserInProcess;
            }
        }

        /// <summary>
        /// internal member to hold the current network credentials
        /// </summary>
        private System.Net.NetworkCredential _currentNetworkInProcess = null;

        /// <summary>
        /// this should the current network credentials
        /// </summary>
        protected virtual System.Net.NetworkCredential CurrentNetworkCredential
        {
            get
            {
                if (_currentNetworkInProcess == null)
                {
                    _currentNetworkInProcess = SPOnlineConnection.CurrentConnection?.GetActiveCredentials();
                }

                return _currentNetworkInProcess;
            }
        }

        #endregion

        protected virtual void WriteConsole(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Log: ERROR
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        protected virtual void LogError(Exception ex, string message, params object[] args)
        {
            if (LoggerAvailable)
            {
                Log.LogError(ex, string.Format(message, args));
            }

            if (string.IsNullOrEmpty(message))
            {
                System.Diagnostics.Trace.TraceError("Exception: {0}", ex.Message);
            }
            else
            {
                System.Diagnostics.Trace.TraceError(message, args);
            }
        }

        /// <summary>
        /// Log: DEBUG
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        protected virtual void LogDebugging(string message, params object[] args)
        {
            if (LoggerAvailable)
            {
                Log.LogDebug(string.Format(message, args));
            }
            System.Diagnostics.Trace.TraceInformation(message, args);
        }

        /// <summary>
        /// Writes a warning message to the cmdlet and logs to directory
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        protected virtual void LogWarning(string message, params object[] args)
        {
            if (LoggerAvailable)
            {
                Log.LogWarning(string.Format(message, args));
            }
            System.Diagnostics.Trace.TraceWarning(message, args);
        }

        /// <summary>
        /// Log: VERBOSE
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        protected virtual void LogVerbose(string message, params object[] args)
        {
            if (LoggerAvailable)
            {
                Log.LogInformation(string.Format(message, args));
            }
            System.Diagnostics.Trace.TraceInformation(message, args);
        }
    }
}
