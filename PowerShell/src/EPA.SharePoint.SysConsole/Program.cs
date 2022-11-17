using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Commands;
using EPA.SharePoint.SysConsole.Extensions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EPA.SharePoint.SysConsole
{
    class Program
    {
        private static IConfigurationRoot Configuration { get; set; }
        private static string EnvironmentName { get; set; }
        protected Program() { }

        static int Main(string[] args)
        {
            Console.WriteLine($"EPA.SharePoint.Console {DateTime.UtcNow:r}");
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            EnvironmentName = string.IsNullOrEmpty(EnvironmentName) ? "Development" : EnvironmentName;
            Console.WriteLine($"Console {EnvironmentName} with current Directory {Environment.CurrentDirectory}");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                .AddAzureKeyVaultIfAvailable();

            if (EnvironmentName.IndexOf("development", StringComparison.OrdinalIgnoreCase) > -1)
            {
                // Re-add User secrets so it takes precedent for local development
                builder.AddUserSecrets<Program>();
            }

            Configuration = builder.Build();

            var appSettings = Configuration.Get<AppSettings>();

            // read through Verbs in all Options files
            var epaVerbs = LoadVerbs();

            var argsAsList = new List<string>(args);
            var parsedArgs = Parser.Default.ParseArguments(argsAsList, epaVerbs);

            parsedArgs.WithNotParsed<object>((errs) =>
            {
                bool continueProcessing = true;
                foreach (Error e in errs)
                {
                    Console.WriteLine("ERROR " + e.Tag.ToString());
                    continueProcessing = !e.StopsProcessing && continueProcessing;
                }
                if (!continueProcessing)
                {
                    return;
                }
            }).WithParsed((obj) => VerbRunner(obj, appSettings));

            return 1;
        }

        private static Type[] LoadVerbs()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        }

        private static void VerbRunner(object obj, IAppSettings appSettings)
        {
            ILogger appLogger = default;
            if (obj is CommonOptions commonopts)
            {
                var defaultLevel = (commonopts.Verbose) ? LogEventLevel.Verbose : LogEventLevel.Error;
                var logFilename = (!string.IsNullOrEmpty(commonopts.LogFileName)) ? commonopts.LogFileName : "poshlogs.txt";
                appLogger = Configuration.GetLogger(appSettings, logFilename, defaultLevel);
            }

            switch (obj)
            {
                case ConnectEPAOnlineOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case DisconnectEPAOnlineOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ConnectADALv1Options opts:
                    opts.RunGenerateAndReturnExitCode(appSettings, appLogger);
                    break;
                case ConnectADALv2Options opts:
                    opts.RunGenerateAndReturnExitCode(appSettings, appLogger);
                    break;
                case GetUnifiedGroupOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings, appLogger);
                    break;
                case GetUsersCommandOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings, appLogger);
                    break;
                case GetAnalyticO365GroupsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings, appLogger);
                    break;
                case ReportUsageAnalyticsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings, appLogger);
                    break;
                case SyncEPASCAListOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPAAppScheduleOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPAAppWebOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAQueryAppMonitorOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPASiteSharingSettingsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPASiteGroupAssociationOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPASetEveryoneGroupOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPASiteMetadataOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPAOD4BMonitorNotificationOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPAExternalRecertifyUsersOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPADocumentMetadataOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAEZFormsRecertificationNotifyOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case MoveEPAEZFormsRequestToFoldersOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case RedoEPAEZFormsTemporaryOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ResetEPAEZFormsApproversOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SendEPAEZFormsElevatedPrivilegeReminderOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SendEPAEZFormsNotificationsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SetEPAEZFormsAdminProcessOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SyncEPAEZFormsOfficeListingOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SetEPAO365GroupsNotifyOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case MakeEPASiteProvisionerOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SetEPASiteRequestTemplateOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAAnalyticO365SitesOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAAnalyticO365UserProfilesOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAAnalyticO365SiteListingOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPASiteRequestsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPAGroupRequestsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings, appLogger);
                    break;
                case GetEPASiteColumnOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case MoveEPAItemToFolderOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case MoveEPAFolderOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ExportEPATermSetOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SetEPARemoteMapperItemsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAAppTenantDetailsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAAppWebDetailsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAAuthenticationRealmOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPAQueryListApiOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case AddEPASiteCollectionAdminOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case GetEPASiteRequestPropertiesOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case ScanEPASiteMailboxesOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case RemoveEPAListItemsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SendEPAEmailOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SetEPAHelpDeskFeatureJsonOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SetEPAListBreakPermissionsOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
                case SetEPAListEnableFoldersOptions opts:
                    opts.RunGenerateAndReturnExitCode(appSettings);
                    break;
            }
        }
    }
}
