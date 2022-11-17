using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.PipeBinds;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("RemoveEPAListItems", HelpText = "The function cmdlet will query a large or small list using the caml override.")]
    public class RemoveEPAListItemsOptions : CommonOptions
    {
        /// <summary>
        /// The site
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// OPTIONAL The list upon which temporary actions will take place
        /// </summary>
        [Option("list-name", Required = true, HelpText = "Library Name")]
        public string ListName { get; set; }

        /// <summary>
        /// OPTIONAL: A CAML filter to override list item queries
        /// </summary>
        [Option("override-caml", Required = false)]
        public string CamlOverride { get; set; }
    }

    public static class RemoveEPAListItemsExtension
    {
        public static int RunGenerateAndReturnExitCode(this RemoveEPAListItemsOptions opts, IAppSettings appSettings)
        {
            var cmd = new RemoveEPAListItems(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will query a large or small list using the caml override.  
    /// It will then delete the list items
    /// </summary>
    public class RemoveEPAListItems : BaseSpoCommand<RemoveEPAListItemsOptions>
    {
        public RemoveEPAListItems(RemoveEPAListItemsOptions opts, IAppSettings settings)
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

            var camlFieldRefs = new string[] {
                ConstantsFields.Field_ID,
                ConstantsFields.Field_Created,
                ConstantsFields.Field_Modified
            };
            var camlViewClause = CAML.ViewFields(camlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());


            var listInSite = ClientContext.Web.GetList(Opts.ListName, spinc => spinc.Title, inc => inc.ItemCount, inn => inn.RootFolder, inn => inn.RootFolder.ServerRelativeUrl, inn => inn.ParentWeb.ServerRelativeUrl);
            var itemIds = new List<int>();

            var startItemId = 0;
            var lastItemId = listInSite.QueryLastItemId();

            // CAML query array
            var camlQueries = listInSite.SafeCamlClauseFromThreshold(2000, Opts.CamlOverride, startItemId, lastItemId);
            camlQueries.ForEach(camlClause =>
            {

                itemIds = new List<int>(); // Reset collection
                var camlWhereClause = camlClause;
                if (!string.IsNullOrEmpty(camlClause))
                {
                    camlWhereClause = CAML.Where(camlClause);
                }

                LogWarning("CAML query {0}", camlWhereClause);

                var camlQuery = new CamlQuery()
                {
                    ViewXml = CAML.ViewQuery(ViewScope.DefaultValue, camlWhereClause, "", camlViewClause, 100),
                    ListItemCollectionPosition = null
                };

                do
                {
                    LogVerbose("Executing query with position {0}", camlQuery?.ListItemCollectionPosition?.PagingInfo ?? "initial");
                    var spListItems = listInSite.GetItems(camlQuery);
                    ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                    ClientContext.ExecuteQueryRetry();
                    camlQuery.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                    foreach (var requestItem in spListItems)
                    {
                        itemIds.Add(requestItem.Id);
                    }
                }
                while (camlQuery.ListItemCollectionPosition != null);


                if (ShouldProcess(string.Format("Will delete {0} items.", itemIds.Count())))
                {
                    var defaultParse = 100;
                    var deleteIdx = 0; var totalIdx = itemIds.Count;
                    foreach (var itemId in itemIds)
                    {
                        deleteIdx++; totalIdx--;
                        var itemToDelete = listInSite.GetItemById(itemId);
                        listInSite.Context.Load(itemToDelete);
                        itemToDelete.DeleteObject();

                        if (deleteIdx >= defaultParse || totalIdx <= 0)
                        {
                            LogVerbose("Deleting items {0} from list {1}", deleteIdx, listInSite.Title);
                            listInSite.Context.ExecuteQueryRetry();
                            deleteIdx = 0;
                        }
                    }
                }
            });

            return 1;
        }

    }
}