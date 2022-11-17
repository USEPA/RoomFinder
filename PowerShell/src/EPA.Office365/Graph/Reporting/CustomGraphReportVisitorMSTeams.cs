using EPA.Office365.Database;
using EPA.Office365.Diagnostics;
using EPA.Office365.Extensions;
using EPA.Office365.Graph.Reporting.TenantReport;
using EPA.Office365.oAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using System;
using System.Linq;
using System.Net.Http;
using TinyCsvParser.Model;

namespace EPA.Office365.Graph.Reporting
{
    public class CustomGraphReportVisitorMSTeams : CustomGraphReportVisitor
    {
        internal AnalyticDbContext ReportingDatabase { get; private set; }

        public CustomGraphReportVisitorMSTeams(IAppSettings appSettings, ICustomGraphReportProperties properties, Serilog.ILogger logger, HttpClient client)
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
                var lastActivityDate = ReportingDatabase.EntitiesEntityMSTeamsActivityUserDetail.Select(m => m.LastActivityDate).Distinct();
                var lastRunDate = FirstRunDate(lastActivityDate, GraphReportProperties.Date);
                foreach (var reportDate in EachDay(lastRunDate, currentRunTilDate))
                {
                    ProcessMSTeamsActivityUserDetail(reportDate);
                }

                // iterate the report dates and store in the database
                var usageLastDate = ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageUserDetail
                    .Where(w => w.LastActivityDate.HasValue).Select(m => m.LastActivityDate.Value).Distinct();
                var usageRunDate = FirstRunDate(usageLastDate, GraphReportProperties.Date);
                foreach (var reportDate in EachDay(usageRunDate, currentRunTilDate))
                {
                    ProcessMSTeamsDeviceUsageDetail(reportDate);
                }

                OnProcessRollup();
            }
            else
            {
                ProcessMSTeamsUserActivityCounts();
                ProcessMSTeamsActivityUserCounts();
                ProcessMSTeamsDeviceUsageDistributionUserCounts();
                ProcessMSTeamsDeviceUsageUserCounts();
            }
        }

        public void ProcessMSTeamsActivityUserDetail(DateTime reportDate)
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getTeamsUserActivityUserDetail:f} reporting...");
            var results = ReportingProcessor.ProcessReport<MSTeamsActivityUserDetail>(GraphReportProperties.Period, reportDate, defaultRows);
            var rowProcessed = results.Count();
            if (rowProcessed <= 0)
            {
                Log.LogWarning($"No records were retreived for {reportDate}... skip ahead a date.");
                return;
            }

            var maxdates = results.Where(d => d.LastActivityDate > DateTime.MinValue).Select(gb => (gb.LastActivityDate)).Distinct().ToList();
            var dbentities = ReportingDatabase.EntitiesEntityMSTeamsActivityUserDetail.AsQueryable().Where(w => maxdates.Contains(w.LastActivityDate)).ToList();
            var rowdx = 0; var totaldx = rowProcessed;

            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                var lastActivityDate = result.LastActivityDate;
                if (lastActivityDate == default || lastActivityDate == DateTime.MinValue)
                {
                    lastActivityDate = result.ReportRefreshDate;
                }

                EntityMSTeamsActivityUserDetail dbentity = null;
                if(dbentities.Any(od => od.LastActivityDate == lastActivityDate && od.UPN == result.UPN))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.LastActivityDate == lastActivityDate && od.UPN == result.UPN);
                }
                else
                {
                    dbentity = new EntityMSTeamsActivityUserDetail()
                    {
                        LastActivityDate = lastActivityDate,
                        UPN = result.UPN
                    };
                    ReportingDatabase.EntitiesEntityMSTeamsActivityUserDetail.Add(dbentity);
                    dbentities.Add(dbentity);

                    dbentity.DeletedDate = result.DeletedDate;
                    dbentity.ProductsAssigned = result.RealizedProductsAssigned;
                    dbentity.ReportPeriod = result.ReportPeriod;
                    dbentity.ReportRefreshDate = result.ReportRefreshDate;
                    dbentity.Deleted = result.Deleted;
                    dbentity.CallCount = result.CallCount;
                    dbentity.MeetingCount = result.MeetingCount;
                    dbentity.PrivateChatMessageCount = result.PrivateChatMessageCount;
                    dbentity.TeamChatMessageCount = result.TeamChatMessageCount;
                    dbentity.HasOtherAction = result.HasOtherAction;
                }

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowProcessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessMSTeamsDeviceUsageDetail(DateTime reportDate)
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getTeamsDeviceUsageUserDetail:f} reporting...");
            var results = ReportingProcessor.ProcessReport<MSTeamsDeviceUsageUserDetail>(GraphReportProperties.Period, reportDate, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for {reportDate}... skip ahead a date.");
                return;
            }

            DateTime.TryParse("2014-01-01", out DateTime nulldefaultdate);

            var rowdx = 0; var totaldx = rowprocessed;
            var reportChunks = results.ChunkBy(25);
            foreach (var chunk in reportChunks)
            {
                // Return the dates so we can filter the EF query to possible Key overlaps
                var specificUsers = chunk.Select(gb => gb.UPN).Distinct().ToList();

                // pull all records for the distinct UPN's
                var dbsiteentities = ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageUserDetail.AsQueryable().Where(w => specificUsers.Contains(w.UPN)).ToList();

                foreach (var result in chunk)
                {
                    rowdx++;
                    totaldx--;

                    var lastActivityDate = result.LastActivityDate;
                    if (lastActivityDate == default || lastActivityDate == DateTime.MinValue)
                    {
                        lastActivityDate = result.ReportRefreshDate;
                    }

                    EntityMSTeamsDeviceUsageUserDetail dbentity = null;
                    if (dbsiteentities.Any(od => od.UPN == result.UPN)
                        || ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageUserDetail.Any(od => od.UPN == result.UPN))
                    {
                        dbentity = dbsiteentities.FirstOrDefault(od => od.UPN == result.UPN);
                        if (dbentity == null)
                        {
                            dbentity = ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageUserDetail.FirstOrDefault(od => od.UPN == result.UPN);
                        }
                    }
                    else
                    {
                        dbentity = new EntityMSTeamsDeviceUsageUserDetail()
                        {
                            LastActivityDate = lastActivityDate,
                            UPN = result.UPN,
                            ReportPeriod = result.ReportPeriod,
                            UsedAndroidPhone = false,
                            UsediOS = false,
                            UsedMac = false,
                            UsedWeb = false,
                            UsedWindows = false,
                            UsedWindowsPhone = false
                        };
                        ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageUserDetail.Add(dbentity);
                        dbsiteentities.Add(dbentity);
                    }


                    dbentity.ReportRefreshDate = result.ReportRefreshDate;
                    if (result?.UsedWeb?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsedWeb = true;
                        dbentity.UsedWebLastDate = result.ReportRefreshDate;
                    }
                    if (result?.UsedAndroidPhone?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsedAndroidPhone = true;
                        dbentity.UsedAndroidPhoneLastDate = result.ReportRefreshDate;
                    }
                    if (result?.UsediOS?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsediOS = true;
                        dbentity.UsediOSLastDate = result.ReportRefreshDate;
                    }
                    if (result?.UsedMac?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsedMac = true;
                        dbentity.UsedMacLastDate = result.ReportRefreshDate;
                    }
                    if (result?.UsedWindows?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsedWindows = true;
                        dbentity.UsedWindowsLastDate = result.ReportRefreshDate;
                    }
                    if (result?.UsedWindowsPhone?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsedWindowsPhone = true;
                        dbentity.UsedWindowsPhoneLastDate = result.ReportRefreshDate;
                    }


                    if (rowdx >= throttle || totaldx <= 0)
                    {
                        var itrrowprocessed = ReportingDatabase.SaveChanges();
                        Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                        rowdx = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Process the rollup Stored Procedures
        /// </summary>
        public override void OnProcessRollup()
        {
            base.OnProcessRollup();

            var rowchanges = ReportingDatabase.Database.ExecuteSqlRaw("[dbo].[MSTeams_01_UsageMonthlyDates]");
            Log.LogInformation($"Usage monthly dates called with {rowchanges} rows");

#if !NETSTANDARD2_0
            ReportingDatabase.Database.SetCommandTimeout(240);
#else
            ReportingDatabase.Database.SetCommandTimeout(new TimeSpan(0, 4, 0));
#endif
            var rollupchanges = ReportingDatabase.Database.ExecuteSqlRaw("[dbo].[MSTeams_02_UsageMonthlyRollupOffice]");
            Log.LogInformation($"Usage rollup by office processed {rollupchanges} rows");

        }

        public void ProcessMSTeamsUserActivityCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getTeamsUserActivityCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<MSTeamsActivityActivityCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntityMSTeamsActivityActivityCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityMSTeamsActivityActivityCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntityMSTeamsActivityActivityCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntityMSTeamsActivityActivityCounts.Add(dbentity);
                }

                dbentity.Calls = result.Calls ?? 0;
                dbentity.Meetings = result.Meetings ?? 0;
                dbentity.PrivateChatMessages = result.PrivateChatMessages ?? 0;
                dbentity.TeamChatMessages = result.TeamChatMessages ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessMSTeamsActivityUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getTeamsUserActivityUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<MSTeamsActivityUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntityMSTeamsActivityUserCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityMSTeamsActivityUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntityMSTeamsActivityUserCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntityMSTeamsActivityUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.Calls = result.Calls ?? 0;
                dbentity.Meetings = result.Meetings ?? 0;
                dbentity.OtherActions = result.OtherActions ?? 0;
                dbentity.PrivateChatMessages = result.PrivateChatMessages ?? 0;
                dbentity.TeamChatMessages = result.TeamChatMessages ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessMSTeamsDeviceUsageDistributionUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getTeamsDeviceUsageDistributionUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<MSTeamsDeviceUsageDistributionUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var dbentities = ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageDistributionUserCounts.ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityMSTeamsDeviceUsageDistributionUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportRefreshDate == result.ReportRefreshDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportRefreshDate == result.ReportRefreshDate);
                }
                else
                {
                    dbentity = new EntityMSTeamsDeviceUsageDistributionUserCounts()
                    {
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageDistributionUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AndroidPhone = result.AndroidPhone ?? 0;
                dbentity.iOS = result.iOS ?? 0;
                dbentity.Mac = result.Mac ?? 0;
                dbentity.Windows = result.Windows ?? 0;
                dbentity.WindowsPhone = result.WindowsPhone ?? 0;
                dbentity.Web = result.Web ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessMSTeamsDeviceUsageUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getTeamsDeviceUsageUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<MSTeamsDeviceUsageUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageUserCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityMSTeamsDeviceUsageUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntityMSTeamsDeviceUsageUserCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntityMSTeamsDeviceUsageUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AndroidPhone = result.AndroidPhone ?? 0;
                dbentity.iOS = result.iOS ?? 0;
                dbentity.Mac = result.Mac ?? 0;
                dbentity.Web = result.Web ?? 0;
                dbentity.Windows = result.Windows ?? 0;
                dbentity.WindowsPhone = result.WindowsPhone ?? 0;

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
