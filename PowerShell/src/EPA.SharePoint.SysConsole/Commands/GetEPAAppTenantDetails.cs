using CommandLine;
using ConsoleTables;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.Online.SharePoint.TenantAdministration.Internal;
using Microsoft.SharePoint.Client;
using Serilog;
using System;
using System.Data;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("getEPAAppTenantDetails", HelpText = "The function will make a connection and write tenant details to the console.")]
    public class GetEPAAppTenantDetailsOptions : CommonOptions
    {
        /// <summary>
        /// The site to search and query Term Sets
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        [Option("app-name", Required = false)]
        public string AppInfoName { get; set; }
    }

    public static class GetEPAAppTenantDetailsOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPAAppTenantDetailsOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAAppTenantDetails(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class GetEPAAppTenantDetails : BaseSpoCommand<GetEPAAppTenantDetailsOptions>
    {
        public GetEPAAppTenantDetails(GetEPAAppTenantDetailsOptions opts, IAppSettings settings)
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

            if (!string.IsNullOrEmpty(Opts.AppInfoName))
            {

                var tenant = new Tenant(this.ClientContext);
                var appinfo = tenant.GetAppInfoByName(Opts.AppInfoName);
                this.ClientContext.Load(appinfo);
                this.ClientContext.ExecuteQueryRetry();
            }


            var servicePrincipal = new SPOWebAppServicePrincipal(ClientContext);
            var permissionGrants = servicePrincipal.PermissionGrants;
            var requests = servicePrincipal.PermissionRequests;

            ClientContext.Load(servicePrincipal);
            ClientContext.Load(permissionGrants);
            ClientContext.Load(requests);
            ClientContext.ExecuteQueryRetry();

            ConsoleTable.From(permissionGrants.Select(g => new TenantServicePrincipalPermissionGrant(g)));

            return 1;
        }

        internal class TenantServicePrincipalPermissionGrant
        {
            public string ClientId { get; set; }
            public string ConsentType { get; set; }
            public string ObjectId { get; set; }
            public string Resource { get; set; }
            public string ResourceId { get; set; }
            public string Scope { get; set; }

            public TenantServicePrincipalPermissionGrant(SPOWebAppServicePrincipalPermissionGrant grant)
            {
                ClientId = grant.ClientId;
                ConsentType = grant.ConsentType;
                ObjectId = grant.ObjectId;
                Resource = grant.Resource;
                ResourceId = grant.ResourceId;
                Scope = grant.Scope;
            }
        }
    }
}
