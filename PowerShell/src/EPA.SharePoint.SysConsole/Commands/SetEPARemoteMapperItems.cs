using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Apps;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using OfficeDevPnP.Core.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("setEPARemoteMapperItems", HelpText = "The function cmdlet will serialize the mappings and push them to sharepoint.")]
    public class SetEPARemoteMapperItemsOptions : CommonOptions
    {
        /// <summary>
        /// The site to search and query Term Sets
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// Represents the directory path for any JSON files for serialization
        /// </summary>
        [Option("mapping-file", Required = true)]
        public string MappingJsonFile { get; set; }
    }

    public static class SetEPARemoteMapperItemsOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this SetEPARemoteMapperItemsOptions opts, IAppSettings appSettings)
        {
            var cmd = new SetEPARemoteMapperItems(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will serialize the mappings and push them to sharepoint
    /// </summary>
    public class SetEPARemoteMapperItems : BaseSpoCommand<SetEPARemoteMapperItemsOptions>
    {
        public SetEPARemoteMapperItems(SetEPARemoteMapperItemsOptions opts, IAppSettings settings)
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

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();

            if (!System.IO.File.Exists(Opts.MappingJsonFile))
            {
                throw new System.IO.FileNotFoundException("Failed to find file on disk", (new System.IO.FileInfo(Opts.MappingJsonFile)).Name);
            }
        }

        public override int OnRun()
        {
            //Move away from method configuration into a JSON file
            var siteComponents = JsonConvert.DeserializeObject<List<MappingModel>>(System.IO.File.ReadAllText(Opts.MappingJsonFile));

            var orgListIds = new Dictionary<string, int>();
            var orgListLookup = this.ClientContext.Web.GetListByTitle(Remotemapper_Organization.ListName);
            foreach (var orgs in siteComponents.Select(s => s.Name))
            {
                ListItem lookupItem = null;
                var orgTitle = orgs.Trim();
                ListItemCollectionPosition listItemCollectionPosition = null;
                var camlQuery = new CamlQuery();
                camlQuery.ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll,
                    CAML.Where(CAML.Eq(CAML.FieldValue(Remotemapper_Organization.Field_Title, FieldType.Text.ToString("f"), orgTitle))),
                    string.Empty,
                    string.Empty,
                    1);
                camlQuery.ListItemCollectionPosition = listItemCollectionPosition;
                var lookupItems = orgListLookup.GetItems(camlQuery);
                this.ClientContext.Load(lookupItems);
                this.ClientContext.ExecuteQueryRetry();

                var itemExists = false;
                if (lookupItems.Any())
                {
                    lookupItem = lookupItems.FirstOrDefault();
                    var title = lookupItem.RetrieveListItemValue(Remotemapper_Organization.Field_Title);
                    LogWarning("Found Org:{0}", title);
                    itemExists = true;
                }

                if (!itemExists)
                {
                    var newItemInfo = new ListItemCreationInformation();
                    lookupItem = orgListLookup.AddItem(newItemInfo);
                    lookupItem[Remotemapper_Organization.Field_Title] = orgTitle;
                    lookupItem.Update();
                    this.ClientContext.ExecuteQueryRetry();
                }

                orgListIds.Add(orgTitle, lookupItem.Id);
            }


            var networkListLookup = this.ClientContext.Web.GetListByTitle(Remotemapper_Networks.ListName);
            var field = networkListLookup.Fields.GetByTitle(Remotemapper_Networks.Field_SubOrganizationChoice);
            this.ClientContext.Load(field);
            this.ClientContext.ExecuteQueryRetry();

            var fieldStatus = this.ClientContext.CastTo<FieldChoice>(field);
            var values = fieldStatus.Choices.ToList();

            var colmodified = false;
            var suborgs = siteComponents.SelectMany(s => s.SubGroups.Select(n => n.Name.Trim())).Distinct();
            foreach (var suborg in suborgs)
            {
                if (!values.Any(fs => fs == suborg))
                {
                    colmodified = true;
                    values.Add(suborg);
                }
            }

            if (colmodified)
            {
                fieldStatus.Choices = values.ToArray();
                fieldStatus.Update();
                this.ClientContext.ExecuteQueryRetry();
            }

            foreach (var org in siteComponents)
            {
                var orgTitle = org.Name.Trim();
                foreach (var suborg in org.SubGroups)
                {
                    var subOrgTitle = suborg.Name.Trim();
                    foreach (var network in suborg.Networks)
                    {
                        LogVerbose("Org:{0} SubOrg:{1} Drive:{2}", orgTitle, subOrgTitle, network.Drive);

                        ListItemCollectionPosition listItemCollectionPosition = null;
                        var camlQuery = new CamlQuery
                        {
                            ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll,
                            CAML.Where(
                                CAML.And(
                                    CAML.And(
                                        CAML.Eq(CAML.FieldValue(Remotemapper_Networks.Field_OrganizationLookup, FieldType.Lookup.ToString("f"), orgTitle)),
                                        CAML.Eq(CAML.FieldValue(Remotemapper_Networks.Field_SubOrganizationChoice, FieldType.Text.ToString("f"), subOrgTitle))
                                        ),
                                    CAML.Eq(CAML.FieldValue(Remotemapper_Networks.Field_Title, FieldType.Text.ToString("f"), network.UNC)))),
                            string.Empty,
                            string.Empty,
                            1),
                            ListItemCollectionPosition = listItemCollectionPosition
                        };
                        var lookupItems = networkListLookup.GetItems(camlQuery);
                        this.ClientContext.Load(lookupItems);
                        this.ClientContext.ExecuteQueryRetry();
                        if (!lookupItems.Any())
                        {
                            LogWarning("Not Found list item for {0}", network.UNC);

                            var networkItem = new ListItemCreationInformation();
                            var newNetworkItem = networkListLookup.AddItem(networkItem);

                            newNetworkItem[Remotemapper_Networks.Field_Title] = network.UNC;
                            newNetworkItem[Remotemapper_Networks.Field_GroupName] = network.Group;
                            newNetworkItem[Remotemapper_Networks.Field_AutoSelectBool] = network.IsChecked;
                            newNetworkItem[Remotemapper_Networks.Field_DriveLetter] = network.Drive;
                            newNetworkItem[Remotemapper_Networks.Field_Description] = network.Description;
                            newNetworkItem[Remotemapper_Networks.Field_OrganizationLookup] = new FieldLookupValue() { LookupId = orgListIds.FirstOrDefault(f => f.Key == orgTitle).Value };
                            newNetworkItem[Remotemapper_Networks.Field_SubOrganizationChoice] = subOrgTitle;
                            newNetworkItem.Update();
                            this.ClientContext.ExecuteQueryRetry();
                        }
                    }
                }
            }
            return 1;
        }
    }
}
