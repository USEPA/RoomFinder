using CommandLine;
using ConsoleTables;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.PipeBinds;
using EPA.SharePoint.SysConsole.Extensions;
using Microsoft.SharePoint.Client;
using Serilog;
using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("SetEPAListEnableFolders", HelpText = "The function cmdlet will enable folders for a library where it is not already enabled.")]
    public class SetEPAListEnableFoldersOptions : CommonOptions
    {
        /// <summary>
        /// The site
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// The list upon which temporary actions will take place
        /// </summary>
        [Option(Required = true, HelpText = "Library Name")]
        public string ListName { get; set; }
    }

    public static class SetEPAListEnableFoldersExtension
    {
        public static int RunGenerateAndReturnExitCode(this SetEPAListEnableFoldersOptions opts, IAppSettings appSettings)
        {
            var cmd = new SetEPAListEnableFolders(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will enable folders for a library where it is not already enabled
    /// </summary>
    public class SetEPAListEnableFolders : BaseSpoCommand<SetEPAListEnableFoldersOptions>
    {
        public SetEPAListEnableFolders(SetEPAListEnableFoldersOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Opts.SiteUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {

            var foundList = ClientContext.Web.GetList(Opts.ListName,
                arl => arl.Title,
                arl => arl.Id,
                arl => arl.ContentTypes,
                ol => ol.RootFolder,
                ol => ol.EnableVersioning,
                ol => ol.EnableFolderCreation,
                ol => ol.ContentTypesEnabled,
                ol => ol.ServerTemplateCanCreateFolders,
                ol => ol.BaseTemplate,
                ol => ol.TemplateFeatureId,
                ol => ol.Views.Include(olv => olv.Id, olv => olv.Title, olv => olv.Hidden, olv => olv.HtmlSchemaXml));
            ClientContext.ExecuteQueryRetry();

            if (foundList.ServerTemplateCanCreateFolders && !foundList.EnableFolderCreation)
            {
                foundList.EnableFolderCreation = true;
                foundList.Update();
                ClientContext.Load(foundList);
                ClientContext.ExecuteQueryRetry();
            }

            var objviews = new List<object>();
            foreach (var view in foundList.Views)
            {
                objviews.Add(new
                {
                    view.Title,
                    view.Hidden,
                    Html = view.HtmlSchemaXml,
                    view.Id
                });
                LogVerbose("View {0} hidden[{1}]", view.Title, view.Hidden);
            }

            ConsoleTable.From(objviews).Write();
            return 1;
        }
    }
}