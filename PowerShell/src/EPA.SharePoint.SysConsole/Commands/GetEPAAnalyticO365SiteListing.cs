using CommandLine;
using EPA.Office365.Database;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.SharePoint.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{

    [Verb("GetEPAAnalyticO365SiteListing", HelpText = "The function will query onedrive and sharepoint site collections.")]
    public class GetEPAAnalyticO365SiteListingOptions : TenantCommandOptions
    {
        [Option("upns", Required = true, SetName = "Token")]
        public IEnumerable<string> OneDriveUPNs { get; set; }

        [Option("sharepoint-urls", Required = true, SetName = "Token")]
        public IEnumerable<string> SharePointUrls { get; set; }
    }

    public static class GetEPAAnalyticO365SiteListingExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPAAnalyticO365SiteListingOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAAnalyticO365SiteListing(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class GetEPAAnalyticO365SiteListing : BaseSpoTenantCommand<GetEPAAnalyticO365SiteListingOptions>
    {
        public GetEPAAnalyticO365SiteListing(GetEPAAnalyticO365SiteListingOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        internal string TenantAdminUrl { get; set; }

        #endregion

        public override void OnInit()
        {
            TenantAdminUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantAdminUrl), null, Username, UserSecret, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            var webUri = new Uri(ClientContext.Url);

            TenantContext.EnsureProperties(tssp => tssp.RootSiteUrl);
            var TenantUrl = TenantContext.RootSiteUrl.EnsureTrailingSlashLowered();
            var MySiteTenantUrl = TenantUrl.Replace(".sharepoint.com", "-my.sharepoint.com");

            // connect to database
            var connectionstring = Settings.ConnectionStrings.AnalyticsConnection;
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
            dbContextOptionsBuilder.UseSqlServer(connectionstring);
            using var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options);

            var OneDriveUrls = new List<string>();

            // Create People Manager object to retrieve profile data
            var peopleManager = new Microsoft.SharePoint.Client.UserProfiles.PeopleManager(ClientContext);

            // Enumerate OneDrive sites
            foreach (var upn in Opts.OneDriveUPNs)
            {
                var userLoginName = $"{ClaimIdentifier}|{upn}";
                var UserProfile = peopleManager.GetPropertiesFor(userLoginName);
                ClientContext.Load(UserProfile);
                ClientContext.ExecuteQueryRetry();
                if (!string.IsNullOrEmpty(UserProfile?.Email)
                    && (UserProfile?.PersonalUrl ?? "").ToLower().IndexOf("person.aspx") > -1)
                {
                    OneDriveUrls.Add(UserProfile.PersonalUrl.ToLower().TrimEnd(new char[] { '/' }));
                }
            }

            // Process information into the Reporting Administrative Site
            StoreSites(_context, TenantUrl, MySiteTenantUrl, OneDriveUrls);


            return 1;
        }

        internal void StoreSites(AnalyticDbContext _context, string TenantUrl, string MySiteTenantUrl, List<string> OneDriveUrls)
        {
            var allUrls = Opts.SharePointUrls.Concat(OneDriveUrls).ToList();


            foreach (var url in allUrls.Select(s => s.ToLower()))
            {
                EntityTenantSiteListing siteList = null;

                if (_context.EntitiesSiteListing.Any(es => es.Url == url))
                {
                    siteList = _context.EntitiesSiteListing.FirstOrDefault(fd => fd.Url == url);
                    siteList.DateModified = DateTime.UtcNow;
                }
                else
                {
                    siteList = new EntityTenantSiteListing()
                    {
                        Url = url,
                        DateModified = DateTime.UtcNow
                    };
                    _context.EntitiesSiteListing.Add(siteList);
                }


                if (url.IndexOf(MySiteTenantUrl) > -1)
                {
                    siteList.SiteType = "OneDrive";
                }
                else
                {
                    siteList.SiteType = "SPO";
                }

                var rows = _context.SaveChanges();
                LogVerbose($"Saved {rows} rows.");
            }
        }


    }
}
