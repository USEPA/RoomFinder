using CommandLine;
using ConsoleTables;
using EPA.Office365.Database;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.SharePoint.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("ScanEPASiteMailboxes", HelpText = "The function cmdlet will ingest a collection of site mailboxes or mailboxes from Exchange associated with SharePoint.")]
    public class ScanEPASiteMailboxesOptions : TenantCommandOptions
    {
        [Option('m', "mailboxes", Required = true)]
        public IEnumerable<SiteMailboxes> Mailboxes { get; set; }

        /// <summary>
        /// The directory where the notification schedule should be written/read
        /// </summary>
        [Option('d', "log-directory", Required = true)]
        public string LogDirectory { get; set; }

        /// <summary>
        /// Should the query run against the root or all items
        /// </summary>
        [Option("noroot", Required = false)]
        public bool NoRootFilter { get; set; }
    }

    public static class ScanEPASiteMailboxesExtension
    {
        public static int RunGenerateAndReturnExitCode(this ScanEPASiteMailboxesOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPASiteMailboxes(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will ingest a collection of site mailboxes or mailboxes from Exchange associated with SharePoint
    /// </summary>
    public class ScanEPASiteMailboxes : BaseSpoTenantCommand<ScanEPASiteMailboxesOptions>
    {
        public ScanEPASiteMailboxes(ScanEPASiteMailboxesOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        internal string TenantAdminUrl { get; set; }
        private const string siteMailboxFeatureId = "502a2d54-6102-4757-aaa0-a90586106368";
        private IList<SiteMailboxes> Removals { get; set; }

        #endregion

        public override void OnInit()
        {
            TenantAdminUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantAdminUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        /// <summary>
        /// Check path for existence
        /// </summary>
        public override void OnBeforeRun()
        {
            base.OnBeforeRun();

            // check if the dump directory exists
            if (!System.IO.Directory.Exists(Opts.LogDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"The directory {Opts.LogDirectory} could not be found.");
            }
        }

        public override int OnRun()
        {
            Removals = new List<SiteMailboxes>();

            TenantContext.EnsureProperties(tssp => tssp.RootSiteUrl);
            var TenantUrl = TenantContext.RootSiteUrl.EnsureTrailingSlashLowered();
            var MySiteTenantUrl = TenantUrl.Replace(".sharepoint.com", "-my.sharepoint.com");



            var connectionstring = Settings.ConnectionStrings.AnalyticsConnection;
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
            dbContextOptionsBuilder.UseSqlServer(connectionstring);

            using var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options);
            foreach (SiteMailboxes siteMailbox in Opts.Mailboxes)
            {
                var siteUrl = siteMailbox.SharePointUrl;

                // Add service account
                SetSiteAdmin(siteUrl, CurrentUserName, true);


                using var webContext = this.ClientContext.Clone(siteUrl);
                webContext.Load(webContext.Web);
                webContext.Load(webContext.Web, ctxw => ctxw.RequestAccessEmail);
                webContext.Load(webContext.Web.Features);
                webContext.ExecuteQueryRetry();

                if (ProcessWeb(webContext, siteUrl))
                {
                    //get site owner
                    var ownerEmails = GetSiteowners(webContext);
                    if (!ownerEmails.Any())
                    {
                        try
                        {
                            ownerEmails.Add(webContext.Web.RequestAccessEmail);
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, $"Failed to get requestaccessEmail property {ex.Message}");
                        }
                    }

                    EntityTenantSiteMailboxes mailbox = null;
                    if (_context.EntitiesSiteMailboxes.Any(s => s.UserName == siteMailbox.UPN && s.Url == siteUrl))
                    {
                        mailbox = _context.EntitiesSiteMailboxes.FirstOrDefault(s => s.UserName == siteMailbox.UPN && s.Url == siteUrl);
                    }
                    else
                    {
                        mailbox = new EntityTenantSiteMailboxes()
                        {
                            Url = siteUrl,
                            UserName = siteMailbox.UPN
                        };
                        _context.EntitiesSiteMailboxes.Add(mailbox);
                    }

                    mailbox.DateRemoved = DateTime.UtcNow;
                    mailbox.MailboxAddresses = string.Join(";", siteMailbox.EmailAddresses);
                    mailbox.SiteOwnerEmail = string.Join(";", ownerEmails);

                    var rowschanged = _context.SaveChanges();
                    LogVerbose($"Saved {rowschanged} rows...");

                    // Remove mailbox
                    Removals.Add(siteMailbox);
                }

                // Revoke service account
                SetSiteAdmin(siteUrl, CurrentUserName, false);
            }

            LogVerbose($"Exporting {Removals?.Count} to memory");
            ConsoleTable.From(Removals).Write(Format.Alternative);

            if (ShouldProcess("Writing file to disc"))
            {
                // serialize JSON to a string and then write string to a file
                var jsonlogfile = $"{Opts.LogDirectory}\\SiteMailbox.json";
                System.IO.File.WriteAllText(jsonlogfile, Newtonsoft.Json.JsonConvert.SerializeObject(Removals));
            }
            return 1;
        }

        private bool ProcessWeb(ClientContext webContext, string siteUrl)
        {
            // find if the feature is turned on in the site
            var found_flag = false;
            var mailboxFeatureGuid = new Guid(siteMailboxFeatureId);

            try
            {
                foreach (var feature in webContext.Web.Features)
                {
                    if (feature.DefinitionId == mailboxFeatureGuid)
                    {
                        found_flag = true;
                        LogVerbose("Site mailbox feature Found.....");
                        break;
                    }
                }

                //# Remove feature from site
                if (found_flag)
                {
                    //# Remove Feature from site
                    LogVerbose("Removing siteMailbox feature from " + siteUrl);

                    //#logMessage $msg "INFO"
                    webContext.Web.Features.Remove(mailboxFeatureGuid, true);
                    webContext.Web.Update();
                    webContext.ExecuteQueryRetry();
                }

            }
            catch (Exception ex)
            {
                LogError(ex, ex.Message);
            }

            return found_flag;
        }

        private List<string> GetSiteowners(ClientContext ctx)
        {
            var ownerEmails = new List<string>();
            var web = ctx.Web;

            try
            {
                ctx.Load(web.AssociatedOwnerGroup);
                ctx.ExecuteQueryRetry();
                ctx.Load(web.AssociatedOwnerGroup.Users);
                ctx.ExecuteQueryRetry();

                foreach (var ownerUser in web.AssociatedOwnerGroup.Users)
                {
                    var seed = ownerUser.LoginName.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    ownerEmails.Add(seed[seed.Length - 1]);

                }
            }
            catch (Exception ex)
            {
                var ErrorMessage = ex.Message;
                LogError(ex, ErrorMessage);
            }


            try
            {
                if (!ownerEmails.Any())
                {
                    var ownerUser = ctx.Site.Owner;

                    ctx.Load(ownerUser);
                    ctx.ExecuteQueryRetry();

                    var seed = ownerUser.LoginName.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    ownerEmails.Add(seed[seed.Length - 1]);
                }
            }
            catch (Exception ex)
            {
                var ErrorMessage = ex.Message;
                LogError(ex, ErrorMessage);
            }

            return ownerEmails;
        }

    }
}
