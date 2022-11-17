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
    public class CustomGraphReportVisitorSkype : CustomGraphReportVisitor
    {
        internal AnalyticDbContext ReportingDatabase { get; private set; }

        public CustomGraphReportVisitorSkype(IAppSettings appSettings, ICustomGraphReportProperties properties, Serilog.ILogger logger, HttpClient client)
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
                var lastActivityDate = ReportingDatabase.EntitiesEntitySkypeForBusinessActivityUserDetail.Select(m => m.LastActivityDate).Distinct();
                var lastRunDate = FirstRunDate(lastActivityDate, GraphReportProperties.Date);
                foreach (var reportDate in EachDay(lastRunDate, currentRunTilDate))
                {
                    ProcessSkypeActivityUserDetail(reportDate);
                }

                // iterate the report dates and store in the database
                var usageLastDate = ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageUserDetail
                    .Where(w => w.LastActivityDate.HasValue).Select(m => m.LastActivityDate.Value).Distinct();
                var usageRunDate = FirstRunDate(usageLastDate, GraphReportProperties.Date);
                foreach (var reportDate in EachDay(usageRunDate, currentRunTilDate))
                {
                    ProcessSkypeDeviceUsageDetail(reportDate);
                }

                OnProcessRollup();
            }
            else
            {
                ProcessSkypeForBusinessActivityActivityCounts();
                ProcessSkypeForBusinessActivityUserCounts();
                ProcessSkypeForBusinessDeviceUsageDistributionUserCounts();
                ProcessSkypeForBusinessDeviceUsageUserCounts();
                ProcessSkypeForBusinessOrganizerActivityCounts();
                ProcessSkypeForBusinessOrganizerActivityMinuteCounts();
                ProcessSkypeForBusinessOrganizerActivityUserCounts();
                ProcessSkypeForBusinessParticipantActivityCounts();
                ProcessSkypeForBusinessParticipantActivityMinuteCounts();
                ProcessSkypeForBusinessParticipantActivityUserCounts();
                ProcessSkypeForBusinessPeerToPeerActivityCounts();
                ProcessSkypeForBusinessPeerToPeerActivityMinuteCounts();
                ProcessSkypeForBusinessPeerToPeerActivityUserCounts();
            }
        }

        /// <summary>
        /// Process the rollup Stored Procedures
        /// </summary>
        public override void OnProcessRollup()
        {
            base.OnProcessRollup();


            // TODO: Call Skype procedure  [Dates], [Rollup]

            var rowchanges = ReportingDatabase.Database.ExecuteSqlRaw("[dbo].[Skype_01_UsageMonthlyDates]");
            Log.LogInformation($"Usage monthly dates called with {rowchanges} rows");

#if !NETSTANDARD2_0
            ReportingDatabase.Database.SetCommandTimeout(240);
#else
            ReportingDatabase.Database.SetCommandTimeout(new TimeSpan(0, 4, 0));
#endif
            var rollupchanges = ReportingDatabase.Database.ExecuteSqlRaw("[dbo].[Skype_02_UsageMonthlyRollupOffice]");
            Log.LogInformation($"Usage rollup by office processed {rollupchanges} rows");

        }

        public void ProcessSkypeForBusinessActivityActivityCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessActivityCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessActivityActivityCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessActivityActivityCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessActivityActivityCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessActivityActivityCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessActivityActivityCounts.Add(dbentity);
                }

                dbentity.Organized = result.Organized ?? 0;
                dbentity.Participated = result.Participated ?? 0;
                dbentity.PeerToPeer = result.PeerToPeer ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessActivityUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessActivityUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessActivityUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessActivityUserCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessActivityUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessActivityUserCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessActivityUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.Organized = result.Organized ?? 0;
                dbentity.Participated = result.Participated ?? 0;
                dbentity.PeerToPeer = result.PeerToPeer ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessDeviceUsageDistributionUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessDeviceUsageDistributionUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessDeviceUsageDistributionUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageDistributionUserCounts.ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessDeviceUsageDistributionUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportRefreshDate == result.ReportRefreshDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportRefreshDate == result.ReportRefreshDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessDeviceUsageDistributionUserCounts()
                    {
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageDistributionUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AndroidPhone = result.AndroidPhone ?? 0;
                dbentity.iPad = result.IPad ?? 0;
                dbentity.iPhone = result.IPhone ?? 0;
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

        public void ProcessSkypeForBusinessDeviceUsageUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessDeviceUsageUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessDeviceUsageUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageUserCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessDeviceUsageUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessDeviceUsageUserCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AndroidPhone = result.AndroidPhone ?? 0;
                dbentity.iPad = result.IPad ?? 0;
                dbentity.iPhone = result.IPhone ?? 0;
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

        public void ProcessSkypeForBusinessOrganizerActivityCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessOrganizerActivityCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessOrganizerActivityCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessOrganizerActivityCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessOrganizerActivityCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessOrganizerActivityCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessOrganizerActivityCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AppSharing = result.AppSharing ?? 0;
                dbentity.AudioVideo = result.AudioVideo ?? 0;
                dbentity.DialInOut3rdParty = result.DialInOut3rdParty ?? 0;
                dbentity.DialInOutMicrosoft = result.DialInOutMicrosoft ?? 0;
                dbentity.IM = result.IM ?? 0;
                dbentity.Web = result.Web ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessOrganizerActivityMinuteCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessOrganizerActivityMinuteCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessOrganizerActivityMinuteCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessOrganizerActivityMinuteCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessOrganizerActivityMinuteCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessOrganizerActivityMinuteCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessOrganizerActivityMinuteCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AudioVideo = result.AudioVideo ?? 0;
                dbentity.DialInMicrosoft = result.DialInMicrosoft ?? 0;
                dbentity.DialOutMicrosoft = result.DialOutMicrosoft ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessOrganizerActivityUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessOrganizerActivityUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessOrganizerActivityUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessOrganizerActivityUserCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessOrganizerActivityUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessOrganizerActivityUserCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessOrganizerActivityUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AppSharing = result.AppSharing ?? 0;
                dbentity.AudioVideo = result.AudioVideo ?? 0;
                dbentity.DialInOut3rdParty = result.DialInOut3rdParty ?? 0;
                dbentity.DialInOutMicrosoft = result.DialInOutMicrosoft ?? 0;
                dbentity.IM = result.IM ?? 0;
                dbentity.Web = result.Web ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessParticipantActivityCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessParticipantActivityCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessParticipantActivityCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessParticipantActivityCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessParticipantActivityCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessParticipantActivityCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessParticipantActivityCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AppSharing = result.AppSharing ?? 0;
                dbentity.AudioVideo = result.AudioVideo ?? 0;
                dbentity.DialInOut3rdParty = result.DialInOut3rdParty ?? 0;
                dbentity.IM = result.IM ?? 0;
                dbentity.Web = result.Web ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessParticipantActivityMinuteCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessParticipantActivityMinuteCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessParticipantActivityMinuteCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessParticipantActivityMinuteCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessParticipantActivityMinuteCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessParticipantActivityMinuteCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessParticipantActivityMinuteCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AudioVideo = result.AudioVideo ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessParticipantActivityUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessParticipantActivityUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessParticipantActivityUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessParticipantActivityUserCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessParticipantActivityUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessParticipantActivityUserCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessParticipantActivityUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AppSharing = result.AppSharing ?? 0;
                dbentity.AudioVideo = result.AudioVideo ?? 0;
                dbentity.DialInOut3rdParty = result.DialInOut3rdParty ?? 0;
                dbentity.IM = result.IM ?? 0;
                dbentity.Web = result.Web ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessPeerToPeerActivityCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessPeerToPeerActivityCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessPeerToPeerActivityCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessPeerToPeerActivityCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessPeerToPeerActivityCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessPeerToPeerActivityCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessPeerToPeerActivityCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AppSharing = result.AppSharing ?? 0;
                dbentity.Audio = result.Audio ?? 0;
                dbentity.FileTransfer = result.FileTransfer ?? 0;
                dbentity.IM = result.IM ?? 0;
                dbentity.Video = result.Video ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessPeerToPeerActivityMinuteCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessPeerToPeerActivityMinuteCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessPeerToPeerActivityMinuteCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessPeerToPeerActivityMinuteCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessPeerToPeerActivityMinuteCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessPeerToPeerActivityMinuteCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessPeerToPeerActivityMinuteCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.Audio = result.Audio ?? 0;
                dbentity.Video = result.Video ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeForBusinessPeerToPeerActivityUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessPeerToPeerActivityUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessPeerToPeerActivityUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Select(gb => gb.ReportDate).Distinct().ToList();

            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessPeerToPeerActivityUserCounts.AsQueryable().Where(w => maxdates.Contains(w.ReportDate)).ToList();
            var rowdx = 0; var totaldx = results.Count();
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntitySkypeForBusinessPeerToPeerActivityUserCounts dbentity = null;
                if (dbentities.Any(od => od.ReportDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.ReportDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessPeerToPeerActivityUserCounts()
                    {
                        ReportDate = result.ReportDate,
                        ReportPeriod = result.ReportPeriod,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessPeerToPeerActivityUserCounts.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.AppSharing = result.AppSharing ?? 0;
                dbentity.Audio = result.Audio ?? 0;
                dbentity.FileTransfer = result.FileTransfer ?? 0;
                dbentity.IM = result.IM ?? 0;
                dbentity.Video = result.Video ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeActivityUserDetail(DateTime reportDate)
        {
            // TODO: date intervals
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessActivityUserDetail:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessActivityUserDetail>(GraphReportProperties.Period, reportDate, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived for {reportDate}... skip ahead a date.");
                return;
            }

            // Return the dates so we can filter the EF query to possible Key overlaps
            var maxdates = results.Where(d => d.LastActivityDate > DateTime.MinValue).Select(gb => (gb.LastActivityDate)).Distinct().ToList();
            var dbentities = ReportingDatabase.EntitiesEntitySkypeForBusinessActivityUserDetail.AsQueryable().Where(w => maxdates.Contains(w.LastActivityDate)).ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                var lastActivityDate = result.LastActivityDate;
                if (lastActivityDate == default || lastActivityDate == DateTime.MinValue)
                {
                    lastActivityDate = reportDate;
                }

                EntitySkypeForBusinessActivityUserDetail dbentity = null;
                if (dbentities.Any(od => od.LastActivityDate == lastActivityDate && od.UPN == result.UPN))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.LastActivityDate == lastActivityDate && od.UPN == result.UPN);
                }
                else
                {
                    dbentity = new EntitySkypeForBusinessActivityUserDetail()
                    {
                        LastActivityDate = lastActivityDate,
                        UPN = result.UPN
                    };
                    ReportingDatabase.EntitiesEntitySkypeForBusinessActivityUserDetail.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.DeletedDate = result.DeletedDate;
                dbentity.ProductsAssigned = result.RealizedProductsAssigned;
                dbentity.ReportPeriod = result.ReportPeriod;
                dbentity.ReportRefreshDate = result.ReportRefreshDate;
                dbentity.Deleted = result.Deleted;
                dbentity.OrganizedConferenceAppSharingCount = result.OrganizedConferenceAppSharingCount ?? 0;
                dbentity.OrganizedConferenceAudioVideoCount = result.OrganizedConferenceAudioVideoCount ?? 0;
                dbentity.OrganizedConferenceAudioVideoMinutes = result.OrganizedConferenceAudioVideoMinutes ?? 0;
                dbentity.OrganizedConferenceCloudDialInMicrosoftMinutes = result.OrganizedConferenceCloudDialInMicrosoftMinutes ?? 0;
                dbentity.OrganizedConferenceCloudDialInOutMicrosoftCount = result.OrganizedConferenceCloudDialInOutMicrosoftCount ?? 0;
                dbentity.OrganizedConferenceCloudDialOutMicrosoftMinutes = result.OrganizedConferenceCloudDialOutMicrosoftMinutes ?? 0;
                dbentity.OrganizedConferenceDialInOut3rdPartyCount = result.OrganizedConferenceDialInOut3rdPartyCount ?? 0;
                dbentity.OrganizedConferenceIMCount = result.OrganizedConferenceIMCount ?? 0;
                dbentity.OrganizedConferenceLastActivityDate = result.OrganizedConferenceLastActivityDate;
                dbentity.OrganizedConferenceWebCount = result.OrganizedConferenceWebCount ?? 0;
                dbentity.ParticipatedConferenceAppSharingCount = result.ParticipatedConferenceAppSharingCount ?? 0;
                dbentity.ParticipatedConferenceAudioVideoCount = result.ParticipatedConferenceAudioVideoCount ?? 0;
                dbentity.ParticipatedConferenceAudioVideoMinutes = result.ParticipatedConferenceAudioVideoMinutes ?? 0;
                dbentity.ParticipatedConferenceDialInOut3rdPartyCount = result.ParticipatedConferenceDialInOut3rdPartyCount ?? 0;
                dbentity.ParticipatedConferenceIMCount = result.ParticipatedConferenceIMCount ?? 0;
                dbentity.ParticipatedConferenceLastActivityDate = result.ParticipatedConferenceLastActivityDate;
                dbentity.ParticipatedConferenceWebCount = result.ParticipatedConferenceWebCount ?? 0;
                dbentity.PeerToPeerAppSharingCount = result.PeerToPeerAppSharingCount ?? 0;
                dbentity.PeerToPeerAudioCount = result.PeerToPeerAudioCount ?? 0;
                dbentity.PeerToPeerAudioMinutes = result.PeerToPeerAudioMinutes ?? 0;
                dbentity.PeerToPeerFileTransferCount = result.PeerToPeerFileTransferCount ?? 0;
                dbentity.PeerToPeerIMCount = result.PeerToPeerIMCount ?? 0;
                dbentity.PeerToPeerLastActivityDate = result.PeerToPeerLastActivityDate;
                dbentity.PeerToPeerVideoCount = result.PeerToPeerVideoCount ?? 0;
                dbentity.PeerToPeerVideoMinutes = result.PeerToPeerVideoMinutes ?? 0;
                dbentity.TotalOrganizedConferenceCount = result.TotalOrganizedConferenceCount ?? 0;
                dbentity.TotalParticipatedConferenceCount = result.TotalParticipatedConferenceCount ?? 0;
                dbentity.TotalPeerToPeerSessionCount = result.TotalPeerToPeerSessionCount ?? 0;


                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }

        public void ProcessSkypeDeviceUsageDetail(DateTime reportDate)
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getSkypeForBusinessDeviceUsageUserDetail:f} reporting...");
            var results = ReportingProcessor.ProcessReport<SkypeForBusinessDeviceUsageUserDetail>(GraphReportProperties.Period, reportDate, defaultRows);
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
                var dbsiteentities = ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageUserDetail.AsQueryable().Where(w => specificUsers.Contains(w.UPN)).ToList();

                foreach (var result in chunk)
                {
                    rowdx++;
                    totaldx--;

                    var lastActivityDate = result.LastActivityDate ?? reportDate;
                    if (lastActivityDate == default || lastActivityDate == DateTime.MinValue)
                    {
                        lastActivityDate = reportDate;
                    }

                    EntitySkypeForBusinessDeviceUsageUserDetail dbentity = null;
                    if (dbsiteentities.Any(od => od.UPN == result.UPN)
                        || ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageUserDetail.Any(od => od.UPN == result.UPN))
                    {
                        dbentity = dbsiteentities.FirstOrDefault(od => od.UPN == result.UPN);
                        if (dbentity == null)
                        {
                            dbentity = ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageUserDetail.FirstOrDefault(od => od.UPN == result.UPN);
                        }
                    }
                    else
                    {
                        dbentity = new EntitySkypeForBusinessDeviceUsageUserDetail()
                        {
                            LastActivityDate = lastActivityDate,
                            UPN = result.UPN,
                            ReportPeriod = result.ReportPeriod,
                            UsedAndroidPhone = false,
                            UsediPad = false,
                            UsediPhone = false,
                            UsedWindows = false,
                            UsedWindowsPhone = false
                        };
                        ReportingDatabase.EntitiesEntitySkypeForBusinessDeviceUsageUserDetail.Add(dbentity);
                        dbsiteentities.Add(dbentity);
                    }


                    dbentity.ReportRefreshDate = result.ReportRefreshDate;
                    if (result?.UsedAndroidPhone?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsedAndroidPhone = true;
                        dbentity.UsedAndroidPhoneLastDate = result.ReportRefreshDate;
                    }
                    if (result?.UsediPad?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsediPad = true;
                        dbentity.UsediPadLastDate = result.ReportRefreshDate;
                    }
                    if (result?.UsediPhone?.Equals("yes", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        dbentity.LastActivityDate = result.ReportRefreshDate;
                        dbentity.UsediPhone = true;
                        dbentity.UsediPhoneLastDate = result.ReportRefreshDate;
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

        protected override void OnDisposing()
        {

            ReportingDatabase.Dispose();
        }
    }
}
