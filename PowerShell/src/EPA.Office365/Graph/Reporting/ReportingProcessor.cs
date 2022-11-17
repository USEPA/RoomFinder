using EPA.Office365.Diagnostics;
using EPA.Office365.Exceptions;
using EPA.Office365.Extensions;
using EPA.Office365.Graph.Reporting.TenantReport;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using CSV = CsvHelper;
using CSVConfig = CsvHelper.Configuration;

namespace EPA.Office365.Graph.Reporting
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handling for grace.")]
    internal class ReportingProcessor : IDisposable
    {
        #region Properties

        internal bool IsDisposed { get; set; }

        internal HttpClient Client { get; private set; }

        #endregion

        /// <summary>
        /// Supply Azure AD Credentials and associated logging utility
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="client">HttpClient configured with Authentication Token refresh</param>
        internal ReportingProcessor(Serilog.ILogger logger, HttpClient client)
        {
            Log.InitializeLogger(logger);
            Client = client;
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }


        /// <summary>
        /// Queries the reporting endpoint with the specified filters and interpolated classes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="reportingFilters"></param>
        /// <param name="betaEndPoint"></param>
        /// <returns></returns>
        private ICollection<JsonModel> QueryBetaOrCSVMap<JsonModel, U>(QueryFilter reportingFilters, bool betaEndPoint = false)
            where JsonModel : JSONODataBase
            where U : CSVConfig.ClassMap
        {
            var successOrWillTry = false;
            var results = new List<JsonModel>();

            if (betaEndPoint)
            {
                // Switch to JSON Output
                try
                {
                    reportingFilters.FormattedOutput = ReportUsageFormatEnum.JSON;

                    var activityresults = RetrieveData<JsonModel>(reportingFilters);
                    results.AddRange(activityresults);
                    successOrWillTry = true;
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, $"Failed for JSON Format with message {ex.Message}");
                }
            }

            if (!successOrWillTry)
            {
                // Switch to CSV Output
                reportingFilters.FormattedOutput = ReportUsageFormatEnum.Default;
                reportingFilters.BetaEndPoint = false;
                var CSVConfig = new CSVConfig.CsvConfiguration(CultureInfo.CurrentCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = true
                };
                CSVConfig.RegisterClassMap<U>();

                var serviceFullUrl = reportingFilters.ToUrl();
                try
                {
                    var resultscsv = new CSV.CsvReader(RetrieveDataAsStream(reportingFilters), CSVConfig);
                    results.AddRange(resultscsv.GetRecords<JsonModel>());
                }
                catch (GraphWebException gwex)
                {
                    Log.LogError(gwex, $"{serviceFullUrl} attempt failed with {gwex}");
                }
            }

            Log.LogInformation($"Found {results.Count} while querying successOrWillTry:{successOrWillTry}");

            return results;
        }


        /// <summary>
        /// Will process the report based on the requested <typeparamref name="T"/> class
        ///     NOTE: <paramref name="betaEndPoint"/> true will default to using the JSON format; if an exception is raise it will retry with the CSV format
        ///     NOTE: <paramref name="betaEndPoint"/> false will skip JSON and use the CSV format with v1.0 endpoint
        /// </summary>
        /// <typeparam name="T">GraphAPIReport Models</typeparam>
        /// <param name="reportPeriod"></param>
        /// <param name="reportDate"></param>
        /// <param name="defaultRecordBatch"></param>
        /// <param name="betaEndPoint"></param>
        /// <returns></returns>
        internal ICollection<JsonModel> ProcessReport<JsonModel>(ReportUsagePeriodEnum reportPeriod, DateTime? reportDate, int defaultRecordBatch = 500, bool betaEndPoint = false)
            where JsonModel : JSONODataBase
        {
            var results = default(ICollection<JsonModel>);
            var reportingFilters = new QueryFilter(defaultRecordBatch, betaEndPoint)
            {
                O365Period = reportPeriod,
                Date = reportDate
            };

            if (typeof(JsonModel) == typeof(Office365ActiveUsersUserDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOffice365ActiveUserDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, Office365ActiveUsersUserDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(Office365ActiveUsersServicesUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOffice365ServicesUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, Office365ActiveUsersServicesUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(Office365GroupsActivityDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOffice365GroupsActivityDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, Office365GroupsActivityDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(Office365GroupsActivityCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOffice365GroupsActivityCounts;
                results = QueryBetaOrCSVMap<JsonModel, Office365GroupsActivityCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(Office365ActiveUsersUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOffice365ActiveUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, Office365ActiveUsersUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(OneDriveActivityFileCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOneDriveActivityFileCounts;
                results = QueryBetaOrCSVMap<JsonModel, OneDriveActivityFileCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(OneDriveActivityUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOneDriveActivityUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, OneDriveActivityUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(OneDriveActivityUserDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOneDriveActivityUserDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, OneDriveActivityUserDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(OneDriveUsageStorage))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOneDriveUsageStorage;
                results = QueryBetaOrCSVMap<JsonModel, OneDriveUsageStorageMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(OneDriveUsageFileCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOneDriveUsageFileCounts;
                results = QueryBetaOrCSVMap<JsonModel, OneDriveUsageFileCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(OneDriveUsageAccountCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOneDriveUsageAccountCounts;
                results = QueryBetaOrCSVMap<JsonModel, OneDriveUsageAccountCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(OneDriveUsageAccountDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getOneDriveUsageAccountDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, OneDriveUsageAccountDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointActivityFileCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointActivityFileCounts;
                results = QueryBetaOrCSVMap<JsonModel, SharePointActivityFileCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointActivityPages))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointActivityPages;
                results = QueryBetaOrCSVMap<JsonModel, SharePointActivityPagesMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointActivityUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointActivityUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, SharePointActivityUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointActivityUserDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointActivityUserDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, SharePointActivityUserDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointActivityUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointActivityUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, SharePointActivityUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointSiteUsageSiteDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointSiteUsageDetail;
                results = QueryBetaOrCSVMap<JsonModel, SharePointSiteUsageSiteDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointSiteUsageFileCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointSiteUsageFileCounts;
                results = QueryBetaOrCSVMap<JsonModel, SharePointSiteUsageFileCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointSiteUsagePages))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointSiteUsagePages;
                results = QueryBetaOrCSVMap<JsonModel, SharePointSiteUsagePagesMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointSiteUsageSiteCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointSiteUsageSiteCounts;
                results = QueryBetaOrCSVMap<JsonModel, SharePointSiteUsageSiteCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SharePointSiteUsageStorage))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSharePointSiteUsageStorage;
                results = QueryBetaOrCSVMap<JsonModel, SharePointSiteUsageStorageMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessActivityUserDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessActivityUserDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessActivityUserDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessActivityActivityCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessActivityCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessActivityActivityCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessActivityUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessActivityUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessActivityUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessDeviceUsageUserDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessDeviceUsageUserDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessDeviceUsageUserDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessDeviceUsageDistributionUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessDeviceUsageDistributionUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessDeviceUsageDistributionUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessDeviceUsageUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessDeviceUsageUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessDeviceUsageUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessOrganizerActivityCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessOrganizerActivityCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessOrganizerActivityCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessOrganizerActivityUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessOrganizerActivityUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessOrganizerActivityUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessOrganizerActivityMinuteCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessOrganizerActivityMinuteCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessOrganizerActivityMinuteCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessParticipantActivityCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessParticipantActivityCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessParticipantActivityCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessParticipantActivityUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessParticipantActivityUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessParticipantActivityUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessParticipantActivityMinuteCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessParticipantActivityMinuteCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessParticipantActivityMinuteCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessPeerToPeerActivityCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessPeerToPeerActivityCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessPeerToPeerActivityCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessPeerToPeerActivityUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessPeerToPeerActivityUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessPeerToPeerActivityUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(SkypeForBusinessPeerToPeerActivityMinuteCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getSkypeForBusinessPeerToPeerActivityMinuteCounts;
                results = QueryBetaOrCSVMap<JsonModel, SkypeForBusinessPeerToPeerActivityMinuteCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(MSTeamsActivityUserDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getTeamsUserActivityUserDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, MSTeamsActivityUserDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(MSTeamsDeviceUsageUserDetail))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getTeamsDeviceUsageUserDetail;
                if (betaEndPoint)
                {
                    Log.LogWarning($"{reportingFilters.O365ReportType:f} typically contains a large dataset; it is recommended to use the CSV format instead");
                }
                results = QueryBetaOrCSVMap<JsonModel, MSTeamsDeviceUsageUserDetailMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(MSTeamsActivityActivityCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getTeamsUserActivityCounts;
                results = QueryBetaOrCSVMap<JsonModel, MSTeamsActivityActivityCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(MSTeamsActivityUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getTeamsUserActivityUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, MSTeamsActivityUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(MSTeamsDeviceUsageDistributionUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getTeamsDeviceUsageDistributionUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, MSTeamsDeviceUsageDistributionUserCountsMap>(reportingFilters, betaEndPoint);
            }
            else if (typeof(JsonModel) == typeof(MSTeamsDeviceUsageUserCounts))
            {
                reportingFilters.O365ReportType = ReportUsageTypeEnum.getTeamsDeviceUsageUserCounts;
                results = QueryBetaOrCSVMap<JsonModel, MSTeamsDeviceUsageUserCountsMap>(reportingFilters, betaEndPoint);
            }


            return results;
        }

        /// <summary>
        /// Queries the Graph API and returns the emitted string output
        /// </summary>
        /// <param name="ReportType"></param>
        /// <param name="reportPeriod"></param>
        /// <param name="reportDate"></param>
        /// <param name="defaultRecordBatch">(OPTIONAL) will default to 500</param>
        /// <param name="betaEndPoint">(OPTIONAL) will default to false</param>
        /// <returns></returns>
        internal string ProcessReport(ReportUsageTypeEnum ReportType, ReportUsagePeriodEnum reportPeriod, DateTime? reportDate, int defaultRecordBatch = 500, bool betaEndPoint = false)
        {
            var serviceQuery = new QueryFilter(defaultRecordBatch, betaEndPoint)
            {
                O365ReportType = ReportType,
                O365Period = reportPeriod,
                Date = reportDate
            };

            var response = RetrieveData(serviceQuery);
            return response;
        }

        /// <summary>
        /// Builds the URI from the Reporting types and returns the streamer
        /// </summary>
        /// <param name="serviceFullUrl">The full URL to the Graph API</param>
        /// <param name="maxAttempts">total number of attempts before proceeding</param>
        /// <param name="backoffIntervalInSeconds">wait interval (in seconds) before retry</param>
        /// <returns></returns>
        internal string ExecuteResponse(Uri serviceFullUrl, int maxAttempts = 3, int backoffIntervalInSeconds = 6)
        {
            var resultResponse = string.Empty;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // Prepare the HTTP request message with the proper HTTP method
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("GET"), serviceFullUrl);

            Log.LogInformation($"Executing {serviceFullUrl}");
            // Fire the HTTP request
            using HttpResponseMessage response = Client
                .InvokeSendAsync(request, userAgent: null, maxAttempts: maxAttempts, backoffIntervalInSeconds: backoffIntervalInSeconds, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    resultResponse = response.Content.ReadAsStringAsync().Result;
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, $"Read response failed {ex.Message}");
                }

                if (string.IsNullOrEmpty(resultResponse))
                {
                    throw new Exception("Response content is Null");
                }
            }
            else
            {
                var responseException = response.Content.ReadAsStringAsync().Result;
                Log.LogWarning($"Failed to issue {serviceFullUrl} with message {responseException}");
            }

            return resultResponse;
        }


        /// <summary>
        /// Builds the URI from the Reporting types and returns the string
        /// </summary>
        /// <param name="serviceQuery">The GraphAPI URI builder object with specific settings</param>
        /// <param name="maxAttempts">total number of attempts before proceeding</param>
        /// <param name="backoffIntervalInSeconds">wait interval (in seconds) before retry</param>
        /// <returns></returns>
        internal string RetrieveData(QueryFilter serviceQuery, int maxAttempts = 3, int backoffIntervalInSeconds = 6)
        {
            var serviceFullUrl = serviceQuery.ToUrl();

            var result = ExecuteResponse(serviceFullUrl, maxAttempts, backoffIntervalInSeconds);
            return result;
        }

        /// <summary>
        /// Builds the URI from the Reporting types and returns the Deserialized Objects
        /// </summary>
        /// <typeparam name="JsonModel"></typeparam>
        /// <param name="serviceQuery">The GraphAPI URI builder object with specific settings</param>
        /// <param name="maxAttempts">total number of attempts before proceeding</param>
        /// <param name="backoffIntervalInSeconds">wait interval (in seconds) before retry</param>
        /// <returns>A deserialized collection of objects</returns>
        internal ICollection<JsonModel> RetrieveData<JsonModel>(QueryFilter serviceQuery, int maxAttempts = 3, int backoffIntervalInSeconds = 6)
            where JsonModel : class
        {
            var objects = new List<JsonModel>();
            var serviceFullUrl = serviceQuery.ToUrl();
            var lastUri = serviceFullUrl;
            var lastUris = new List<string>();

            while (true)
            {
                var result = ExecuteResponse(lastUri, maxAttempts, backoffIntervalInSeconds);
                if (string.IsNullOrEmpty(result))
                {
                    break;
                }

                var items = JsonConvert.DeserializeObject<JSONAuditCollection<JsonModel>>(result);
                objects.AddRange(items.Results);

                if (string.IsNullOrEmpty(items.NextLink))
                {
                    // last in the set
                    break;
                }
                lastUri = new Uri(items.NextLink);

                // TODO: Bug in Beta EndPoint cycled through endlessly, adding additional check to skip
                if (lastUris.Contains(items.NextLink))
                {
                    Log.LogWarning($"Write log entry for next URI {items.NextLink} and jump out");
                    break;
                }
                lastUris.Add(items.NextLink);
            }
            return objects;
        }

        /// <summary>
        /// Builds the URI from the Reporting types and returns the streamer
        /// </summary>
        /// <param name="serviceQuery">The GraphAPI URI builder object with specific settings</param>
        /// <param name="maxAttempts">total number of attempts before proceeding</param>
        /// <param name="backoffIntervalInSeconds">wait interval (in seconds) before retry</param>
        /// <returns>An open Text reader which should be disposed</returns>
        internal TextReader RetrieveDataAsStream(QueryFilter serviceQuery, int maxAttempts = 3, int backoffIntervalInSeconds = 6)
        {
            var serviceFullUrl = serviceQuery.ToUrl();

            var result = ExecuteResponse(serviceFullUrl, maxAttempts, backoffIntervalInSeconds);
            TextReader textReader = new StringReader(result);
            return textReader;
        }


        /// <summary>
        /// Dispose of the class
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing
                && !IsDisposed)
            {
            }
            IsDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
