using CommandLine;
using EPA.Office365;
using EPA.Office365.Database;
using EPA.Office365.Graph;
using EPA.Office365.Graph.Reporting.TenantReport;
using EPA.Office365.oAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinyCsvParser;
using TinyCsvParser.Mapping;

namespace EPA.SharePoint.SysConsole.Commands
{
    /// <summary>
    /// Enumerate Unified Groups and Process into the Database
    /// </summary>
    [Verb(name: "getoffice365groups", HelpText = "Retreive Office 365 Groups")]
    public class GetAnalyticO365GroupsOptions : CommonOptions
    {
        [Option("resource-uri", Required = false, HelpText = "The URI of the resource to query")]
        public string ResourceUri { get; set; }

        [Option("starts-with-letter", Required = false, Default = "A", HelpText = "When retrieving Unified Groups via For Loop, only return groups starting with this letter. Ex: for (char c = 'A'; c <= 'Z'; c++)")]
        public string StartsWithLetter { get; set; }

        [Option("ends-with-letter", Required = false, Default = "Z", HelpText = "When retrieving Unified Groups via For Loop, only return groups through this letter. Ex: for (char c = 'A'; c <= 'Z'; c++)")]
        public string EndsWithLetter { get; set; }

        [Option("starts-with-number", Required = false, Default = 0, HelpText = "When retrieving Unified Groups via For Loop, return groups starting with numbers. Ex: for (int i = 0; i <= 9; i++)")]
        public int StartsWithNumber { get; set; }
    }

    public static class GetAnalyticO365GroupsExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetAnalyticO365GroupsOptions opts, IAppSettings appSettings, Serilog.ILogger logger)
        {
            var cmd = new GetAnalyticO365GroupsCommand(opts, appSettings, logger);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Query the tenant to discover all O365 Groups; 
    ///     query the Usage and Analytics for the SharePoint Sites
    ///     Save the result into the External Database for future processing
    /// </summary>
    /// <remarks>
    /// MSAL Scopes required: ("Group.Read.All", "MailboxSettings.ReadWrite")
    /// </remarks>
    public class GetAnalyticO365GroupsCommand : BaseAdalCommand<GetAnalyticO365GroupsOptions>
    {
        public GetAnalyticO365GroupsCommand(GetAnalyticO365GroupsOptions opts, IAppSettings settings, Serilog.ILogger traceLogger)
            : base(opts, settings, traceLogger)
        {
        }

        public override void OnBeginInit()
        {
            var useInteractiveLogin = false;
            var scopes = new string[] { Settings.Graph.DefaultScope };

            Settings.AzureAd.ClientId = Settings?.SpoADALepaReporting.ClientId;
            Settings.AzureAd.ClientSecret = Settings?.SpoADALepaReporting.ClientSecret;
            Settings.AzureAd.PostLogoutRedirectURI = ConstantsAuthentication.GraphResourceId;
            Settings.AzureAd.MSALScopes = scopes.ToArray();

            if (string.IsNullOrEmpty(Settings.AzureAd.ClientSecret) || string.IsNullOrEmpty(Settings.AzureAd.ClientId))
            {
                throw new Exception($"You must set credentials for spoAddInMakeEPASite");
            }

            TokenCache = new AzureADv2TokenCache(Settings, TraceLogger, useInteractiveLogin);
        }

        /// <summary>
        /// Process the O365 Groups based on group requests
        /// </summary>
        public override int OnRun()
        {
            var groupCount = 0;
            var connectionstring = Settings.ConnectionStrings.AnalyticsConnection;
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
            dbContextOptionsBuilder.UseSqlServer(connectionstring);

            char startingLetter = Convert.ToChar(Opts.StartsWithLetter);
            char endingLetter = Convert.ToChar(Opts.EndsWithLetter);
            bool startsWithNumber = Convert.ToBoolean(Opts.StartsWithNumber);

            using var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options);

            // Query Reporting CSV
            var o365DataResult = GetOffice365GroupsActivityDetail();

            var utility = new GraphUtility(TokenCache, TraceLogger);
            var client = utility.CreateGraphClient(retryCount: 15, delay: 750);

            // Retrieve all the groups
            Task.Run(async () =>
            {
                for (char c = startingLetter; c <= endingLetter; c++)
                {
                    var _unifiedGroups = await utility.ListUnifiedGroups(includeSite: true, includeOwner: true, includeHasTeam: true, includeClassification: true, startsWith: c.ToString());
                    foreach (var _group in _unifiedGroups)
                    {
                        groupCount++;
                        ProcessUnifiedGroup(_context, _group, o365DataResult);
                        var rowProcessed = _context.SaveChanges();
                        TraceLogger.Information($"The Group, {_group.DisplayName}, was saved to the DB.");
                        TraceLogger.Information($"Processed {rowProcessed} rows");
                    }
                }

                if (startsWithNumber)
                {
                    for (int c = 0; c <= 9; c++)
                    {
                        var _unifiedGroups = await utility.ListUnifiedGroups(includeSite: true, includeOwner: true, includeHasTeam: true, includeClassification: true, startsWith: c.ToString());
                        foreach (var _group in _unifiedGroups)
                        {
                            groupCount++;
                            ProcessUnifiedGroup(_context, _group, o365DataResult);
                            var rowProcessed = _context.SaveChanges();
                            TraceLogger.Information("Processed {0} rows", rowProcessed);
                        }
                    }
                }
            }).GetAwaiter().GetResult();

            TraceLogger.Information("Total groups: {0}", groupCount);
            return 1;
        }

        internal void ProcessUnifiedGroup(AnalyticDbContext _context, UnifiedGroupEntity _group, IEnumerable<CsvMappingResult<Office365GroupsActivityDetail>> o365DataResult)
        {
            var siteUrl = string.Empty;
            var groupId = _group.GroupId;

            foreach (var item in o365DataResult.Where(fd => fd.Result.GroupId?.Trim() == groupId))
            {
                _group.ReportRefreshDate = item.Result.ReportRefreshDate;
                _group.IsDeleted = item.Result.IsDeleted;
                _group.LastActivityDate = item.Result.LastActivityDate;
                _group.MemberCount = item.Result.MemberCount;
                _group.ExchangeReceivedEmailCount = item.Result.ExchangeReceivedEmailCount;
                _group.SharePointActiveFileCount = item.Result.SharePointActiveFileCount;
                _group.ExchangeMailboxTotalItemCount = item.Result.ExchangeMailboxTotalItemCount;
                _group.ExchangeMailboxStorageUsed_Byte = item.Result.ExchangeMailboxStorageUsed_Byte;
                _group.SharePointTotalFileCount = item.Result.SharePointTotalFileCount;
                _group.SharePointSiteStorageUsed_Byte = item.Result.SharePointSiteStorageUsed_Byte;
                _group.ReportPeriod = item.Result.ReportPeriod;
                break;
            }

            EntityAnalyticsUnifiedGroup unifiedGroup = null;
            if (_context.EntitiesUnifiedGroup.Any(ug => ug.GroupId == groupId))
            {
                unifiedGroup = _context.EntitiesUnifiedGroup.FirstOrDefault(ug => ug.GroupId == groupId);
                unifiedGroup = UpdateGroupDetails(unifiedGroup, _group);
            }
            else
            {
                unifiedGroup = new EntityAnalyticsUnifiedGroup()
                {
                    GroupName = _group.DisplayName,
                    GroupId = _group.GroupId,
                    DTADDED = DateTime.UtcNow
                };
                unifiedGroup = UpdateGroupDetails(unifiedGroup, _group);
                _context.EntitiesUnifiedGroup.Add(unifiedGroup);
            }

        }

        internal EntityAnalyticsUnifiedGroup UpdateGroupDetails(EntityAnalyticsUnifiedGroup unifiedGroup, UnifiedGroupEntity _group)
        {
            unifiedGroup.GroupId = _group.GroupId;
            unifiedGroup.GroupName = _group.DisplayName;
            unifiedGroup.Site = _group.SiteUrl;
            unifiedGroup.AllowExternalSenders = _group.AllowExternalSenders;
            unifiedGroup.AutoSubscribeNewMembers = _group.AutoSubscribeNewMembers;
            unifiedGroup.MailAddress = _group.Mail;
            unifiedGroup.MailEnabled = _group.MailEnabled;
            unifiedGroup.PublicPrivate = _group.Visibility;
            unifiedGroup.DTUPD = DateTime.UtcNow;
            unifiedGroup.CreatedDate = _group.Created;
            unifiedGroup.ReportRefreshDate = _group.ReportRefreshDate;
            unifiedGroup.IsDeleted = _group.IsDeleted;
            unifiedGroup.LastActivityDate = _group.LastActivityDate;
            unifiedGroup.MemberCount = _group.MemberCount;
            unifiedGroup.ExchangeReceivedEmailCount = _group.ExchangeReceivedEmailCount;
            unifiedGroup.SharePointActiveFileCount = _group.SharePointActiveFileCount;
            unifiedGroup.ExchangeMailboxTotalItemCount = _group.ExchangeMailboxTotalItemCount;
            unifiedGroup.ExchangeMailboxStorageUsed_Byte = _group.ExchangeMailboxStorageUsed_Byte;
            unifiedGroup.SharePointTotalFileCount = _group.SharePointTotalFileCount;
            unifiedGroup.SharePointSiteStorageUsed_Byte = _group.SharePointSiteStorageUsed_Byte;
            unifiedGroup.ReportPeriod = _group.ReportPeriod;
            unifiedGroup.Owners = string.Join(";", _group?.GroupOwners.Select(sel => sel.UserPrincipalName));
            unifiedGroup.PrimaryOwner = _group?.PrimaryOwner?.UserPrincipalName;
            unifiedGroup.IsSiteProvisioned = _group.IsSiteProvisioned;

            if (_group.HasTeam == true)
            {
                unifiedGroup.TeamDisplayName = _group.TeamDisplayName;
                unifiedGroup.TeamDescription = _group.TeamDescription;
                unifiedGroup.TeamInternalId = _group.TeamInternalId;
                unifiedGroup.TeamClassification = _group.TeamClassification;
                unifiedGroup.TeamSpecialization = _group.TeamSpecialization;
                unifiedGroup.TeamVisibility = _group.TeamVisibility;
                unifiedGroup.TeamDiscoverySettings = _group.TeamDiscoverySettings;
                unifiedGroup.TeamResponseHeaders = _group.TeamResponseHeaders;
                unifiedGroup.TeamStatusCode = _group.TeamStatusCode;
                unifiedGroup.TeamIsArchived = _group.TeamIsArchived;
                unifiedGroup.TeamWebUrl = _group.TeamWebUrl;
            }
            return unifiedGroup;
        }


        /// <summary>
        /// Parses reporting endpoint to retreive CSV of reports
        /// </summary>
        /// <returns></returns>
        private List<CsvMappingResult<Office365GroupsActivityDetail>> GetOffice365GroupsActivityDetail()
        {
            var accessToken = TokenCache.AccessTokenAsync(string.Empty).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            List<CsvMappingResult<Office365GroupsActivityDetail>> result = null;

            try
            {
                string getGroupUrl = $"{GraphHttpHelper.MicrosoftGraphV1BaseUri}reports/getOffice365GroupsActivityDetail(date={DateTime.Now.AddDays(-3):yyyy-MM-dd})";

                var getGroupResult = GraphHttpHelper.MakeGetRequestForString(
                    getGroupUrl,
                    accessToken: accessToken);

                CsvParserOptions csvParserOptions = new CsvParserOptions(true, ',');
                CsvReaderOptions csvReaderOptions = new CsvReaderOptions(new[] { Environment.NewLine });
                CsvOffice365GroupsActivityDetailMapping csvMapper = new CsvOffice365GroupsActivityDetailMapping();
                CsvParser<Office365GroupsActivityDetail> csvParser = new CsvParser<Office365GroupsActivityDetail>(csvParserOptions, csvMapper);

                result = csvParser
                    .ReadFromString(csvReaderOptions, getGroupResult)
                    .ToList();
            }
            catch (ServiceException e)
            {
                TraceLogger.Information(e, $"GetOffice365GroupsActivityDetail Error:{e.Message}");
            }

            return result;
        }
    }
}