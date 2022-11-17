using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using Serilog;
using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("SendEPAEmail", HelpText = "The function cmdlet will send an email using Exchange Online SMTP.")]
    public class SendEPAEmailOptions : CommonOptions
    {
        /// <summary>
        /// The site to search and query Term Sets
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// Represents the directory path for any JSON files for serialization
        /// </summary>
        [Option("emails", Required = true)]
        public IEnumerable<string> Emails { get; set; }

        [Option("subject", Required = true)]
        public string Subject { get; set; }

        [Option("body", Required = true)]
        public string Body { get; set; }
    }

    public static class SendEPAEmailExtension
    {
        public static int RunGenerateAndReturnExitCode(this SendEPAEmailOptions opts, IAppSettings appSettings)
        {
            var cmd = new SendEPAEmail(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class SendEPAEmail : BaseSpoCommand<SendEPAEmailOptions>
    {
        public SendEPAEmail(SendEPAEmailOptions opts, IAppSettings settings)
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

            var properties = new Microsoft.SharePoint.Client.Utilities.EmailProperties
            {
                To = Opts.Emails,
                Subject = Opts.Subject,
                Body = Opts.Body
            };

            Microsoft.SharePoint.Client.Utilities.Utility.SendEmail(this.ClientContext, properties);
            ClientContext.ExecuteQueryRetry();

            return 1;
        }
    }
}
