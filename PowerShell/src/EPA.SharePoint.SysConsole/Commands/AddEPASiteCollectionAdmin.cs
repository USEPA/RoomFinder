using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using Serilog;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("AddEPASiteCollectionAdmin", HelpText = "The function will adds user as a Site Collection Admin for the respective site collection.")]
    public class AddEPASiteCollectionAdminOptions : TenantCommandOptions
    {
        /// <summary>
        /// The site add a site collection administrator
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        [Option('u', "user-name", Required = true)]
        public string UserName { get; set; }
    }

    public static class AddEPASiteCollectionAdminExtension
    {
        public static int RunGenerateAndReturnExitCode(this AddEPASiteCollectionAdminOptions opts, IAppSettings appSettings)
        {
            var cmd = new AddEPASiteCollectionAdmin(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Adds user as a Site Collection Admin for the respective site collection
    /// </summary>
    public class AddEPASiteCollectionAdmin : BaseSpoTenantCommand<AddEPASiteCollectionAdminOptions>
    {
        public AddEPASiteCollectionAdmin(AddEPASiteCollectionAdminOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var TenantUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            var realmId = this.ClientContext.Web.GetAuthenticationRealm();
            LogVerbose($"Connecting to Realm {realmId}");

            SetSiteAdmin(Opts.SiteUrl, Opts.UserName, true);

            return 1;
        }
    }
}
