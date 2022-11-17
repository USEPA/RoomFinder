using CommandLine;
using EPA.Office365;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Framework.Provisioning;
using EPA.SharePoint.SysConsole.HttpServices;
using Serilog;
using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("makeEPASiteProvisioner", HelpText = "Processes a site request into a full sharepoint site.")]
    public class MakeEPASiteProvisionerOptions : TenantCommandOptions
    {
        /// <summary>
        /// Tenant ID
        /// </summary>
        [Option("realm", Required = false, SetName = "Token")]
        public string Realm { get; set; }

        [Option("watch-directory", Required = true)]
        public string WatchDirectory { get; set; }

        /// <summary>
        /// Unique ID of the site request
        /// </summary>
        [Option("site-requestid", Required = true)]
        public int SiteRequestId { get; set; }

        /// <summary>
        /// lets leave the existing welcomepage intact
        /// </summary>
        [Option("overwrite-welcomepage", Required = false)]
        public bool OverwriteWelcomePage { get; set; }

        /// <summary>
        ///  lets leave the existing associated membership groups in place
        /// </summary>
        [Option("overwrite-groups", Required = false)]
        public bool OverwriteAssociatedGroups { get; set; }

        /// <summary>
        /// lets leave navigation links in place
        /// </summary>
        [Option("overwrite-navigation", Required = false)]
        public bool OverwriteNavigationNodes { get; set; }

        /// <summary>
        /// should the end process send an email message
        /// </summary>
        [Option("sendemail", Required = false)]
        public bool SendEmailMessage { get; set; }
    }

    public static class MakeEPASiteProvisionerOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this MakeEPASiteProvisionerOptions opts, IAppSettings appSettings)
        {
            var cmd = new MakeEPASiteProvisioner(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Query the site request and process the site provisioning response
    /// </summary>
    public class MakeEPASiteProvisioner : BaseSpoTenantCommand<MakeEPASiteProvisionerOptions>
    {
        public MakeEPASiteProvisioner(MakeEPASiteProvisionerOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        internal string TenantAdminUrl { get; set; }
        internal string ClientId { get; set; }
        internal string ClientSecret { get; set; }

        #endregion


        public override void OnBeforeRun()
        {
            if (!System.IO.Directory.Exists(Opts.WatchDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"Directory {Opts.WatchDirectory} could not be found");
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
            var TraceLogger = new DefaultUsageLogger(LogVerbose, LogWarning, LogError);


            var tenantAdminUrl = new Uri(TenantAdminUrl);
            var siteRequestUrl = Settings.Commands.SPOSiteRequestUrl;
            var additional = Settings.Commands.SPOAdditionalUsers;
            var additionalAdmins = new List<string>(additional.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));

            var provisioner = new EPAProvisioner(TraceLogger, ClientId, ClientSecret, Opts.Realm)
            {
            };

            provisioner.RelayEmailNotification(Opts.SendEmailMessage);
            provisioner.UpdateAdditionalAdministrators(additionalAdmins);
            provisioner.ExecuteProvisioner(Opts.WatchDirectory, tenantAdminUrl, siteRequestUrl, Opts.SiteRequestId, Opts.OverwriteWelcomePage, Opts.OverwriteAssociatedGroups, Opts.OverwriteNavigationNodes);

            LogVerbose("Provisioning Complete.");
            return 1;
        }
    }
}
