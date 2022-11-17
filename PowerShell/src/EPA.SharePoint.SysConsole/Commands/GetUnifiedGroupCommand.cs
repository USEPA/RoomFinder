using CommandLine;
using ConsoleTables;
using EPA.Office365;
using EPA.Office365.Graph;
using EPA.Office365.oAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb(name: "getunifiedgroup", HelpText = "Retreive Office 365 Groups || Unified Groups")]
    public class GetUnifiedGroupOptions : CommonOptions
    {
        [Option("identity", Required = false, SetName = "userprincipal", HelpText = "The display name of the group.")]
        public string Identity { get; set; }

        [Option("groupId", Required = false, SetName = "guid", HelpText = "the guid.")]
        public Guid GroupId { get; set; }

        [Option("resource-uri", Required = false, HelpText = "The URI of the resource to query")]
        public string ResourceUri { get; set; }
    }

    public static class GetUnifiedGroupCommandExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetUnifiedGroupOptions opts, IAppSettings appSettings, Serilog.ILogger logger)
        {
            var cmd = new GetUnifiedGroupCommand(opts, appSettings, logger);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Query the tenant to discover all O365 Groups; 
    ///     if identity is not specified return all groups
    ///     otherwise return the group based on the Title or ID
    /// </summary>
    public class GetUnifiedGroupCommand : BaseAdalCommand<GetUnifiedGroupOptions>
    {
        public GetUnifiedGroupCommand(GetUnifiedGroupOptions opts, IAppSettings settings, Serilog.ILogger traceLogger)
            : base(opts, settings, traceLogger)
        {
        }

        public override void OnBeginInit()
        {
            var useInteractiveLogin = false;
            var scopes = new string[] { Settings.Graph.DefaultScope };

            Settings.AzureAd.ClientId = Settings?.SpoAddInMakeEPASite.ClientId;
            Settings.AzureAd.ClientSecret = Settings?.SpoAddInMakeEPASite.ClientSecret;
            Settings.AzureAd.PostLogoutRedirectURI = ConstantsAuthentication.GraphResourceId;
            Settings.AzureAd.MSALScopes = scopes.ToArray();

            if (string.IsNullOrEmpty(Settings.AzureAd.ClientSecret) || string.IsNullOrEmpty(Settings.AzureAd.ClientId))
            {
                throw new Exception($"You must set credentials for spoAddInMakeEPASite");
            }

            TokenCache = new AzureADv2TokenCache(Settings, TraceLogger, useInteractiveLogin);
        }


        public override int OnRun()
        {
            var utility = new GraphUtility(TokenCache, TraceLogger);
            var client = utility.CreateGraphClient(retryCount: 10);

            var groups = Task.Run(async () =>
            {
                List<UnifiedGroupEntity> results = null;

                try
                {
                    // We have to retrieve a specific group
                    if (Opts.GroupId != null && Opts.GroupId != Guid.Empty)
                    {
                        var group = await utility.GetUnifiedGroup(Opts.GroupId.ToString(), includeSite: true);
                        if (string.IsNullOrEmpty(group?.Id))
                        {
                            TraceLogger.Warning($"Failed to get group {Opts.GroupId}");
                        }
                        else
                        {
                            results.Add(group);
                        }
                    }
                    else if (!string.IsNullOrEmpty(Opts.Identity))
                    {
                        results.AddRange(await utility.ListUnifiedGroups(displayName: Opts.Identity));
                    }
                    else
                    {
                        // Retrieve all the groups
                        results.AddRange(await utility.ListUnifiedGroups());
                    }

                }
                catch (Microsoft.Graph.ServiceException gex)
                {
                    TraceLogger.Error($"GraphEx {gex} =>InvalidOperation");
                }

                return results;
            }).GetAwaiter().GetResult();

            ConsoleTable.From(groups).Write();
            return 1;
        }
    }
}