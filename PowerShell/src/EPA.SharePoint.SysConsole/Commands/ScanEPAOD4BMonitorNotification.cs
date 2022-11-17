using CommandLine;
using EPA.Office365;
using EPA.Office365.Database;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEPAOD4BMonitorNotification", HelpText = "Scan/Identify ODFB Subsites")]
    public class ScanEPAOD4BMonitorNotificationOptions : TenantCommandOptions
    {
        [Option("log-directory", Required = true)]
        public string LogDirectory { get; set; }
    }

    public static class ScanEPAOD4BMonitorNotificationExtension
    {
        /// <summary>
        /// Will execute the scan for OneDrive changes
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this ScanEPAOD4BMonitorNotificationOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPAOD4BMonitorNotification(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Scan-EPAOD4BMonitorNotification 
    ///     will query the user profile service, 
    ///     opening each MySite, 
    ///     querying subwebs along with Lists to compile a report
    /// </summary>
    public class ScanEPAOD4BMonitorNotification : BaseSpoTenantCommand<ScanEPAOD4BMonitorNotificationOptions>
    {
        public ScanEPAOD4BMonitorNotification(ScanEPAOD4BMonitorNotificationOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region private

        private const int Retention = 0; //// // #  in days

        private const int LastAlert = 80; // // // #  in days

        private AnalyticDbContext _context { get; set; }

        #endregion

        public override void OnInit()
        {
            var TenantUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();
            if (!System.IO.Directory.Exists(Opts.LogDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"Directory {Opts.LogDirectory} not found.");
            }
        }

        public override int OnRun()
        {
            // Specifies the URL for your organization's SPO admin service
            var reportSiteURL = "https://usepa.sharepoint.com/sites/OEI_Custom/ODBReview";
            var emailNotificationListName = "Notification Queue";
            var ilogger = new DefaultUsageLogger(LogVerbose, LogWarning, LogError);


            TenantContext.EnsureProperties(tssp => tssp.RootSiteUrl);
            var TenantUrl = TenantContext.RootSiteUrl.EnsureTrailingSlashLowered();
            var MySiteTenantUrl = TenantUrl.Replace(".sharepoint.com", "-my.sharepoint.com");


            //// // #  Connect to report site and get & groups list
            var reportSiteContext = this.ClientContext.Clone(reportSiteURL);

            var reportWeb = reportSiteContext.Web;
            var emailNotificationList = reportWeb.Lists.GetByTitle(emailNotificationListName);

            reportSiteContext.Load(reportWeb);
            reportSiteContext.Load(emailNotificationList);
            reportSiteContext.ExecuteQuery();


            var connectionstring = Settings.ConnectionStrings.AnalyticsConnection;
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
            dbContextOptionsBuilder.UseSqlServer(connectionstring);
            var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options);


            //// // #   enum all ODB sites and logs subsites
            EnumODBSites(reportSiteContext, ilogger, MySiteTenantUrl);


            // // // #   enum and remove reported subsites that haven't been 
            ProcessSitesList(reportSiteContext, emailNotificationList);

            return 1;
        }

        /// <summary>
        /// Function: process ODB subsite ... record if not a blog
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userName"></param>
        private void ProcessSubsites(ClientContext reportSiteContext, string url, string userName)
        {
            var Site = this.ClientContext.Clone(url);

            var tWeb = Site.Web;
            Site.Load(tWeb);
            var listQuery = Site.LoadQuery(Site.Web.Lists.Include(s => s.Title));
            Site.Load(tWeb.Webs, cwebs => cwebs.Include(cwebt => cwebt.Url));
            Site.ExecuteQueryRetry();

            LogVerbose($"{tWeb.WebTemplate} -- {tWeb.Url}");

            try
            {
                LogVerbose($"Adding List item {tWeb.Title} mainUserInfo {userName}");

                EntityAnalyticsUserProfilesSubsites subsite = null;
                if (_context.EntitiesUserProfileSubsites.Any(s => s.Url == url))
                {
                    subsite = _context.EntitiesUserProfileSubsites.FirstOrDefault(s => s.Url == url);
                }
                else
                {
                    subsite = new EntityAnalyticsUserProfilesSubsites()
                    {
                        Url = url,
                        SiteOwner = userName
                    };
                    _context.EntitiesUserProfileSubsites.Add(subsite);
                }

                subsite.Status = "Pending";
                subsite.Title = tWeb.Title;
                subsite.SiteTemplate = tWeb.WebTemplate;
                subsite.LastModified = tWeb.LastItemModifiedDate;
                subsite.DateCreated = tWeb.Created;
                subsite.ListCount = (listQuery != null) ? listQuery.Count() : 0;
                subsite.FlaggedCount = (subsite?.FlaggedCount > 0) ? ++subsite.FlaggedCount : 1;

                var rowchanges = _context.SaveChanges();
                LogVerbose($"Saved {rowchanges} rows..");
            }
            catch (Exception ex)
            {
                LogError(ex, "ErrorMessage {0}", ex.Message);
            }


            foreach (var webin in tWeb.Webs)
            {
                ProcessSubsites(reportSiteContext, webin.Url, userName);
            }
        }

        /// <summary>
        /// Function: process reported sites list ... identify site to be deleted
        /// </summary>
        /// <param name="reportSiteContext">Context of the Site/Web where the Report List will exist</param>
        /// <param name="emailNotificationList"></param>
        private void ProcessSitesList(ClientContext reportSiteContext, List emailNotificationList)
        {

            var spListItems = _context.EntitiesUserProfileSubsites.AsQueryable().Where(w => w.Status == "Pending").GroupBy(gb => gb.SiteOwner);
            foreach (var item in spListItems)
            {

                var _tmpOwner = item.Key; // site owner
                var deletions = new List<string>();
                var notifying = new List<string>();
                var lastnotic = new List<string>();

                foreach (var sites in item.ToList())
                {
                    var EndDate = DateTime.Now;
                    var StartDate = sites.DateCreated;
                    var timeSpan = (EndDate.Subtract(StartDate));

                    if (timeSpan.Days >= Retention)
                    {
                        // #  Delete Site
                        // #  processSiteForDeletion  $item
                        // #  Create email notification item ---> site delete message
                        deletions.Add(sites.Url);
                    }

                    if ((timeSpan.Days < Retention) && (timeSpan.Days >= LastAlert))
                    {
                        // #  Create email notification --> last warning
                        lastnotic.Add(sites.Url);
                    }

                    if (timeSpan.Days < LastAlert)
                    {
                        // #  Create email notification --> first warning
                        notifying.Add(sites.Url);
                    }
                }

                ProcessFirstNotification(reportSiteContext, emailNotificationList, _tmpOwner, notifying);
                ProcessLastNotification(reportSiteContext, emailNotificationList, _tmpOwner, lastnotic);
                ProcessSiteForDeletion(reportSiteContext, emailNotificationList, _tmpOwner, deletions);
            }
        }

        /// <summary>
        /// Function: Remove ODB subsite
        /// </summary>
        /// <param name="reportSiteContext"></param>
        /// <param name="siteItem"></param>
        private void ProcessSiteForDeletion(ClientContext reportSiteContext, List emailNotificationList, string siteowner, List<string> Urls)
        {

            LogVerbose($"Deleting: {string.Join(";", Urls)}");
            if (Urls.Any())
            {
                ProcessNotification(reportSiteContext, emailNotificationList, siteowner, Urls, "Last");
            }

            // #  Delete ODB sub site
            // # $siteItem

            // # if site

            // #  update status in the list

        }

        private void ProcessNotification(ClientContext reportSiteContext, List emailNotificationList, string siteowner, List<string> Urls, string notification = "First")
        {
            try
            {
                var itemCreateInfo = new ListItemCreationInformation();
                var newItem = emailNotificationList.AddItem(itemCreateInfo);
                newItem["Title"] = "Title";
                newItem["Urls"] = string.Join("<br>", Urls);
                newItem["SendTo"] = siteowner;
                newItem["Notification"] = notification;

                newItem.Update();
                reportSiteContext.Load(newItem);
                reportSiteContext.ExecuteQueryRetry();
            }
            catch (Exception ex)
            {
                LogError(ex, "ErrorMessage {0}", ex.Message);
            }
        }

        /// <summary>
        /// Function: Remove ODB subsite
        /// </summary>
        /// <param name="siteowner"></param>
        /// <param name="Urls"></param>
        private void ProcessLastNotification(ClientContext reportSiteContext, List emailNotificationList, string siteowner, List<string> Urls)
        {

            LogVerbose("Notifying : {0} -- {1}", siteowner, Urls);
            if (Urls.Any())
            {
                ProcessNotification(reportSiteContext, emailNotificationList, siteowner, Urls, "Second");
            }
        }

        /// <summary>
        /// Function add item to subsites list
        /// </summary>
        /// <param name="reportSiteContext"></param>
        /// <param name="emailNotificationList"></param>
        /// <param name="siteowner"></param>
        /// <param name="Urls"></param>
        private void ProcessFirstNotification(ClientContext reportSiteContext, List emailNotificationList, string siteowner, List<string> Urls)
        {
            if (Urls.Any())
            {
                ProcessNotification(reportSiteContext, emailNotificationList, siteowner, Urls);
            }
        }

        /// <summary>
        /// Function: enumerate user profiles and get their ODB site
        /// </summary>
        /// <param name="reportSiteContext"></param>
        /// <param name="traceLogger"></param>
        /// <param name="MySiteTenantUrl"></param>
        private void EnumODBSites(ClientContext reportSiteContext, ITraceLogger traceLogger, string MySiteTenantUrl)
        {
            LogVerbose("Starting- This could take a while.");

            //// // #   Sets the first User profile, at index -1
            var profiles = reportSiteContext.GetOneDriveProfiles(traceLogger, MySiteTenantUrl);
            var NumProfiles = profiles.Count;

            // #  As long as the next User profile is NOT the one we started with (at -1)...
            foreach (var profile in profiles)
            {
                // #  (PersonalSpace is the name of the path to a user's OneDrive for Business site. Users who have not yet created a 
                // #  OneDrive for Business site might not have this property set.)

                var Url = profile.PersonalSpaceProperty;
                var SiteUrl = profile.Url;
                var odbFound = profile.HasProfile;
                var UserName = profile.UserName;


                if (odbFound)
                {
                    SetSiteAdmin(SiteUrl, CurrentUserName);

                    var Site = this.ClientContext.Clone(SiteUrl);
                    var Web = Site.Web;
                    Site.Load(Web);
                    Site.Load(Web.Webs, cwebs => cwebs.Include(cwebtx => cwebtx.Url));
                    Site.ExecuteQueryRetry();

                    foreach (var webin in Web.Webs)
                    {
                        // // #   process each subsite
                        ProcessSubsites(reportSiteContext, webin.Url, UserName);
                    }

                    SetSiteAdmin(SiteUrl, CurrentUserName, false);
                }

            }
        }
    }
}
