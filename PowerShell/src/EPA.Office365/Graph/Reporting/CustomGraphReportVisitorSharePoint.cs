using EPA.Office365.Database;
using EPA.Office365.Diagnostics;
using EPA.Office365.Graph.Reporting.TenantReport;
using EPA.Office365.oAuth;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Http;

namespace EPA.Office365.Graph.Reporting
{
    public class CustomGraphReportVisitorSharePoint : CustomGraphReportVisitor
    {
        internal AnalyticDbContext ReportingDatabase { get; private set; }

        public CustomGraphReportVisitorSharePoint(IAppSettings appSettings, ICustomGraphReportProperties properties, Serilog.ILogger logger, HttpClient client)
            : base(properties, logger, client)
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
            dbContextOptionsBuilder.UseSqlServer(appSettings.ConnectionStrings.AnalyticsConnection);
            ReportingDatabase = new AnalyticDbContext(dbContextOptionsBuilder.Options);
        }

        /// <summary>
        /// <seealso cref="CustomGraphReportVisitor.ProcessReporting(bool)"/>
        /// </summary>
        /// <param name="details">(OPTIONAL) if specified should run detail endpoints</param>
        public override void ProcessReporting(bool details)
        {
            if (details)
            {
                var currentRunUTC = DateTime.UtcNow.Date;
                var currentRunTilDate = currentRunUTC;

                // iterate the report dates and store in the database
                var lastActivityDate = ReportingDatabase.EntitiesGraphSharePointActivityUserDetail.Select(m => m.LastActivityDate).Distinct();
                var lastRunDate = FirstRunDate(lastActivityDate, GraphReportProperties.Date);
                foreach (var reportDate in EachDay(lastRunDate, currentRunTilDate))
                {
                    ProcessSharePointActivityUserDetail(reportDate);
                }

                // iterate the report dates and store in the database
                var usageLastDate = ReportingDatabase.EntitiesGraphSharePointSiteUsageDetail.Select(m => m.LastActivityDate).Distinct();
                var usageRunDate = FirstRunDate(usageLastDate, GraphReportProperties.Date);
                foreach (var reportDate in EachDay(usageRunDate, currentRunTilDate))
                {
                    ProcessSharePointSiteUsageDetail(reportDate);
                }

                OnProcessRollup();
            }
            else
            {
                ProcessSharePointActivityFileCounts();
                ProcessSharePointActivityPages();
                ProcessSharePointActivityUserCounts();
                ProcessSharePointSiteUsageFileCounts();
                ProcessSharePointSiteUsagePages();
                ProcessSharePointSiteUsageSiteCounts();
                ProcessSharePointSiteUsageStorage();
            }
        }

        public void ProcessSharePointActivityFileCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointActivityFileCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointActivityFileCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesGraphSharePointActivityFileCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphSharePointActivityFileCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntityGraphSharePointActivityFileCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesGraphSharePointActivityFileCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.SharedExternally = result.SharedExternally ?? 0;
                dbentity.SharedInternally = result.SharedInternally ?? 0;
                dbentity.Synced = result.Synced ?? 0;
                dbentity.ViewedOrEdited = result.ViewedOrEdited ?? 0;


                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSharePointActivityPages()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointActivityPages:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointActivityPages>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesGraphSharePointActivityPagesCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphSharePointActivityPagesCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntityGraphSharePointActivityPagesCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesGraphSharePointActivityPagesCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.VisitedPageCount = result.VisitedPageCount ?? 0;


                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSharePointActivityUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointActivityUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointActivityUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesGraphSharePointActivityUserCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphSharePointActivityUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntityGraphSharePointActivityUserCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesGraphSharePointActivityUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.SharedExternally = result.SharedExternally ?? 0;
                dbentity.SharedInternally = result.SharedInternally ?? 0;
                dbentity.Synced = result.Synced ?? 0;
                dbentity.ViewedOrEdited = result.ViewedOrEdited ?? 0;
                dbentity.VisitedPage = result.VisitedPage ?? 0;


                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSharePointActivityUserDetail(DateTime reportDate)
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointActivityUserDetail:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointActivityUserDetail>(GraphReportProperties.Period, reportDate, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for {reportDate}... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => (gb.LastActivityDate)).Distinct().ToList();
            var dbentities = ReportingDatabase.EntitiesGraphSharePointActivityUserDetail.AsQueryable().Where(w => maxdates.Contains(w.LastActivityDate)).ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                var lastActivityDate = result.LastActivityDate;

                EntityGraphSharePointActivityUserDetail dbentity = null;
                if (dbentities.Any(od => od.LastActivityDate == lastActivityDate && od.UserPrincipalName == result.UserPrincipalName))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.LastActivityDate == lastActivityDate && od.UserPrincipalName == result.UserPrincipalName);
                }
                else
                {
                    dbentity = new EntityGraphSharePointActivityUserDetail()
                    {
                        LastActivityDate = lastActivityDate,
                        UserPrincipalName = result.UserPrincipalName
                    };
                    ReportingDatabase.EntitiesGraphSharePointActivityUserDetail.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.DeletedDate = result.DeletedDate;
                dbentity.IsDeleted = result.IsDeleted;
                dbentity.ProductsAssigned = result.RealizedProductsAssigned;
                dbentity.ReportPeriod = result.ReportPeriod;
                dbentity.ReportRefreshDate = result.ReportRefreshDate;
                dbentity.SharedExternallyFileCount = result.SharedExternallyFileCount;
                dbentity.SharedInternallyFileCount = result.SharedInternallyFileCount;
                dbentity.SyncedFileCount = result.SyncedFileCount;
                dbentity.ViewedOrEditedFileCount = result.ViewedOrEditedFileCount;
                dbentity.VisitedPageCount = result.VisitedPageCount;

                if (rowdx >= 25 || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSharePointSiteUsageDetail(DateTime reportDate)
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointSiteUsageDetail:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointSiteUsageSiteDetail>(GraphReportProperties.Period, reportDate, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for {reportDate}... skip ahead a date.");
                return;
            }

            DateTime.TryParse("2014-01-01", out DateTime nulldefaultdate);

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.LastActivityDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesGraphSharePointSiteUsageDetail.AsQueryable().Where(w => maxdates.Contains(w.LastActivityDate)).ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                var lastActivityDate = result.LastActivityDate;

                EntityGraphSharePointSiteUsageDetail dbentity = null;
                if (dbentities.Any(od => od.LastActivityDate == lastActivityDate && od.SiteURL == result.SiteURL))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.LastActivityDate == lastActivityDate && od.SiteURL == result.SiteURL);
                }
                else
                {
                    dbentity = new EntityGraphSharePointSiteUsageDetail()
                    {
                        LastActivityDate = lastActivityDate,
                        SiteURL = result.SiteURL
                    };
                    ReportingDatabase.EntitiesGraphSharePointSiteUsageDetail.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.ReportPeriod = result.ReportPeriod;
                dbentity.ReportRefreshDate = result.ReportRefreshDate;
                dbentity.OwnerDisplayName = result.OwnerDisplayName;
                dbentity.IsDeleted = result.IsDeleted;
                dbentity.FileCount = result.FileCount ?? 0;
                dbentity.ActiveFileCount = result.ActiveFileCount ?? 0;
                dbentity.PageViewCount = result.PageViewCount ?? 0;
                dbentity.VisitedPageCount = result.VisitedPageCount ?? 0;
                dbentity.StorageAllocated_Byte = result.StorageAllocated_Byte ?? 0;
                dbentity.StorageUsed_Byte = result.StorageUsed_Byte ?? 0;
                dbentity.RootWebTemplate = result.RootWebTemplate;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSharePointSiteUsageFileCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointSiteUsageFileCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointSiteUsageFileCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesGraphSharePointSiteUsageFileCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphSharePointSiteUsageFileCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate && od.SiteType == result.SiteType))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate && od.SiteType == result.SiteType);
                }
                else
                {
                    dbentity = new EntityGraphSharePointSiteUsageFileCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate,
                        SiteType = result.SiteType
                    };
                    ReportingDatabase.EntitiesGraphSharePointSiteUsageFileCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.Active = result.Active ?? 0;
                dbentity.Total = result.Total ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSharePointSiteUsagePages()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointSiteUsagePages:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointSiteUsagePages>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesGraphSharePointSiteUsagePages.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphSharePointSiteUsagePages dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate && od.SiteType == result.SiteType))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate && od.SiteType == result.SiteType);
                }
                else
                {
                    dbentity = new EntityGraphSharePointSiteUsagePages()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate,
                        SiteType = result.SiteType
                    };
                    ReportingDatabase.EntitiesGraphSharePointSiteUsagePages.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.PageViewCount = result.PageViewCount ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSharePointSiteUsageSiteCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointSiteUsageSiteCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointSiteUsageSiteCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesGraphSharePointSiteUsageSiteCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                var lastActivityDate = result.ReportDate;

                EntityGraphSharePointSiteUsageSiteCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == lastActivityDate && od.SiteType == result.SiteType))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == lastActivityDate && od.SiteType == result.SiteType);
                }
                else
                {
                    dbentity = new EntityGraphSharePointSiteUsageSiteCounts()
                    {
                        ReportDate = lastActivityDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate,
                        SiteType = result.SiteType
                    };
                    ReportingDatabase.EntitiesGraphSharePointSiteUsageSiteCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.Total = result.Total ?? 0;
                dbentity.Active = result.Active ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSharePointSiteUsageStorage()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSharePointSiteUsageStorage:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SharePointSiteUsageStorage>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesGraphSharePointSiteUsageStorage.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                var lastActivityDate = result.ReportDate;

                EntityGraphSharePointSiteUsageStorage dbentity = null;
                if (dbentities.Any(od => od.ReportDate == lastActivityDate && od.SiteType == result.SiteType))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == lastActivityDate && od.SiteType == result.SiteType);
                }
                else
                {
                    dbentity = new EntityGraphSharePointSiteUsageStorage()
                    {
                        ReportDate = lastActivityDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate,
                        SiteType = result.SiteType
                    };
                    ReportingDatabase.EntitiesGraphSharePointSiteUsageStorage.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.StorageUsed_Byte = result.StorageUsed_Byte ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        protected override void OnDisposing()
        {

            ReportingDatabase.Dispose();
        }
    }
}
