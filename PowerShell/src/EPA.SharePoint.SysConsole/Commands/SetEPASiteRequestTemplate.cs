using CommandLine;
using EPA.Office365;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Framework.Provisioning;
using EPA.SharePoint.SysConsole.HttpServices;
using Serilog;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("SetEPASiteRequestTemplate", HelpText = "Resets a site collection to a specific template if it shifts from original scope.")]
    public class SetEPASiteRequestTemplateOptions : TenantCommandOptions
    {
        [Option("spo-users", Required = true)]
        public string SPOAdditionalUsers { get; set; }

        [Option("watch-directory", Required = true)]
        public string WatchDirectory { get; set; }

        [Option("realm", Required = true)]
        public string Realm { get; set; }

        [Option("spo-tenanturl", Required = true)]
        public string SPOTenantUrl { get; set; }

        [Option("spo-siterequesturl", Required = true)]
        public string SPOSiteRequestUrl { get; set; }

        [Option("pnp-templateid", Required = true)]
        public string PnPTemplateId { get; set; }

        [Option("pnp-templatefile", Required = true)]
        public string PnPTemplateFile { get; set; }

        [Option("pnp-templatepages", Required = true)]
        public string PnPTemplatePages { get; set; }
    }

    public static class SetEPASiteRequestTemplateOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this SetEPASiteRequestTemplateOptions opts, IAppSettings appSettings)
        {
            var cmd = new SetEPASiteRequestTemplate(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will set the site structure based on the specified PnP Template files
    /// </summary>
    public class SetEPASiteRequestTemplate : BaseSpoTenantCommand<SetEPASiteRequestTemplateOptions>
    {
        public SetEPASiteRequestTemplate(SetEPASiteRequestTemplateOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private Variables

        internal string TenantAdminUrl { get; set; }
        internal string ClientId { get; set; }
        internal string ClientSecret { get; set; }
        internal string ReleaseVersion { get; } = "20171130.1";

        #endregion

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();
            if (!System.IO.Directory.Exists(Opts.WatchDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"Could not find directory {Opts.WatchDirectory}");
            }
        }

        public override void OnInit()
        {
            TenantAdminUrl = Settings.Commands.TenantAdminUrl;
            ClientId = Settings.SpoAddInMakeEPASite.ClientId;
            ClientSecret = Settings.SpoAddInMakeEPASite.ClientSecret;

            LogVerbose($"AppCredential connecting and setting Add-In keys");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantAdminUrl), null, ClientId, ClientSecret, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            var ilogger = new DefaultUsageLogger(LogVerbose, LogWarning,
                (Exception ex, string msg, object[] margs) =>
                {
                    System.Diagnostics.Trace.TraceError(msg, margs);
                    LogError(ex, "Exception {0}", ex.StackTrace);

                    if (!string.IsNullOrEmpty(msg))
                    {
                        LogWarning(msg, margs);
                    }
                });

            try
            {

                if (this.SPConnection.IsAddInCredentials)
                {
                    var ClientId = this.SPConnection.AddInCredentials.AppId;
                    var ClientSecret = this.SPConnection.AddInCredentials.AppKey;

                    var siteStructure = new SiteRequestStructure();

                    var siteRequestUri = new Uri(Opts.SPOSiteRequestUrl);
                    var siteRequestAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
                    using var siteRequestCtx = siteRequestAuthManager.GetAppOnlyAuthenticatedContext(siteRequestUri.AbsoluteUri, ClientId, ClientSecret);
                    siteStructure.ApplyTemplate(Opts.WatchDirectory, ilogger, siteRequestCtx, Opts.PnPTemplateFile, Opts.PnPTemplatePages, Opts.PnPTemplateId);
                }
                else
                {
                    ilogger.LogWarning($"Failed to connect, you must specify an AppCredential to continue.");
                }
            }
            catch(Exception ex)
            {
                ilogger.LogError(ex, $"Failed to provision via the template {Opts.PnPTemplateFile} in watcher {Opts.WatchDirectory}");
                return -1;
            }

            return 1;
        }
    }
}
