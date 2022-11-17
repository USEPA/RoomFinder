using CommandLine;
using EPA.Office365.Database;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEPASiteSharingSettings", HelpText = "Scans epa sites evaluating site sharing settings.")]
    public class ScanEPASiteSharingSettingsOptions : TenantCommandOptions
    {
        [Option('s', "site-url", Required = false)]
        public string SiteUrl { get; set; }

        [Option('d', "log-directory", Required = true)]
        public string LogDirectory { get; set; }
    }

    public static class ScanEPASiteSharingSettingsExtensions
    {
        public static int RunGenerateAndReturnExitCode(this ScanEPASiteSharingSettingsOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPASiteSharingSettings(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Catalogs external users and descrepancies for the sharepoint site
    /// </summary>
    public class ScanEPASiteSharingSettings : BaseSpoTenantCommand<ScanEPASiteSharingSettingsOptions>
    {
        public ScanEPASiteSharingSettings(ScanEPASiteSharingSettingsOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        static readonly List<SiteSharingSettings> _results = new List<SiteSharingSettings>();

        const string RootUrl = "https://usepa.sharepoint.com";


        public override void OnInit()
        {
            var Url = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Url), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
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
            try
            {
                var sites = GetSiteCollections();

                var connectionstring = Settings.ConnectionStrings.AnalyticsConnection;

                var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
                dbContextOptionsBuilder.UseSqlServer(connectionstring);
                using var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options);
                foreach (var site in sites)
                {
                    if (site.Url.ToLower().IndexOf(RootUrl) > -1)
                    {
                        try
                        {
                            LogVerbose(site.Url);
                            SetSiteAdmin(site.Url, CurrentUserName, true);
                            ProcessSite(site.Url, _context);
                            SetSiteAdmin(site.Url, CurrentUserName, false);
                        }
                        catch (Exception e)
                        {
                            LogError(e, e.Message);
                        }
                    }
                }

                var _dbResults = _context.EntitiesWebSharing.ToList();

                foreach (var _web in _dbResults)
                {
                    var _tmpText = new SiteSharingSettings()
                    {
                        TemplateId = _web.SiteTemplateId,
                        WebId = _web.WebGuid,
                        Title = _web.WebTitle,
                        Region = _web.Region,
                        SiteType = _web.SiteType,
                        SiteUrl = _web.WebUrl,
                        CanMemberShare = _web.CanMemberShare
                    };

                    LogVerbose($"Site {_tmpText}");
                    _results.Add(_tmpText);
                }

            }
            catch (Exception e)
            {
                LogVerbose(e.Message);
            }


            if (ShouldProcess("Writing file to disc"))
            {
                // Create JSON Directory if it does not exists
                var jsonString = JsonConvert.SerializeObject(_results, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 5,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                var outFile = System.IO.Path.Combine(Opts.LogDirectory, $"schedule-sitesharing-{DateTime.UtcNow:yyyy-MM-dd_HH_mm_ss}.json");
                System.IO.File.WriteAllText(outFile, jsonString, System.Text.Encoding.UTF8);
            }

            return 1;
        }

        private void ProcessSite(string _siteUrl, AnalyticDbContext _dbContext)
        {

            using var ctx = this.ClientContext.Clone(_siteUrl);
            Web _web = ctx.Web;
            ctx.Load(_web, s => s.Id, s => s.Url, s => s.Title, s => s.Title, s => s.MembersCanShare, s => s.WebTemplate);
            IEnumerable<Web> webQuery = ctx.LoadQuery(ctx.Web.Webs.Include(winc => winc.Url));
            ctx.ExecuteQuery();

            var _region = GetRegionSiteType(_siteUrl);

            // Save the changes to the reporting database
            EntityTenantWebSharing site = null;
            if (_dbContext.EntitiesWebSharing.Any(es => es.WebGuid == _web.Id))
            {
                site = _dbContext.EntitiesWebSharing.FirstOrDefault(fs => fs.WebGuid == _web.Id);
                site.CanMemberShare = _web.MembersCanShare;
            }
            else
            {
                site = new EntityTenantWebSharing()
                {
                    SiteTemplateId = _web.WebTemplate,
                    WebGuid = _web.Id,
                    WebTitle = _web.Title,
                    Region = _region.Region,
                    SiteType = _region.SiteType,
                    WebUrl = _siteUrl,
                    CanMemberShare = _web.MembersCanShare
                };
                _dbContext.EntitiesWebSharing.Add(site);
            }

            var _rows = _dbContext.SaveChanges();
            LogVerbose($"{_rows} have been returned");

            foreach (Web web in webQuery)
            {
                if (web.Url.ToLower().IndexOf(RootUrl) > -1)
                {
                    ProcessSite(web.Url, _dbContext);
                }
            }

        }
    }

}
