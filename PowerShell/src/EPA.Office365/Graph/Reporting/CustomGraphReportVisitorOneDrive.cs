using EPA.Office365.Database;
using EPA.Office365.Diagnostics;
using EPA.Office365.Extensions;
using EPA.Office365.Graph.Reporting.TenantReport;
using EPA.Office365.oAuth;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Http;

namespace EPA.Office365.Graph.Reporting
{
    public class CustomGraphReportVisitorOneDrive : CustomGraphReportVisitor
    {
        internal AnalyticDbContext ReportingDatabase { get; private set; }

        public CustomGraphReportVisitorOneDrive(IAppSettings appSettings, ICustomGraphReportProperties properties, Serilog.ILogger logger, HttpClient client)
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
                var lastActivityDate = ReportingDatabase.EntitiesPreviewOneDriveActivityDetail.Select(m => m.LastActivityDate).Distinct();
                var lastRunDate = FirstRunDate(lastActivityDate, GraphReportProperties.Date);
                foreach (var reportDate in EachDay(lastRunDate, currentRunTilDate))
                {
                    ProcessOneDriveActivityUserDetail(reportDate);
                }


                // iterate the report dates and store in the database
                var usageLastDate = ReportingDatabase.EntitiesPreviewOneDriveUsageDetail.Select(m => m.LastActivityDate).Distinct();
                var usageRunDate = FirstRunDate(usageLastDate, GraphReportProperties.Date);
                foreach (var reportDate in EachDay(usageRunDate, currentRunTilDate))
                {
                    ProcessOneDriveUsageAccountDetail(reportDate);
                }

                OnProcessRollup();
            }
            else
            {
                ProcessOneDriveActivityFileCounts();
                ProcessOneDriveActivityUserCounts();
                ProcessOneDriveUsageAccountCounts();
                ProcessOneDriveUsageFileCounts();
                ProcessOneDriveUsageStorage();
            }
        }

        /// <summary>
        /// Process the Rollup stored procedures
        /// </summary>
        public override void OnProcessRollup()
        {
            base.OnProcessRollup();


            // TODO: Call OneDrive procedures  [Dates], [Rollup]

            var rowchanges = ReportingDatabase.Database.ExecuteSqlRaw("[dbo].[OD4B_01_UsageMonthlyDates]");
            Log.LogInformation($"Usage monthly dates called with {rowchanges} rows");

#if !NETSTANDARD2_0
            ReportingDatabase.Database.SetCommandTimeout(240);
#else
            ReportingDatabase.Database.SetCommandTimeout(new TimeSpan(0, 4, 0));
#endif
            var rollupOfficeChanges = ReportingDatabase.Database.ExecuteSqlRaw("[dbo].[OD4B_02_UsageMonthlyRollupOffice]");
            Log.LogInformation($"Usage rollup by office processed {rollupOfficeChanges} rows");

#if !NETSTANDARD2_0
            ReportingDatabase.Database.SetCommandTimeout(480);
#else
            ReportingDatabase.Database.SetCommandTimeout(new TimeSpan(0, 8, 0));
#endif
            var rollupUserChanges = ReportingDatabase.Database.ExecuteSqlRaw("[dbo].[OD4B_03_UsageMonthlyRollupUser]");
            Log.LogInformation($"Usage rollup by user processed {rollupUserChanges} rows");
        }

        /// <summary>
        /// getOneDriveActivityFileCounts
        /// </summary>
        public void ProcessOneDriveActivityFileCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getOneDriveActivityFileCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<OneDriveActivityFileCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for ... skip ahead a date.");
                return;
            }

            // Retreive Database Objects
            var oneDriveActivityFiles = ReportingDatabase.EntitiesPreviewOneDriveActivityFiles.ToList();

            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphOneDriveActivityFiles odactivityfile = null;
                if (oneDriveActivityFiles.Any(od => od.LastActivityDate == result.ReportDate))
                {
                    odactivityfile = oneDriveActivityFiles.FirstOrDefault(od => od.LastActivityDate == result.ReportDate);
                }
                else
                {
                    odactivityfile = new EntityGraphOneDriveActivityFiles()
                    {
                        LastActivityDate = result.ReportDate
                    };
                    ReportingDatabase.EntitiesPreviewOneDriveActivityFiles.Add(odactivityfile);
                }

                odactivityfile.ReportingPeriodInDays = result.ReportPeriod;
                odactivityfile.ODB_TotalFileViewedModified = ParseDefault(result.FilesViewedModified, 0);
                odactivityfile.ODB_TotalFileSynched = ParseDefault(result.FilesSynced, 0);
                odactivityfile.ODB_TotalFileSharedINT = ParseDefault(result.FilesSharedINT, 0);
                odactivityfile.ODB_TotalFileSharedEXT = ParseDefault(result.FilesSharedEXT, 0);

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessOneDriveActivityUserCounts()
        {

            Log.LogInformation($"{ReportUsageTypeEnum.getOneDriveActivityUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<OneDriveActivityUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for ... skip ahead a date.");
                return;
            }


            var oneDriveActivityUsers = ReportingDatabase.EntitiesPreviewOneDriveActivityUsers.ToList();

            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphOneDriveActivityUsers odactivityuser = null;
                if (oneDriveActivityUsers.Any(od => od.LastActivityDate == result.ReportDate))
                {
                    odactivityuser = oneDriveActivityUsers.FirstOrDefault(od => od.LastActivityDate == result.ReportDate);
                }
                else
                {
                    odactivityuser = new EntityGraphOneDriveActivityUsers()
                    {
                        LastActivityDate = result.ReportDate
                    };
                    ReportingDatabase.EntitiesPreviewOneDriveActivityUsers.Add(odactivityuser);
                }

                odactivityuser.ReportingPeriodInDays = result.ReportPeriod;
                odactivityuser.ODB_TotalFileViewedModified = ParseDefault(result.FilesViewedModified, 0);
                odactivityuser.ODB_TotalFileSynched = ParseDefault(result.FilesSynced, 0);
                odactivityuser.ODB_TotalFileSharedINT = ParseDefault(result.FilesSharedINT, 0);
                odactivityuser.ODB_TotalFileSharedEXT = ParseDefault(result.FilesSharedEXT, 0);

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessOneDriveActivityUserDetail(DateTime reportDate)
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getOneDriveActivityUserDetail:f} reporting.. date {reportDate}.");
            var results = ReportingProcessor.ProcessReport<OneDriveActivityUserDetail>(GraphReportProperties.Period, reportDate, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for {reportDate}... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Where(d => d.LastActivityDateUTC > DateTime.MinValue).Select(gb => gb.LastActivityDateUTC).Distinct().ToList();

            var oneDriveActivityDetail = ReportingDatabase.EntitiesPreviewOneDriveActivityDetail.AsQueryable().Where(w => maxdates.Contains(w.LastActivityDate)).ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphOneDriveActivityDetail odactivity = null;
                if (oneDriveActivityDetail.Any(od => od.LastActivityDate == result.LastActivityDateUTC && od.UPN == result.UPN))
                {
                    odactivity = oneDriveActivityDetail.FirstOrDefault(od => od.LastActivityDate == result.LastActivityDateUTC && od.UPN == result.UPN);
                }
                else
                {
                    odactivity = new EntityGraphOneDriveActivityDetail()
                    {
                        LastActivityDate = result.LastActivityDateUTC,
                        UPN = result.UPN
                    };
                    ReportingDatabase.EntitiesPreviewOneDriveActivityDetail.Add(odactivity);
                    oneDriveActivityDetail.Add(odactivity);
                }

                odactivity.Deleted = result.Deleted;
                odactivity.DeletedDate = result.DeletedDate;
                odactivity.ReportingPeriodInDays = result.ReportPeriod;
                odactivity.ODB_TotalFileViewedModified = result.FilesViewedModified ?? 0;
                odactivity.ODB_TotalFileSynched = result.SyncedFileCount ?? 0;
                odactivity.ODB_TotalFileSharedINT = result.SharedInternallyFileCount ?? 0;
                odactivity.ODB_TotalFileSharedEXT = result.SharedExternallyFileCount ?? 0;
                odactivity.ProductsAssigned = result.RealizedProductsAssigned;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessOneDriveUsageAccountCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getOneDriveUsageAccountCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<OneDriveUsageAccountCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for ... skip ahead a date.");
                return;
            }

            var oneDriveUsageAccount = ReportingDatabase.EntitiesPreviewOneDriveUsageAccount.ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphOneDriveUsageAccount odaccount = null;
                if (oneDriveUsageAccount.Any(od => od.LastActivityDate == result.ReportDate && od.SiteType == result.SiteType))
                {
                    odaccount = oneDriveUsageAccount.FirstOrDefault(od => od.LastActivityDate == result.ReportDate && od.SiteType == result.SiteType);
                }
                else
                {
                    odaccount = new EntityGraphOneDriveUsageAccount()
                    {
                        LastActivityDate = result.ReportDate,
                        SiteType = result.SiteType
                    };
                    ReportingDatabase.EntitiesPreviewOneDriveUsageAccount.Add(odaccount);
                }

                odaccount.ReportingPeriodInDays = result.ReportPeriod;
                odaccount.TotalAccounts = result.Total_Accounts ?? 0;
                odaccount.TotalActiveAccounts = result.Active_Accounts ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessOneDriveUsageFileCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getOneDriveUsageFileCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<OneDriveUsageFileCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            var dbentities = ReportingDatabase.EntitiesGraphOneDriveUsageFileCounts.ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphOneDriveUsageFileCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate && od.SiteType == result.SiteType))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate && od.SiteType == result.SiteType);
                }
                else
                {
                    dbentity = new EntityGraphOneDriveUsageFileCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        SiteType = result.SiteType
                    };
                    ReportingDatabase.EntitiesGraphOneDriveUsageFileCounts.Add(dbentity);
                }

                dbentity.ReportRefreshDate = result.ReportRefreshDate;
                dbentity.Active = result.Active;
                dbentity.Total = result.Total;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessOneDriveUsageStorage()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getOneDriveUsageStorage:f} reporting...");
            var results = ReportingProcessor.ProcessReport<OneDriveUsageStorage>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for ... skip ahead a date.");
                return;
            }

            var dbentities = ReportingDatabase.EntitiesGraphOneDriveUsageStorage.ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphOneDriveUsageStorage dbentity = null;
                if (dbentities.Any(od => od.LastActivityDate == result.ReportDate && od.SiteType == result.SiteType))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.LastActivityDate == result.ReportDate && od.SiteType == result.SiteType);
                }
                else
                {
                    dbentity = new EntityGraphOneDriveUsageStorage()
                    {
                        LastActivityDate = result.ReportDate,
                        SiteType = result.SiteType
                    };
                    ReportingDatabase.EntitiesGraphOneDriveUsageStorage.Add(dbentity);
                }

                dbentity.ReportingPeriodInDays = result.ReportingPeriodDays;
                dbentity.StorageUsedByte = result.StorageUsed_Bytes ?? 0;
                dbentity.StorageUsedMB = result.StorageUsed_Bytes.ParseInt64DecimalByPower(2);
                dbentity.StorageUsedGB = result.StorageUsed_Bytes.ParseInt64DecimalByPower(3);
                dbentity.StorageUsedTB = result.StorageUsed_Bytes.ParseInt64DecimalByPower(4);

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessOneDriveUsageAccountDetail(DateTime reportDate)
        {

            // TODO: need to iterate over Dates -
            Log.LogInformation($"{ReportUsageTypeEnum.getOneDriveUsageAccountDetail:f} reporting...");
            var results = ReportingProcessor.ProcessReport<OneDriveUsageAccountDetail>(GraphReportProperties.Period, reportDate, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for {reportDate}... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.LastActivityDateUTC).Distinct().ToList();

            var oneDriveUsageDetail = ReportingDatabase.EntitiesPreviewOneDriveUsageDetail.AsQueryable().Where(w => maxdates.Contains(w.LastActivityDate)).ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphOneDriveUsageDetail odusage = null;
                if (oneDriveUsageDetail.Any(od => od.LastActivityDate == result.LastActivityDateUTC && od.SiteURL == result.SiteURL))
                {
                    odusage = oneDriveUsageDetail.FirstOrDefault(od => od.LastActivityDate == result.LastActivityDateUTC && od.SiteURL == result.SiteURL);
                }
                else
                {
                    odusage = new EntityGraphOneDriveUsageDetail()
                    {
                        LastActivityDate = result.LastActivityDateUTC,
                        SiteURL = result.SiteURL,
                        SiteOwner = result.SiteOwner
                    };
                    ReportingDatabase.EntitiesPreviewOneDriveUsageDetail.Add(odusage);
                    oneDriveUsageDetail.Add(odusage);
                }

                odusage.Deleted = result.Deleted;
                odusage.ReportingPeriodInDays = result.ReportingPeriodDays;
                odusage.Storage_Allocated_B = result.StorageAllocatedInBytes ?? 0;
                odusage.StorageUsedByte = result.StorageUsedInBytes ?? 0;
                odusage.TotalFiles = result.Files ?? 0;
                odusage.TotalFilesViewedModified = result.FilesViewedModified ?? 0;

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
