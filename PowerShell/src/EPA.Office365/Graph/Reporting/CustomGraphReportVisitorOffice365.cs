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
    public class CustomGraphReportVisitorOffice365 : CustomGraphReportVisitor
    {
        internal AnalyticDbContext ReportingDatabase { get; private set; }

        public CustomGraphReportVisitorOffice365(IAppSettings appSettings, ICustomGraphReportProperties properties, Serilog.ILogger logger, HttpClient client)
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
                var lastActivityDate = ReportingDatabase.EntitiesPreviewActiveUserDetail.Select(m => m.ReportRefreshDate).Distinct();
                var lastRunDate = FirstRunDate(lastActivityDate, GraphReportProperties.Date, 5); // 5 days is the max for this Endpoint
                foreach (var reportDate in EachDay(lastRunDate, currentRunTilDate))
                {
                    ProcessOffice365ActiveUserDetail(reportDate);
                }

                OnProcessRollup();
            }
            else
            {
                ProcessOffice365ServicesUserCounts();

                ProcessOffice365ActiveUserCounts();
            }
        }

        public void ProcessOffice365ActiveUserDetail(DateTime reportDate)
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getOffice365ActiveUserDetail:f} reporting...");
            var results = ReportingProcessor.ProcessReport<Office365ActiveUsersUserDetail>(GraphReportProperties.Period, reportDate, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                var lastActivityDate = result.ReportRefreshDate;
                if (lastActivityDate == default || lastActivityDate == DateTime.MinValue)
                {
                    lastActivityDate = reportDate;
                }

                var userPrincipalName = result.UPN.ToLower().Trim();

                EntityGraphO365ActiveUserDetails dbentity = null;
                if (ReportingDatabase.EntitiesPreviewActiveUserDetail.Any(od => od.UPN == userPrincipalName))
                {
                    dbentity = ReportingDatabase.EntitiesPreviewActiveUserDetail.FirstOrDefault(od => od.UPN == userPrincipalName);
                }
                else
                {
                    dbentity = new EntityGraphO365ActiveUserDetails()
                    {
                        UPN = userPrincipalName,
                        DisplayName = result.DisplayName
                    };
                    ReportingDatabase.EntitiesPreviewActiveUserDetail.Add(dbentity);
                }

                dbentity.ReportRefreshDate = lastActivityDate;
                dbentity.Deleted = result.Deleted;
                dbentity.DeletedDate = result.DeletedDate;
                dbentity.LicenseForExchange = result.LicenseForExchange;
                dbentity.LicenseForOneDrive = result.LicenseForOneDrive;
                dbentity.LicenseForSharePoint = result.LicenseForSharePoint;
                dbentity.LicenseForSkypeForBusiness = result.LicenseForSkypeForBusiness;
                dbentity.LicenseForYammer = result.LicenseForYammer;
                dbentity.LicenseForTeams = result.LicenseForMSTeams;
                dbentity.LastActivityDateForExchange = result.LastActivityDateForExchange;
                dbentity.LastActivityDateForOneDrive = result.LastActivityDateForOneDrive;
                dbentity.LastActivityDateForSharePoint = result.LastActivityDateForSharePoint;
                dbentity.LastActivityDateForSkypeForBusiness = result.LastActivityDateForSkypeForBusiness;
                dbentity.LastActivityDateForYammer = result.LastActivityDateForYammer;
                dbentity.LastActivityDateForTeams = result.LastActivityDateForMSTeams;
                dbentity.LicenseAssignedDateForExchange = result.LicenseAssignedDateForExchange;
                dbentity.LicenseAssignedDateForOneDrive = result.LicenseAssignedDateForOneDrive;
                dbentity.LicenseAssignedDateForSharePoint = result.LicenseAssignedDateForSharePoint;
                dbentity.LicenseAssignedDateForSkypeForBusiness = result.LicenseAssignedDateForSkypeForBusiness;
                dbentity.LicenseAssignedDateForYammer = result.LicenseAssignedDateForYammer;
                dbentity.LicenseAssignedDateForTeams = result.LicenseAssignedDateForMSTeams;
                dbentity.ProductsAssigned = result.ProductsAssignedCSV;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }


        public void ProcessOffice365ServicesUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getOffice365ServicesUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<Office365ActiveUsersServicesUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            var dbentities = ReportingDatabase.EntitiesPreviewActiveUserService.ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphO365ActiveUserService dbentity = null;
                if (dbentities.Any(od => od.LastActivityDate == result.ReportRefreshDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.LastActivityDate == result.ReportRefreshDate);
                }
                else
                {
                    dbentity = new EntityGraphO365ActiveUserService()
                    {
                        LastActivityDate = result.ReportRefreshDate,
                        ReportingPeriodDays = result.ReportingPeriodDays,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesPreviewActiveUserService.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.ExchangeActive = result.ExchangeActive ?? 0;
                dbentity.ExchangeInActive = result.ExchangeInActive ?? 0;
                dbentity.OneDriveActive = result.OneDriveActive ?? 0;
                dbentity.OneDriveInActive = result.OneDriveInActive ?? 0;
                dbentity.SharePointActive = result.SharePointActive ?? 0;
                dbentity.SharePointInActive = result.SharePointInActive ?? 0;
                dbentity.SkypeActive = result.SkypeActive ?? 0;
                dbentity.SkypeInActive = result.SkypeInActive ?? 0;
                dbentity.YammerActive = result.YammerActive ?? 0;
                dbentity.YammerInActive = result.YammerInActive ?? 0;
                dbentity.TeamsActive = result.MSTeamActive ?? 0;
                dbentity.TeamsInActive = result.MSTeamInActive ?? 0;

                if (rowdx >= throttle || totaldx <= 0)
                {
                    var itrrowprocessed = ReportingDatabase.SaveChanges();
                    Log.LogInformation($"Processing {rowprocessed} rows; Persisted {itrrowprocessed} rows; {totaldx} remaining");
                    rowdx = 0;
                }
            }
        }


        public void ProcessOffice365ActiveUserCounts()
        {
            Log.LogInformation($"{ReportUsageTypeEnum.getOffice365ActiveUserCounts:f} reporting...");
            var results = ReportingProcessor.ProcessReport<Office365ActiveUsersUserCounts>(GraphReportProperties.Period, GraphReportProperties.Date, defaultRows);
            var rowprocessed = results.Count();
            if (rowprocessed <= 0)
            {
                Log.LogWarning($"No records were retreived ... skip ahead a date.");
                return;
            }

            var dbentities = ReportingDatabase.EntitiesPreviewActiveUser.ToList();
            var rowdx = 0; var totaldx = rowprocessed;
            foreach (var result in results)
            {
                rowdx++;
                totaldx--;

                EntityGraphO365ActiveUsers dbentity = null;
                if (dbentities.Any(od => od.LastActivityDate == result.ReportDate))
                {
                    dbentity = dbentities.FirstOrDefault(od => od.LastActivityDate == result.ReportDate);
                }
                else
                {
                    dbentity = new EntityGraphO365ActiveUsers()
                    {
                        LastActivityDate = result.ReportDate,
                        ReportingPeriodDays = result.ReportingPeriodDays,
                        ReportRefreshDate = result.ReportRefreshDate
                    };
                    ReportingDatabase.EntitiesPreviewActiveUser.Add(dbentity);
                    dbentities.Add(dbentity);
                }

                dbentity.Office365 = result.Office365 ?? 0;
                dbentity.Exchange = result.Exchange ?? 0;
                dbentity.OneDrive = result.OneDrive ?? 0;
                dbentity.SharePoint = result.SharePoint ?? 0;
                dbentity.SkypeForBusiness = result.SkypeForBusiness ?? 0;
                dbentity.Yammer = result.Yammer ?? 0;
                dbentity.Teams = result.MSTeams ?? 0;

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
