using CommandLine;
using EPA.Office365.Database;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using EPA.SharePoint.SysConsole.Models.Governance;
using EPA.SharePoint.SysConsole.Models.Reporting;
using EPA.SharePoint.SysConsole.Models.Scan;
using Microsoft.EntityFrameworkCore;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("getEPAAnalyticO365Sites", HelpText = "Pulls sharepoint sites data via CSOM and stores it in the database")]
    public class GetEPAAnalyticO365SitesOptions : TenantCommandOptions
    {
        /// <summary>
        /// Should we recursively enumerate all the subsites
        /// </summary>
        [Option("enumerate-subsites", Required = false, SetName = "Main", HelpText = "If you want to process all sites.")]
        public bool EnumerateSubsites { get; set; }

        [Option("site-url", Required = false, SetName = "Main", HelpText = "If you want to process all sites.")]
        public string SiteUrl { get; set; }
    }

    public static class GetEPAAnalyticO365SitesExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPAAnalyticO365SitesOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAAnalyticO365Sites(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Query the tenant to discover all O365 sites and process statistics
    /// </summary>
    /// <remarks>
    /// *************************************************************************
    /// ************************ Enumerate throughout sites' list and process eac ***********************
    /// *************************************************************************
    /// </remarks>
    public class GetEPAAnalyticO365Sites : BaseSpoTenantCommand<GetEPAAnalyticO365SitesOptions>
    {
        public GetEPAAnalyticO365Sites(GetEPAAnalyticO365SitesOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private Variables

        internal string TenantAdminUrl { get; set; }
        internal string ClientId { get; set; }
        internal string ClientSecret { get; set; }
        private string RootSiteUrl { get; set; }
        private int _SitesRowCount { get; set; }
        private int _TotalWebsRowCount { get; set; }
        private double _spoTenantUsedStorage { get; set; }
        private double _spoTenantQuotaBytes { get; set; }
        private string _UserAgent { get; set; }
        private bool _spoPreviouslyInitiated { get; set; }

        #endregion

        public override void OnInit()
        {
            TenantAdminUrl = Settings.Commands.TenantAdminUrl;
            ClientId = Settings.SpoAddInMakeEPASite.ClientId;
            ClientSecret = Settings.SpoAddInMakeEPASite.ClientSecret;

            Settings.AzureAd.SPClientID = ClientId;
            Settings.AzureAd.SPClientSecret = ClientSecret;

            LogVerbose($"AppCredential connecting and setting Add-In keys");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantAdminUrl), null, ClientId, ClientSecret, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {

            RootSiteUrl = "";
            _spoTenantUsedStorage = 0;
            _spoTenantQuotaBytes = 0;
            _TotalWebsRowCount = 0;
            _SitesRowCount = 0;
            _spoPreviouslyInitiated = false;
            _UserAgent = Settings.Commands.SharePointPnPUserAgent;
            var connectionstring = Settings.ConnectionStrings.AnalyticsConnection;

            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
            dbContextOptionsBuilder.UseSqlServer(connectionstring);
            using var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options);
            try
            {
                EntityTenantDates tenantDate = null;
                if (_context.EntitiesTenantDates.Any(etd => !etd.IsComplete))
                {
                    tenantDate = _context.EntitiesTenantDates.Include(etd => etd.SiteAnalytics).FirstOrDefault(fd => !fd.IsComplete);
                    _spoPreviouslyInitiated = true;
                }
                else
                {
                    tenantDate = new EntityTenantDates()
                    {
                        AnalyticType = AnalyticTypeEnum.Sites,
                        DTSTART = Opts.LogDateTime ?? DateTime.UtcNow,
                        FormattedDate = (Opts.LogDateTime ?? DateTime.UtcNow).ToString("MM/dd/yyyy"),
                        SiteAnalytics = new List<EntityTenantSiteAnalytics>(),
                        TotalSites = 0,
                        TotalWebs = 0
                    };
                    _context.EntitiesTenantDates.Add(tenantDate);
                }

                // Ensure we have the root site URL
                TenantContext.EnsureProperties(ct => ct.RootSiteUrl);
                RootSiteUrl = UrlPattern(TenantContext.RootSiteUrl);

                // Pull all site collections
                var urls = new List<SPOSiteCollectionModel>();

                if (string.IsNullOrEmpty(Opts.SiteUrl))
                {
                    urls = GetSiteCollections(true);
                }
                else
                {
                    urls.Add(new SPOSiteCollectionModel() { Url = UrlPattern(Opts.SiteUrl) });
                }

                // if previously initiatied get sites that were scanned and pop all but the last URL off the stack to be queried
                if (_spoPreviouslyInitiated)
                {
                    var tenantId = tenantDate.ID;
                    var _spoPreviouslyScanned = _context.EntitiesSites
                        .Include(ines => ines.SiteAnalytics)
                        .Where(es => es.SiteAnalytics.Any(esa => esa.TenantDateId == tenantId && esa.ScanCompleted))
                        .OrderByDescending(ob => ob.DTUPD)
                        .Select(s => new { s.SiteUrl, s.DTUPD })
                        .ToList();

                    foreach (var _spoSite in _spoPreviouslyScanned)
                    {
                        var _siteUrl = UrlPattern(_spoSite.SiteUrl);
                        if (urls.Any(u => u.Url.Equals(_siteUrl, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            var site = urls.First(u => u.Url.Equals(_siteUrl, StringComparison.CurrentCultureIgnoreCase));
                            urls.Remove(site);
                        }
                    }
                }

                // Scan through the sites and output content from solution gallery (exists in root site of site collection)
                foreach (var site in urls)
                {
                    var siteUrl = site.Url;
                    if (siteUrl.IndexOf(RootSiteUrl) > -1) // focused on sites and not MySites or Public Sites
                    {
                        try
                        {
                            LogVerbose("Processing site collection {0}....", siteUrl);
                            ProcessSiteCollection(_context, tenantDate, site);
                        }
                        catch (Exception e)
                        {
                            LogError(e, "Failed to process site collection {0}", e.Message);
                        }
                    }
                }

                // After processing the tenant lets set the Tenant reporting date holder
                var tdates = _context.EntitiesTenantDates.AsQueryable().Where(w => w.IsCurrent);
                tdates.ToList().ForEach(fe => fe.IsCurrent = false);
                tenantDate.IsCurrent = true;
                tenantDate.IsComplete = true;
                tenantDate.DTEND = DateTime.UtcNow;
                tenantDate.TotalSites = urls?.Count() ?? 0;
                tenantDate.TotalWebs = _TotalWebsRowCount;
                var dateset = _context.SaveChanges();
                LogVerbose("Update tenant dates {0}", dateset);
            }
            catch (Exception e)
            {
                LogError(e, "Failed to execute cmdlet {0}", e.Message);
                return -1;
            }

            return 1;
        }



        /// <summary>
        /// process the site collection and optionaly its subsites
        /// </summary>
        /// <param name="_context">PowerBI Database context</param>
        /// <param name="_siteUrl"></param>
        internal void ProcessSiteCollection(AnalyticDbContext _context, EntityTenantDates tenantDate, SPOSiteCollectionModel _site)
        {
            var _siteUrl = UrlPattern(_site.Url);
            var siteAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
            using var ctx = siteAuthManager.GetAppOnlyAuthenticatedContext(_siteUrl, ClientId, ClientSecret);
            var siteusercount = 0;
            var scalist = string.Empty;
            var _regionSiteType = GetRegionSiteType(_siteUrl);
            var _siteType = _regionSiteType.SiteType;
            var _siteRegion = _regionSiteType.Region;
            var siteTracked = false;
            if ((_siteType == "ORG")
                || (_siteType == "WORK")
                || (_siteType == "COMMUNITY")
                || (_siteType == "APPLICATIONS")
                || (_siteType == "DEVELOPMENT")
                || (_siteType == "CUSTOM"))
            {
                siteTracked = true;
            }

            try
            {
                var totalusers = ctx.LoadQuery(ctx.Web.SiteUsers
                    .Where(su => su.PrincipalType == Microsoft.SharePoint.Client.Utilities.PrincipalType.User)
                    .Include(ictx => ictx.Id, ictx => ictx.IsSiteAdmin, ictx => ictx.Email));

                ctx.ExecuteQueryRetry(20, 1000, userAgent: _UserAgent);
                siteusercount = totalusers.Count();
                scalist = Newtonsoft.Json.JsonConvert.SerializeObject(totalusers.Where(w => w.IsSiteAdmin).Select(s => s.Email));
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to retrieve SCAs {0} stack trace {1}", ex.Message, ex.StackTrace);
            }

            // ---> Web Properties
            ctx.Load(ctx.Web,
                s => s.AssociatedOwnerGroup,
                s => s.Id,
                s => s.UIVersion,
                s => s.LastItemUserModifiedDate,
                s => s.Created,
                s => s.Url);

            // ---> Site Usage Properties
            ctx.Load(ctx.Site, cts => cts.Usage, cts => cts.Id, ctxs => ctxs.AllowMasterPageEditing);
            ctx.ExecuteQueryRetry(userAgent: _UserAgent);


            var _web = ctx.Web;
            var _usageInfo = ctx.Site.GetSiteUsageMetric();
            DateTime _createdDate = (DateTime)ctx.Web.Created;

            _spoTenantQuotaBytes += _usageInfo.StorageQuotaBytes;
            _spoTenantUsedStorage += _usageInfo.StorageUsedPercentage;

            EntityTenantSite site = null;
            if (_context.EntitiesSites.Any(sc => sc.SiteGuid == _web.Id))
            {
                site = _context.EntitiesSites.Include(es => es.SiteAnalytics).FirstOrDefault(f => f.SiteGuid == _web.Id);
            }
            else
            {
                site = new EntityTenantSite()
                {
                    SiteUrl = _siteUrl,
                    SiteGuid = _web.Id,
                    SiteAnalytics = new List<EntityTenantSiteAnalytics>()
                };
                _context.EntitiesSites.Add(site);
            }

            site.DTUPD = DateTime.UtcNow;
            site.Region = _siteRegion;
            site.SiteType = _siteType;


            EntityTenantSiteAnalytics siteAnalytics = null;
            if (!tenantDate.SiteAnalytics.Any(sa => sa.SiteGuid == _web.Id))
            {
                siteAnalytics = new EntityTenantSiteAnalytics()
                {
                    SiteGuid = _web.Id,
                    TenantSiteLookup = site
                };
                tenantDate.SiteAnalytics.Add(siteAnalytics);
            }
            else
            {
                siteAnalytics = tenantDate.SiteAnalytics.FirstOrDefault(sa => sa.SiteGuid == _web.Id);
            }

            siteAnalytics.SiteOwners = scalist;
            siteAnalytics.TotalSiteUsers = siteusercount;
            siteAnalytics.TrackedSite = siteTracked;
            siteAnalytics.Storage_Allocated_GB = _usageInfo.AllocatedGbDecimal;
            siteAnalytics.Storage_Allocated_MB = _usageInfo.AllocatedMbDecimal;
            siteAnalytics.Storage_Usage_GB = _usageInfo.UsageGbDecimal;
            siteAnalytics.Storage_Usage_MB = _usageInfo.UsageMbDecimal;
            siteAnalytics.Storage_Used_Perct = _usageInfo.StorageUsedPercentageDecimal;
            siteAnalytics.DTMETRIC = DateTime.UtcNow;


            var rowsProcessed = _context.SaveChanges();
            LogVerbose("Rows {0} processed", rowsProcessed);


            _SitesRowCount = 0;

            // Process subsites
            if (Opts.EnumerateSubsites)
            {
                // process the Site Collection Web
                ProcessSubSites(_context, tenantDate, site, ctx.Web.Url, _siteUrl);
            }

            // Save the total count and processing status
            siteAnalytics.ScanCompleted = true;
            siteAnalytics.SubSiteCount = _SitesRowCount; // should be aggregated up from recursive deep dive
            rowsProcessed = _context.SaveChanges();
            LogVerbose("Rows {0} processed", rowsProcessed);
        }

        /// <summary>
        ///  Process Subsites 
        /// </summary>
        /// <param name="_context">PowerBI database</param>
        /// <param name="siteId">The site collection unique ID</param>
        /// <param name="siteRegion">The prefixed region</param>
        /// <param name="_web"></param>
        internal void ProcessSubSites(AnalyticDbContext _context, EntityTenantDates tenantDate, EntityTenantSite siteId, string _webUrl, string _parentWebUrl)
        {
            var _siteUrl = UrlPattern(_webUrl);
            var _siteType = GetRegionSiteType(_siteUrl);

            var siteAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
            using var ctx = siteAuthManager.GetAppOnlyAuthenticatedContext(_siteUrl, ClientId, ClientSecret);
            // Process current site
            var siteLogLine = ProcessSite(_context, ctx, tenantDate, siteId, _siteUrl, _parentWebUrl);
            if (siteLogLine)
            {
                _SitesRowCount++;
                _TotalWebsRowCount++;
            }

            // Process subsites
            if (Opts.EnumerateSubsites)
            {
                var webs = ctx.Web.Webs;
                IEnumerable<Web> webQuery = ctx.LoadQuery(webs);
                ctx.ExecuteQueryRetry(userAgent: _UserAgent);
                var webUrls = webQuery.Where(w => !string.IsNullOrEmpty(w.Url)).Select(s => s.Url);
                foreach (var _inWebUrl in webUrls)
                {
                    ProcessSubSites(_context, tenantDate, siteId, _inWebUrl, _siteUrl);
                }
            }
        }

        /// <summary>
        /// Capture Sub-site Data
        /// </summary>
        /// <param name="_context">PowerBI database</param>
        /// <param name="ctx"></param>
        /// <param name="siteId">The site collection unique ID</param>
        /// <param name="siteRegion">The prefixed region</param>
        /// <param name="_siteUrl">Ensure this is a URL that has been appended with a slash</param>
        /// <param name="_parentWebUrl">Ensure this URL is the parent web URL for this subsite</param>
        internal bool ProcessSite(AnalyticDbContext _context, ClientContext ctx, EntityTenantDates tenantDate, EntityTenantSite siteId, string _siteUrl, string _parentWebUrl)
        {
            var currrentRuntime = DateTime.UtcNow;
            var liststatus = new List<ListStatistic>();
            long _discussionCount = 0;
            long _discussionRepliesCount = 0;
            long _totalDocumentLibrariesCount = 0;
            long _totalDocumentItemsCount = 0;
            var _discussionReplyLastDate = default(Nullable<DateTime>);
            var _discussionLastDate = default(Nullable<DateTime>);
            long _memberTotalItemCount = 0;
            var _memberJoinedLastDate = default(Nullable<DateTime>);
            var _lastDocumentEdited = new DateTime(1900, 1, 1);
            var _hasCommunity = false;
            var _siteDocumentActivity = default(Nullable<decimal>);
            var pagestatus = new List<PageFileStatistic>();
            var _webAddIn = false;
            var webRelativeUrl = string.Empty;
            var _siteowners = string.Empty;

            Microsoft.SharePoint.Client.Search.Analytics.UsageAnalytics UsageAnalyticsObj = null;

            Web _web = ctx.Web;
            ctx.Load(_web, website => website.RootFolder.WelcomePage,
                    website => website.ServerRelativeUrl,
                    website => website.Url,
                    website => website.Id,
                    website => website.UIVersion,
                    website => website.Title,
                    website => website.Created,
                    website => website.LastItemModifiedDate,
                    website => website.LastItemUserModifiedDate,
                    website => website.AppInstanceId,
                    website => website.WebTemplate,
                    website => website.CustomMasterUrl,
                    website => website.MasterUrl,
                    website => website.AssociatedOwnerGroup,
                    website => website.AssociatedMemberGroup,
                    website => website.AssociatedVisitorGroup,
                    website => website.HasUniqueRoleAssignments,
                    website => website.NoCrawl);

            try
            {
                LogVerbose($"Running initial web loader for {ctx.Url}");
                ctx.ExecuteQueryRetry(userAgent: _UserAgent);
            }
            catch (Exception e)
            {
                LogError(e, "Failed to process site {0} with message {1}", ctx.Url, e.Message);
                return false;
            }

            try
            {
                webRelativeUrl = TokenHelper.EnsureTrailingSlash(_web.ServerRelativeUrl);

                // process app instance in this web
                _webAddIn = (_web.AppInstanceId != null && _web?.AppInstanceId != Guid.Empty);
                if (_webAddIn)
                {
                    // Grab basics regarding the Add-In
                    try
                    {
                        var uri = new Uri(new Uri(RootSiteUrl), _parentWebUrl);

                        var siteAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
                        using var parentWebContext = siteAuthManager.GetAppOnlyAuthenticatedContext(uri.ToString(), ClientId, ClientSecret);
                        var parentWeb = parentWebContext.Web;
                        parentWebContext.Load(parentWeb);
                        parentWebContext.ExecuteQueryRetry(userAgent: _UserAgent);

                        var scanmodel = ProcessAddinInstance(parentWeb, _web.AppInstanceId, true);

                        EntityTenantWebAddIn addIn = null;
                        if (_context.EntitiesWebAddIn.Any(ew => ew.AppGuid == _web.AppInstanceId && ew.WebGuid == _web.Id))
                        {
                            addIn = _context.EntitiesWebAddIn.FirstOrDefault(ew => ew.AppGuid == _web.AppInstanceId && ew.WebGuid == _web.Id);
                        }
                        else
                        {
                            addIn = new EntityTenantWebAddIn()
                            {
                                AppGuid = _web.AppInstanceId,
                                WebGuid = _web.Id
                            };
                            _context.EntitiesWebAddIn.Add(addIn);
                        }

                        addIn.AppDescription = scanmodel.ShortDescription;
                        addIn.AppPrincipalId = scanmodel.AppPrincipalId;
                        addIn.AppRedirectUrl = scanmodel.AppRedirectUrl;
                        addIn.AppStatus = scanmodel.Status.ToString("f");
                        addIn.AppTitle = scanmodel.Title;
                        addIn.AppWebFullUrl = scanmodel.AppWebFullUrl;
                        addIn.EulaUrl = scanmodel.EulaUrl;
                        addIn.HostedType = scanmodel.HostedType.ToString("f");
                        addIn.HostWebUrl = scanmodel.HostWebUrl;
                        addIn.ImageFallbackUrl = scanmodel.ImageFallbackUrl;
                        addIn.ImageUrl = scanmodel.ImageUrl;
                        addIn.InError = scanmodel.InError;
                        addIn.PrivacyUrl = scanmodel.PrivacyUrl;
                        addIn.ProductGuid = scanmodel.ProductId;
                        addIn.Publisher = scanmodel.Publisher;
                        addIn.RemoteAppUrl = scanmodel.RemoteAppUrl;
                        addIn.SettingsPageUrl = scanmodel.SettingsPageUrl;
                        addIn.SiteGuid = scanmodel.SiteId;
                        addIn.StartPage = scanmodel.StartPage;
                        addIn.SupportUrl = scanmodel.SupportUrl;
                        addIn.TenantSiteId = siteId.ID;
                        if (scanmodel.HasPermissions && scanmodel.AppPermissions.Any())
                        {
                            addIn.AppPermissions = string.Join("<br/>", scanmodel?.AppPermissions);
                        }
                    }
                    catch (Exception aex)
                    {
                        LogWarning("Failed to process app instance ID {0}", aex);
                    }
                }
                else
                {
                    // expand properties for the NON Add-In
                    ctx.Load(ctx.Site);
                    // lets gather statistics on the NON Add-Ins
                    // establish analytics by the site object
                    UsageAnalyticsObj = new Microsoft.SharePoint.Client.Search.Analytics.UsageAnalytics(ctx, ctx.Site);
                    ctx.Load(UsageAnalyticsObj);
                    ctx.ExecuteQueryRetry(userAgent: _UserAgent);
                }

                var hasUniquePerm = false;
                var hasAssociatedOwner = false;
                var hasAssociatedMember = false;
                var hasAssociatedVisitor = false;

                try
                {
                    LogVerbose("Processing:site {0} =>permissions", _siteUrl);

                    hasUniquePerm = _web?.HasUniqueRoleAssignments ?? false;
                    hasAssociatedMember = !_web?.AssociatedMemberGroup?.ServerObjectIsNull ?? false;
                    hasAssociatedVisitor = !_web?.AssociatedVisitorGroup?.ServerObjectIsNull ?? false;

                    if (_web?.AssociatedOwnerGroup?.ServerObjectIsNull == false)
                    {
                        hasAssociatedOwner = true;
                        var _owners = ctx.LoadQuery(_web.AssociatedOwnerGroup.Users);
                        ctx.ExecuteQueryRetry(userAgent: _UserAgent);

                        if (_owners != null)
                        {
                            _siteowners = Newtonsoft.Json.JsonConvert.SerializeObject(_owners.Where(s => !string.IsNullOrEmpty(s.Email)).Select(os => os.Email));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Failed to query for users {0}", ex.Message);
                }

                var listTypeTracking = new ListTemplateType[]
                {
                    ListTemplateType.DocumentLibrary,
                    ListTemplateType.Survey,
                    ListTemplateType.PictureLibrary
                };
                var hasMetadataList = false;
                var metadataItemCount = 0;
                var metadataUniqueRolePermissions = false;
                var totalDiscussions = new List<ListStatistic>();
                DateTime _createdDate = _web.Created;
                DateTime _webLastItemModifiedDate = _web.LastItemUserModifiedDate;
                var _webSiteActivity = new TimeWindow(_webLastItemModifiedDate, currrentRuntime);

                LogVerbose($"Processing:site {_siteUrl} =>lists");

                // Get all lists to scan for content types
                var _webLists = ctx.LoadQuery(_web.Lists.Include(
                    wl => wl.Id,
                    wlc => wlc.Title,
                    wlc => wlc.ItemCount,
                    wlc => wlc.Created,
                    wlc => wlc.LastItemModifiedDate,
                    wlc => wlc.LastItemUserModifiedDate,
                    wlc => wlc.BaseTemplate,
                    wlc => wlc.BaseType,
                    wlc => wlc.IsApplicationList,
                    wlc => wlc.IsCatalog,
                    wlc => wlc.IsPrivate,
                    wlc => wlc.IsSiteAssetsLibrary,
                    wlc => wlc.IsSystemList,
                    wlc => wlc.Hidden,
                    wlc => wlc.HasUniqueRoleAssignments,
                    wlc => wlc.RootFolder,
                    wlc => wlc.ParentWebUrl));
                ctx.ExecuteQueryRetry(userAgent: _UserAgent);

                // enumerate the lists on the site and collection metadata
                foreach (List _webListDetail in _webLists)
                {
                    var listRelativeUrl = _webListDetail.GetWebRelativeUrl();
                    var listTitle = _webListDetail.Title;

                    if (listTitle.Equals("Community Members", StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            LogVerbose("Processing:site {0} =>list {1}", _siteUrl, listTitle);
                            _webListDetail.EnsureProperties(cts => cts.LastItemUserModifiedDate, cts => cts.ItemCount);
                            _memberTotalItemCount = _webListDetail.ItemCount;
                            _memberJoinedLastDate = _webListDetail.LastItemUserModifiedDate;
                            _hasCommunity = true;
                        }
                        catch (Exception e)
                        {
                            LogWarning("Failed to retrieve community members list {0}", e.Message);
                        }
                    }

                    if (listTitle.Equals("MetaData", StringComparison.CurrentCultureIgnoreCase)
                        || listRelativeUrl.IndexOf("lists/metadata", StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        LogVerbose("Processing:site {0} =>list {1}", _siteUrl, listTitle);
                        hasMetadataList = true;
                        metadataItemCount = _webListDetail.ItemCount;
                        metadataUniqueRolePermissions = _webListDetail.HasUniqueRoleAssignments;
                    }

                    if (listRelativeUrl.IndexOf("/sitepages", StringComparison.CurrentCultureIgnoreCase) > -1
                        || listRelativeUrl.IndexOf("/pages", StringComparison.CurrentCultureIgnoreCase) > -1
                        || _webListDetail.BaseTemplate == (int)ListTemplateType.WebPageLibrary)
                    {
                        LogVerbose("Processing:site {0} =>list {1}", _siteUrl, listTitle);
                        try
                        {
                            var rootFiles = ctx.LoadQuery(_webListDetail.RootFolder.Files
                                .Include(
                                    srfin => srfin.ServerRelativeUrl,
                                    srfin => srfin.Length,
                                    srfin => srfin.Level,
                                    srfin => srfin.Name,
                                    srfin => srfin.TimeCreated,
                                    srfin => srfin.Title,
                                    srfin => srfin.UIVersionLabel,
                                    srfin => srfin.ListItemAllFields,
                                    srfin => srfin.SiteId,
                                    srfin => srfin.WebId,
                                    srfin => srfin.ListId,
                                    srfin => srfin.UniqueId,
                                    srfin => srfin.TimeLastModified,
                                    srfin => srfin.ModifiedBy));
                            ctx.ExecuteQueryRetry(userAgent: _UserAgent);

                            foreach (Microsoft.SharePoint.Client.File _targetPage in rootFiles)
                            {
                                var serverRelativeUrl = string.Empty;
                                try
                                {
                                    serverRelativeUrl = _targetPage.ServerRelativeUrl;
                                    if (!pagestatus.Any(ps => ps.Url.Equals(serverRelativeUrl)))
                                    {
                                        ListItem _targetItem = _targetPage.ListItemAllFields;
                                        ctx.Load(_targetPage, ctp => ctp.Length
                                                            , ctp => ctp.Level
                                                            , ctp => ctp.ListId
                                                            , ctp => ctp.Name
                                                            , ctp => ctp.ModifiedBy
                                                            , ctp => ctp.SiteId
                                                            , ctp => ctp.ServerRelativeUrl
                                                            , ctp => ctp.CustomizedPageStatus
                                                            , ctp => ctp.Exists
                                                            , ctp => ctp.TimeLastModified
                                                            , ctp => ctp.TimeCreated
                                                            , ctp => ctp.Title
                                                            , ctp => ctp.UniqueId
                                                            , ctp => ctp.UIVersionLabel
                                                            , ctp => ctp.WebId);
                                        ctx.Load(_targetItem);
                                        ctx.ExecuteQueryRetry(userAgent: _UserAgent);
                                        var _pageStatus = new PageFileStatistic()
                                        {
                                            SiteGuid = _targetPage.SiteId,
                                            WebGuid = _targetPage.WebId,
                                            ListGuid = _targetPage.ListId,
                                            PageGuid = _targetPage.UniqueId,
                                            PageId = _targetItem.Id,
                                            Url = serverRelativeUrl,
                                            TotalHits = 0,
                                            TotalUniqueUsers = 0,
                                            Edited = _targetPage.TimeLastModified,
                                            EditedUser = _targetPage.ModifiedBy.ToUserEmailValue()
                                        };

                                        if (!_webAddIn && _targetItem.IsPropertyAvailable(tctx => tctx.Id))
                                        {
                                            try
                                            {
                                                Microsoft.SharePoint.Client.Search.Analytics.AnalyticsItemData eachListItemAnalyticsData = UsageAnalyticsObj.GetAnalyticsItemData(1, _targetItem);
                                                ctx.Load(eachListItemAnalyticsData, s => s.TotalHits, s => s.TotalUniqueUsers, sti => sti.LastProcessingTime);
                                                ctx.ExecuteQueryRetry(userAgent: _UserAgent);
                                                _pageStatus.TotalHits = eachListItemAnalyticsData.TotalHits;
                                                _pageStatus.TotalUniqueUsers = eachListItemAnalyticsData.TotalUniqueUsers;
                                            }
                                            catch (Exception e)
                                            {
                                                LogWarning($"Failed to retrieve [{listTitle} => File {serverRelativeUrl}] {e.Message}");
                                            }
                                        }

                                        pagestatus.Add(_pageStatus);
                                    }
                                }
                                catch (Exception e)
                                {
                                    // Most Likely the document library does not exists
                                    LogWarning("Failed to retrieve File [{0}] {1}", serverRelativeUrl, e.Message);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            LogWarning("Failed to process list [{0}] {1}", listTitle, ex.Message);
                        }
                    }

                    try
                    {
                        LogVerbose("Processing:site {0} =>list {1} =>content types", _siteUrl, listTitle);
                        if (listTypeTracking.Any(ltt => _webListDetail.BaseTemplate == (int)ltt)
                            && !_webListDetail.IsSystemList
                            && !_webListDetail.IsSiteAssetsLibrary
                            && !_webListDetail.IsApplicationList
                            && !_webListDetail.IsPrivate)
                        {
                            liststatus.Add(new ListStatistic()
                            {
                                ListName = listTitle,
                                ListType = _webListDetail.BaseType,
                                ListTemplateType = (ListTemplateType)_webListDetail.BaseTemplate,
                                ItemCount = _webListDetail.ItemCount,
                                FirstItemCreated = _webListDetail.Created,
                                LastItemUserModifiedDate = _webListDetail.LastItemUserModifiedDate
                            });
                        }

                        if (_webListDetail.BaseTemplate == (int)ListTemplateType.DiscussionBoard)
                        {
                            var discussions = GetDiscussionBoards(ctx, _webListDetail);
                            totalDiscussions.AddRange(discussions);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning("Failed to process content types [{0}] {1}", listTitle, ex.Message);
                    }
                }

                // parse the welcomepage to identity the welcomepage stats
                var welcomePage = string.Empty; var welcomePageFailed = false;
                if (!string.IsNullOrEmpty(_web.RootFolder.WelcomePage))
                {
                    welcomePage = TokenHelper.EnsureTrailingSlash(_web.ServerRelativeUrl) + _web.RootFolder.WelcomePage;
                    try
                    {
                        Microsoft.SharePoint.Client.File _targetPage = ctx.Web.GetFileByServerRelativeUrl(welcomePage);
                        ListItem _targetItem = _targetPage.ListItemAllFields;
                        ctx.Load(_targetPage, ctp => ctp.Length
                                            , ctp => ctp.Level
                                            , ctp => ctp.ListId
                                            , ctp => ctp.Name
                                            , ctp => ctp.ModifiedBy
                                            , ctp => ctp.SiteId
                                            , ctp => ctp.ServerRelativeUrl
                                            , ctp => ctp.CustomizedPageStatus
                                            , ctp => ctp.Exists
                                            , ctp => ctp.TimeLastModified
                                            , ctp => ctp.TimeCreated
                                            , ctp => ctp.Title
                                            , ctp => ctp.UniqueId
                                            , ctp => ctp.UIVersionLabel
                                            , ctp => ctp.CheckOutType
                                            , ctp => ctp.WebId);
                        ctx.Load(_targetItem);
                        ctx.ExecuteQueryRetry(userAgent: _UserAgent);

                        PageFileStatistic pagestats = null;
                        if (pagestatus.Any(ps => ps.Url.Equals(_targetPage.ServerRelativeUrl)))
                        {
                            pagestats = pagestatus.FirstOrDefault(ps => ps.Url.Equals(_targetPage.ServerRelativeUrl));
                        }
                        else
                        {
                            pagestats = new PageFileStatistic()
                            {
                                SiteGuid = _targetPage.SiteId,
                                WebGuid = _targetPage.WebId,
                                ListGuid = _targetPage.ListId,
                                PageGuid = _targetPage.UniqueId,
                                Url = _targetPage.ServerRelativeUrl,
                                TotalHits = 0,
                                TotalUniqueUsers = 0
                            };

                            // Not a Page Type | May not be associated with a ListItem
                            if (_targetItem.IsPropertyAvailable(ti => ti.Id))
                            {
                                pagestats.PageId = _targetItem.Id;
                            }

                            pagestatus.Add(pagestats);
                        }
                        pagestats.IsWelcomePage = true;
                        pagestats.Edited = _targetPage.TimeLastModified;
                        pagestats.EditedUser = _targetPage.ModifiedBy.ToUserEmailValue();

                        // Not a Page Type | May not be associated with a ListItem
                        if (!_webAddIn && _targetItem.IsPropertyAvailable(ti => ti.Id))
                        {
                            try
                            {
                                Microsoft.SharePoint.Client.Search.Analytics.AnalyticsItemData eachListItemAnalyticsData = UsageAnalyticsObj.GetAnalyticsItemData(1, _targetItem);
                                ctx.Load(eachListItemAnalyticsData, s => s.TotalHits, s => s.TotalUniqueUsers);
                                ctx.ExecuteQueryRetry(userAgent: _UserAgent);
                                pagestats.TotalHits = eachListItemAnalyticsData.TotalHits;
                                pagestats.TotalUniqueUsers = eachListItemAnalyticsData.TotalUniqueUsers;
                            }
                            catch (Exception e)
                            {
                                LogWarning($"Failed to retrieve analytics [Welcome Page => {welcomePage}] {e.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        welcomePageFailed = true;
                        // Most Likely the document library does not exists
                        LogWarning("Failed to retrieve File [{0}] {1}", welcomePage, e.Message);
                    }
                }

                if (liststatus.Any(ls => ls.ListTemplateType == ListTemplateType.DocumentLibrary))
                {
                    var documentLists = liststatus.Where(w => w.ListTemplateType == ListTemplateType.DocumentLibrary);
                    _lastDocumentEdited = documentLists.Max(m => m.LastItemUserModifiedDate);
                    _totalDocumentItemsCount = documentLists.Sum(m => m.ItemCount);
                    _totalDocumentLibrariesCount = documentLists.Count();
                    _siteDocumentActivity = TryParseDouble((new TimeWindow(_lastDocumentEdited, currrentRuntime)).TotalDays, 0);

                }

                if (totalDiscussions.Any())
                {
                    _discussionCount = totalDiscussions.Count();
                    _discussionLastDate = totalDiscussions.Where(w => w.LastItemUserModifiedDate != null).Max(s => s.LastItemUserModifiedDate);
                    _discussionRepliesCount = totalDiscussions.Where(w => w.Replies != null).Sum(m => m.Replies.Count());
                    if (_discussionRepliesCount > 0)
                    {
                        var _replies = totalDiscussions.Where(w => w.Replies != null).SelectMany(sm => sm.Replies.Select(s => s.LastItemUserModifiedDate));
                        if (_replies != null && _replies.Count() > 0)
                        {
                            _discussionReplyLastDate = _replies.Max();
                        }
                    }
                }

                // Calculate statistics from Libraries
                var _totalHits = pagestatus.Sum(f => f.TotalHits);
                var _totalUniqueVisitors = pagestatus.Sum(f => f.TotalUniqueUsers);
                var _totalHitsHomePage = pagestatus.Where(w => w.IsWelcomePage == true).Sum(f => f.TotalHits);
                var _totalUniqueVisitorsHomePage = pagestatus.Where(w => w.IsWelcomePage == true).Sum(f => f.TotalUniqueUsers);

                // Save the changes to the reporting database
                EntityTenantWeb site = null;
                if (_context.EntitiesWebs.Any(es => es.WebGuid == _web.Id))
                {
                    site = _context.EntitiesWebs.FirstOrDefault(fs => fs.WebGuid == _web.Id);
                }
                else
                {
                    site = new EntityTenantWeb()
                    {
                        WebGuid = _web.Id,
                        WebUrl = _siteUrl,
                        SiteCreatedDate = _createdDate
                    };
                    _context.EntitiesWebs.Add(site);
                }

                site.WebTitle = _web.Title; // consistently changed by end user
                site.DTUPD = DateTime.UtcNow;
                site.TenantSiteLookup = siteId;
                site.SiteTemplateId = _web.WebTemplate;
                site.SiteLastModified = _webLastItemModifiedDate;
                site.SiteActivity = TryParseDouble(_webSiteActivity.TotalDays, 0);
                site.SiteIsAddIn = _webAddIn;
                site.SiteMetadata = hasMetadataList;
                site.SiteMetadataCount = metadataItemCount;
                site.SiteMetadataPermissions = metadataUniqueRolePermissions;
                site.PermUnique = hasUniquePerm;
                site.PermAssociatedOwner = hasAssociatedOwner;
                site.PermAssociatedMember = hasAssociatedMember;
                site.PermAssociatedVisitor = hasAssociatedVisitor;
                site.SiteOwners = _siteowners;
                site.DocumentsCount = _totalDocumentItemsCount;
                site.DocumentLastEditedDate = _lastDocumentEdited;
                site.DocumentActivityStatus = _siteDocumentActivity;
                site.WelcomePage = welcomePage;
                site.WelcomePageError = welcomePageFailed;
                site.TotalHits = _totalHits;
                site.TotalUniqueVisitors = _totalUniqueVisitors;
                site.TotalHitsHomePage = _totalHitsHomePage;
                site.UniqueVisitorsHomePage = _totalUniqueVisitorsHomePage;
                site.HasCommunity = _hasCommunity;
                site.MemberJoinLastDate = _memberJoinedLastDate;
                site.MemberJoinCount = _memberTotalItemCount;
                site.DiscussionCount = _discussionCount;
                site.DiscussionLastDate = _discussionLastDate;
                site.DiscussionReplyCount = _discussionRepliesCount;
                site.DiscussionReplyLastDate = _discussionReplyLastDate;

                var sitePages = pagestatus.Select(spage =>
                {
                    JsonAnalyticsSitePage page = new JsonAnalyticsSitePage()
                    {
                        SiteGuid = spage.SiteGuid,
                        WebGuid = spage.WebGuid,
                        ListGuid = spage.ListGuid,
                        PageGuid = spage.PageGuid,
                        PageId = spage.PageId,
                        PageUrl = spage.Url,
                        EditedDate = spage.Edited,
                        EditedUser = spage.EditedUser,
                        IsWelcomePage = spage.IsWelcomePage,
                        TotalHits = spage.TotalHits,
                        TotalUniqueVisitors = spage.TotalUniqueUsers
                    };
                    return page;
                });
                site.TotalPagesJson = Newtonsoft.Json.JsonConvert.SerializeObject(sitePages);

                if (totalDiscussions.Any())
                {
                    site.DiscussionJSON = Newtonsoft.Json.JsonConvert.SerializeObject(totalDiscussions);
                }

                if (!site.WebUrl.Equals(_siteUrl, StringComparison.CurrentCultureIgnoreCase))
                {
                    var siteUrls = new List<JsonAnalyticsSiteUrl>();
                    if (!string.IsNullOrEmpty(site.TotalUrlsJson))
                    {
                        LogVerbose($"Found JSON storage for URLs for the site {site.WebTitle}");
                        siteUrls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonAnalyticsSiteUrl>>(site.TotalUrlsJson);
                    }

                    var oldUrlObject = new JsonAnalyticsSiteUrl()
                    {
                        LastSeenDate = site.DTUPD,
                        URL = site.WebUrl
                    };

                    siteUrls.Add(oldUrlObject);

                    site.WebUrl = _siteUrl;
                    site.TotalUrlsJson = Newtonsoft.Json.JsonConvert.SerializeObject(siteUrls);
                }


                var rowsProcessed = _context.SaveChanges();
                LogVerbose("Rows {0} processed", rowsProcessed);
                return true;
            }
            catch (Exception e)
            {
                LogError(e, "Failed to process site {0} with message {1}", _siteUrl, e.Message);
            }

            return false;
        }

        internal List<ListStatistic> GetDiscussionBoards(ClientContext _ctx, List discussionBoard)
        {
            var discussions = new List<ListStatistic>();
            if (discussionBoard.ItemCount > 0)
            {
                try
                {
                    var camlViewClause = CAML.ViewFields((
                        new string[] {
                            "Title",
                            "FileRef",
                            "Body",
                            "Id",
                            "Question",
                            "MyAuthor",
                            "MyEditor",
                            "Author",
                            "Editor",
                            "ParentItemEditor",
                            "LastReplyBy",
                            "ItemChildCount",
                            "IsFeatured",
                            ConstantsListFields.Field_Created,
                            ConstantsListFields.Field_Modified
                        }).Select(s => CAML.FieldRef(s)).ToArray());
                    //Get the topics in the list
                    var discussionQuery = new CamlQuery
                    {
                        ViewXml = CAML.ViewQuery(ViewScope.DefaultValue, string.Empty, string.Empty, camlViewClause, 100),
                        ListItemCollectionPosition = null
                    };

                    LogVerbose($"Executing Discussion Board {discussionBoard.Title} with {discussionBoard.ItemCount}# discussions");

                    do
                    {
                        LogVerbose($"Paging execution {(discussionQuery?.ListItemCollectionPosition?.PagingInfo ?? "None")}");
                        ListItemCollection allDiscussions = discussionBoard.GetItems(discussionQuery);
                        _ctx.Load(allDiscussions, ctxlst => ctxlst.ListItemCollectionPosition);
                        _ctx.ExecuteQueryRetry(userAgent: _UserAgent);
                        discussionQuery.ListItemCollectionPosition = allDiscussions.ListItemCollectionPosition;

                        foreach (var discussion in allDiscussions)
                        {
                            var itemCount = discussion.RetrieveListItemValue("ItemChildCount").ToInt32(0);
                            var discussionauthor = discussion.RetrieveListItemUserValue("Author");
                            var discussioneditor = discussion.RetrieveListItemUserValue("Editor");
                            var discussionItem = new ListStatistic()
                            {
                                RelativeUrl = discussion.RetrieveListItemValue("FileRef"),
                                ListTemplateType = ListTemplateType.DiscussionBoard,
                                LastItemUserModifiedDate = discussion.RetrieveListItemValue(ConstantsListFields.Field_Modified).ToDateTime(),
                                ListName = discussionBoard.Title,
                                ItemCount = itemCount,
                                FirstItemCreated = discussion.RetrieveListItemValue(ConstantsListFields.Field_Created).ToDateTime(),
                                Replies = new List<ListDiscussionReplyStatistic>()
                            };

                            if (itemCount > 0)
                            {
                                //Get the replies of the selected topic
                                var replyQuery = new CamlQuery
                                {
                                    ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, string.Empty, string.Empty, camlViewClause, 100),
                                    ListItemCollectionPosition = null,
                                    FolderServerRelativeUrl = discussionItem.RelativeUrl
                                };

                                var replyItems = new List<ListDiscussionReplyStatistic>();
                                while (true)
                                {
                                    ListItemCollection replies = discussionBoard.GetItems(replyQuery);
                                    _ctx.Load(replies, ctxreply => ctxreply.ListItemCollectionPosition);
                                    _ctx.ExecuteQueryRetry(userAgent: _UserAgent);
                                    replyQuery.ListItemCollectionPosition = replies.ListItemCollectionPosition;

                                    foreach (var reply in replies)
                                    {
                                        var author = reply.RetrieveListItemUserValue("Author");
                                        var editor = reply.RetrieveListItemUserValue("Editor");
                                        replyItems.Add(new ListDiscussionReplyStatistic
                                        {
                                            RelativeUrl = reply.RetrieveListItemValue("FileRef"),
                                            AuthorName = author != null ? author.Email : string.Empty,
                                            LastItemUserModifiedDate = reply.RetrieveListItemValue(ConstantsListFields.Field_Modified).ToDateTime()
                                        });
                                    }

                                    if (replyQuery.ListItemCollectionPosition == null)
                                    {
                                        break;
                                    }
                                }

                                if (replyItems.Count > 0)
                                {
                                    discussionItem.Replies = replyItems;
                                }
                                LogVerbose($"Executed Discussion Board - Replies ({replyItems.Count}) CAML Query for {discussionItem.RelativeUrl}");
                            }
                            discussions.Add(discussionItem);
                        }
                    }
                    while (discussionQuery.ListItemCollectionPosition != null);
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error {0}", ex.Message);
                }
            }
            return discussions;
        }

        internal decimal TryParseDouble(double? value, int defaultValue = 0)
        {
            if (value.HasValue)
            {
                return value.Value.TryParseDecimal(defaultValue);
            }

            return 0;
        }



        /// <summary>
        /// Process the appinstance
        /// </summary>
        /// <param name="_web"></param>
        /// <param name="appInstanceId"></param>
        internal ScanAddInModel ProcessAddinInstance(Web _web, Guid appInstanceId, bool evaluateFullDepth = false)
        {
            var _webUrl = TokenHelper.EnsureTrailingSlash(_web.Url);
            LogVerbose("Checking installed app .. {0} .. {1} ", _webUrl, appInstanceId);

            AppInstance appInstance = _web.GetAppInstanceById(appInstanceId);
            _web.Context.Load(appInstance);
            _web.Context.ExecuteQueryRetry(userAgent: _UserAgent);

            var _addInTokens = new List<string>
            {
                "~appWebUrl",
                "~controlTemplates",
                "~hostUrl",
                "~layouts",
                "~remoteAppUrl",
                "~site",
                "~sitecollection"
            };
            var appDetails = new ScanAddInModel(_webUrl, appInstance);

            if (!string.IsNullOrEmpty(appInstance.RemoteAppUrl)
                || (!string.IsNullOrEmpty(appInstance.StartPage) && !_addInTokens.Any(tok => appInstance.StartPage.IndexOf(tok, StringComparison.InvariantCultureIgnoreCase) > -1)))
            {
                appDetails.HostedType = AddInEnum.ProviderHosted;
            }
            else
            {
                appDetails.HostedType = AddInEnum.SharePointHosted;
            }

            if (evaluateFullDepth)
            {
                try
                {
                    // Pulls from the Store and App Catalog
                    var appInstanceDetails = AppCatalog.GetAppDetails(_web.Context, _web, appInstance);
                    _web.Context.Load(appInstanceDetails, actx => actx.EulaUrl, actx => actx.PrivacyUrl, actx => actx.Publisher, actx => actx.ShortDescription, actx => actx.SupportUrl);
                    _web.Context.ExecuteQueryRetry(userAgent: _UserAgent);

                    if (appInstanceDetails != null && appInstanceDetails.ServerObjectIsNull == false)
                    {
                        appDetails.LoadAppDetails(appInstanceDetails);
                    }
                }
                catch (Exception appDetailEx)
                {
                    LogWarning("Failed to retreive app {0} instance details {1}", appInstanceId, appDetailEx.Message);
                }

                try
                {
                    var appInstanceEx = AppCatalog.GetAppInstance(_web.Context, _web, appInstanceId);
                    _web.Context.Load(appInstanceEx);
                    _web.Context.ExecuteQueryRetry(userAgent: _UserAgent);
                }
                catch (Exception appDetailEx)
                {
                    LogError(appDetailEx, $"Failed to process AppWeb Instance Id {appDetailEx.Message}");
                }

                try
                {
                    // Pulls from App Instance Permission XML
                    var appInstancePermissions = AppCatalog.GetAppPermissionDescriptions(_web.Context, _web, appInstance);
                    _web.Context.ExecuteQueryRetry(userAgent: _UserAgent);

                    if (appInstancePermissions != null && appInstancePermissions.Value != null)
                    {
                        appDetails.LoadAppPermissions(appInstancePermissions.Value);
                    }
                }
                catch (Exception appPermEx)
                {
                    LogWarning($"Failed to retreive app {appInstanceId} instance permissions {appPermEx.Message}");
                }
            }
            return appDetails;
        }



    }
}