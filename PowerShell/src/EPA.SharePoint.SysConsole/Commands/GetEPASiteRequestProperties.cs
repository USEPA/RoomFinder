using CommandLine;
using ConsoleTables;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using Serilog;
using System;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("GetEPASiteRequestProperties", HelpText = "The function cmdlet will return propeties required for the siterequest application to function correctly.")]
    public class GetEPASiteRequestPropertiesOptions : TenantCommandOptions
    {
        /// <summary>
        /// Tenant ID
        /// </summary>
        [Option("realm", Required = false, SetName = "Token")]
        public string Realm { get; set; }

        /// <summary>
        /// Contains the distinct azure ad group name
        /// </summary>
        [Option("ad-groupname", Required = true)]
        public string AzureADGroupName { get; set; }
    }

    public static class GetEPASiteRequestPropertiesOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPASiteRequestPropertiesOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPASiteRequestProperties(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }


    /// <summary>
    /// The function cmdlet will return propeties required for the siterequest application to function correctly. 
    /// The siterequest provisions sites and as such must know where the application, app catalog, and everyone group Tenant ID is
    /// </summary>
    /// <remarks>Filter requests by threshold date</remarks>
    public class GetEPASiteRequestProperties : BaseSpoTenantCommand<GetEPASiteRequestPropertiesOptions>
    {
        public GetEPASiteRequestProperties(GetEPASiteRequestPropertiesOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        internal string TenantAdminUrl { get; set; }
        internal string ClientId { get; set; }
        internal string ClientSecret { get; set; }

        #endregion

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
            // Ensure we have the root site URL

            TenantContext.EnsureProperties(ct => ct.RootSiteUrl);
            var rootSiteUrl = UrlPattern(TenantContext.RootSiteUrl);

            var appCatalogUri = TenantContext.GetAppCatalog();
            var tenantRootUri = new Uri(rootSiteUrl);
            var siteAppUri = new Uri(tenantRootUri, "/sites/SiteRequest");
            var siteRequestUrl = siteAppUri.AbsoluteUri;
            var _webUrl = UrlPattern(siteAppUri.PathAndQuery);
            var _appUrl = UrlPattern(appCatalogUri.PathAndQuery);
            var _groupId = string.Empty;


            var siteRequestAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
            using (var _spcontext = siteRequestAuthManager.GetAppOnlyAuthenticatedContext(siteRequestUrl, ClientId, ClientSecret))
            {

                var _site = _spcontext.Site;
                _spcontext.Load(_site,
                    sctx => sctx.Id,
                    sctx => sctx.AllowCreateDeclarativeWorkflow,
                    sctx => sctx.AllowDesigner,
                    sctx => sctx.AllowMasterPageEditing,
                    sctx => sctx.CanUpgrade,
                    sctx => sctx.Classification,
                    sctx => sctx.UpgradeInfo);

                var _web = _spcontext.Web;
                _spcontext.Load(_web,
                    wctx => wctx.Url,
                    wctx => wctx.ServerRelativeUrl,
                    wctx => wctx.AllProperties);
                _spcontext.ExecuteQueryRetry();


                var _users = _spcontext.LoadQuery(_web.SiteUsers.Where(u => u.Title == Opts.AzureADGroupName));
                var _groups = _spcontext.LoadQuery(_web.SiteGroups.Include(s => s.Users));
                _spcontext.ExecuteQueryRetry();
                foreach (var _group in _groups)
                {
                    if (_group.Users.Any())
                    {
                        var _user = _group.Users.FirstOrDefault(uctx => uctx.Title == Opts.AzureADGroupName);
                        if (_user != null)
                        {
                            _groupId = _user.LoginName;
                            break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(_groupId))
                {
                    var _user = _users.FirstOrDefault();
                    if (_user != null)
                    {
                        _groupId = _user.LoginName;
                    }
                }

            }


            var model = new SiteProperties[] {
                new SiteProperties
                {
                    DefaultHostUrl = rootSiteUrl,
                    WebAppCatalog = _webUrl,
                    AppCatalog = _appUrl,
                    EveryoneGroup = _groupId
                }
            };

            ConsoleTable.From(model).Write(Format.MarkDown);

            return 1;
        }

        internal class SiteProperties
        {
            public string DefaultHostUrl { get; set; }

            public string WebAppCatalog { get; set; }

            public string AppCatalog { get; set; }

            public string EveryoneGroup { get; set; }
        }

    }
}
