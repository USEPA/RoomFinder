using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using Serilog;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("GetEPAAppWebDetails", HelpText = "The function will return app instances for the specified site-url.")]
    public class GetEPAAppWebDetailsOptions : CommonOptions
    {
        /// <summary>
        /// The site to search and query Term Sets
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

    }

    public static class GetEPAAppWebDetailsOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPAAppWebDetailsOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAAppWebDetails(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class GetEPAAppWebDetails : BaseSpoCommand<GetEPAAppWebDetailsOptions>
    {
        public GetEPAAppWebDetails(GetEPAAppWebDetailsOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Opts.SiteUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            var apps = this.ClientContext.Web.GetAppInstances();
            this.ClientContext.Load(apps);
            this.ClientContext.ExecuteQueryRetry();

            foreach (var app in apps)
            {
                LogVerbose($"App {app.AppPrincipalId} with {app.Title}");
            }

            return 1;
        }
    }
}
