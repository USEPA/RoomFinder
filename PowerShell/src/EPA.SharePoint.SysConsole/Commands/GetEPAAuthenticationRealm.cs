using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using Serilog;
using System;
using System.Net;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("GetEPAAuthenticationRealm", HelpText = "The function cmdlet will serialize the mappings and push them to sharepoint.")]
    public class GetEPAAuthenticationRealmOptions : CommonOptions
    {
        /// <summary>
        /// The site on which to operate
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

    }

    public static class GetEPAAuthenticationRealmOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPAAuthenticationRealmOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAAuthenticationRealm(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class GetEPAAuthenticationRealm : BaseSpoCommand<GetEPAAuthenticationRealmOptions>
    {
        public GetEPAAuthenticationRealm(GetEPAAuthenticationRealmOptions opts, IAppSettings settings)
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
            WebRequest request = WebRequest.Create(new Uri(Opts.SiteUrl) + "/_vti_bin/client.svc");
            request.Headers.Add("Authorization: Bearer ");

            try
            {
                using (request.GetResponse())
                {
                }
            }
            catch (WebException e)
            {
                var bearerResponseHeader = e.Response.Headers["WWW-Authenticate"];

                const string bearer = "Bearer realm=\"";
                var bearerIndex = bearerResponseHeader.IndexOf(bearer, StringComparison.Ordinal);

                var realmIndex = bearerIndex + bearer.Length;

                if (bearerResponseHeader.Length >= realmIndex + 36)
                {
                    var targetRealm = bearerResponseHeader.Substring(realmIndex, 36);

                    if (Guid.TryParse(targetRealm, out Guid realmGuid))
                    {
                        WriteConsole($"Realm: => {realmGuid}");
                    }
                }
            }

            return 1;
        }


    }
}
