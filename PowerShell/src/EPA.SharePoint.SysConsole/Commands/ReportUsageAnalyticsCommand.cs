using CommandLine;
using EPA.Office365;
using EPA.Office365.Diagnostics;
using EPA.Office365.Graph.Reporting;
using EPA.Office365.oAuth;
using Microsoft.Graph;
using System;
using System.Linq;
using System.Net.Http;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb(name: "reportusage", HelpText = "Scan Office 365 Usage & Analytics")]
    public class ReportUsageAnalyticsOptions : CommonOptions
    {
        [Option('r', "reportType", Required = true, HelpText = "Report type to process")]
        public ReportUsageEnum ReportType { get; set; }

        [Option('p', "period", Required = true, SetName = "Period", HelpText = "Report period")]
        public ReportUsagePeriodEnum Period { get; set; }

        [Option("details", Required = true, SetName = "ReportDate", HelpText = "Report period")]
        public bool Details { get; set; }

        [Option("reportDate", Required = false, SetName = "ReportDate", HelpText = "Timespan in which to report the data")]
        public DateTime? ReportDate { get; set; }

        [Option("beta-endpoint", Required = false, HelpText = "Should the process use the Graph Beta endpoint for processing.")]
        public bool BetaEndPoint { get; set; }

        [Option("resource-uri", Required = false, HelpText = "Additional MS Graph endpoint for claiming tokens.")]
        public string ResourceUri { get; set; }
    }

    public static class ReportUsageAnalyticsCommandExtension
    {
        public static int RunGenerateAndReturnExitCode(this ReportUsageAnalyticsOptions opts, IAppSettings appSettings, Serilog.ILogger logger)
        {
            var cmd = new ReportUsageAnalyticsCommand(opts, appSettings, logger);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Consumes Office 365 Report and Usage Graph API endpoints
    /// </summary>
    public class ReportUsageAnalyticsCommand : BaseAdalCommand<ReportUsageAnalyticsOptions>
    {
        public ReportUsageAnalyticsCommand(ReportUsageAnalyticsOptions opts, IAppSettings settings, Serilog.ILogger traceLogger)
            : base(opts, settings, traceLogger)
        {
        }

        public override void OnBeginInit()
        {
            var useInteractiveLogin = false;
            var scopes = new string[] { Settings.Graph.DefaultScope };
            Settings.AzureAd.ClientId = Settings.SpoADALepaReporting.ClientId;
            Settings.AzureAd.ClientSecret = Settings.SpoADALepaReporting.ClientSecret;
            Settings.AzureAd.PostLogoutRedirectURI = Opts.ResourceUri ?? ConstantsAuthentication.GraphResourceId;
            Settings.AzureAd.MSALScopes = scopes.ToArray();

            var tokenCache = new AzureADv2TokenCache(Settings, TraceLogger, useInteractiveLogin);
            TokenCache = tokenCache;

            Client = GraphClientFactory.Create(tokenCache.AuthenticationProvider);
        }

        internal HttpClient Client { get; private set; }

        public override int OnRun()
        {
            var _graphProps = new CustomGraphReportProperties()
            {
                Period = Opts.Period,
                Date = Opts.ReportDate ?? DateTime.UtcNow.AddDays(-45)
            };

            Log.LogInformation($"Report => (Usage Type {Opts.ReportType}) (Period {Opts.Period})");

            // process for reporting
            if (Opts.ReportType == ReportUsageEnum.Office365)
            {
                using var reporter = new CustomGraphReportVisitorOffice365(Settings, _graphProps, TraceLogger, Client);
                reporter.ProcessReporting(Opts.Details);
            }
            else if (Opts.ReportType == ReportUsageEnum.OneDrive)
            {
                using var reporter = new CustomGraphReportVisitorOneDrive(Settings, _graphProps, TraceLogger, Client);
                reporter.ProcessReporting(Opts.Details);
            }
            else if (Opts.ReportType == ReportUsageEnum.SharePoint)
            {
                using var reporter = new CustomGraphReportVisitorSharePoint(Settings, _graphProps, TraceLogger, Client);
                reporter.ProcessReporting(Opts.Details);
            }
            else if (Opts.ReportType == ReportUsageEnum.Skype)
            {
                using var reporter = new CustomGraphReportVisitorSkype(Settings, _graphProps, TraceLogger, Client);
                reporter.ProcessReporting(Opts.Details);
            }
            else if (Opts.ReportType == ReportUsageEnum.MSTeams)
            {
                using var reporter = new CustomGraphReportVisitorMSTeams(Settings, _graphProps, TraceLogger, Client);
                reporter.ProcessReporting(Opts.Details);
            }
            else
            {
                Log.LogWarning($"Invalid report usage selected");
            }

            return 1;
        }
    }
}
