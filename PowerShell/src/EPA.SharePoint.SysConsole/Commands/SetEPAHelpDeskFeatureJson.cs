using CommandLine;
using EPA.Office365.Extensions;
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
    [Verb("SetEPAHelpDeskFeatureJson", HelpText = "The function query the specific list and update list items based on criteria.")]
    public class SetEPAHelpDeskFeatureJsonOptions : CommonOptions
    {
        /// <summary>
        /// The site
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// Includes the destination directory for the files
        /// </summary>
        [Option("log-directory", Required = true, HelpText = "Includes the destination directory for the files.")]
        public string LogDirectory { get; set; }

        /// <summary>
        /// Represents the directory path for any JSON files for serialization
        /// </summary>
        [Option("view-fields", Required = false)]
        public IEnumerable<object> ViewFields { get; set; }
    }

    public static class SetEPAHelpDeskFeatureJsonExtension
    {
        public static int RunGenerateAndReturnExitCode(this SetEPAHelpDeskFeatureJsonOptions opts, IAppSettings appSettings)
        {
            var cmd = new SetEPAHelpDeskFeatureJson(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Query the specific list and update list items based on criteria.
    /// </summary>
    public class SetEPAHelpDeskFeatureJson : BaseSpoCommand<SetEPAHelpDeskFeatureJsonOptions>
    {
        public SetEPAHelpDeskFeatureJson(SetEPAHelpDeskFeatureJsonOptions opts, IAppSettings settings)
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

            if (!System.IO.Directory.Exists(Opts.LogDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"{Opts.LogDirectory} could not be found.");
            }
        }

        public override int OnRun()
        {

            var mockData = new MockModel();
            mockData.features.Add(new FeatureSetModel()
            {
                dataTypes = "Available",
                dataTypeIcon = "launched",
                dataTypeBanner = "Launched",
                dataTypeCountId = "launchedCount",
                dataCount = 0,
                filteredCount = 0,
                dataDescription = "Fully released updates that are now generally available for EPA users, developers, administrators",
                dataItems = "launchedItems"
            });
            mockData.features.Add(new FeatureSetModel()
            {
                dataTypes = "Previously released",
                dataTypeIcon = null,
                dataTypeBanner = "Previouslyreleased",
                dataTypeCountId = "prevReleasedCount",
                dataCount = 0,
                filteredCount = 0,
                dataDescription = "Generally available updates for all for EPA users, developers, administrators",
                dataItems = "prevReleasesItems"
            });
            mockData.features.Add(new FeatureSetModel()
            {
                dataTypes = "Coming Soon",
                dataTypeIcon = "rolling-out",
                dataTypeBanner = "Rollingout",
                dataTypeCountId = "rollingOutCount",
                dataCount = 0,
                filteredCount = 0,
                dataDescription = "Updates that are beginning to roll-out and are not yet available",
                dataItems = "rollingOutItems"
            });
            mockData.features.Add(new FeatureSetModel()
            {
                dataTypes = "Removed",
                dataTypeIcon = "cancelled",
                dataTypeBanner = "Cancelled",
                dataTypeCountId = "cancelledCount",
                dataCount = 0,
                filteredCount = 0,
                dataDescription = "Features that are disabled by Governance | EPA or no longer relevant to Office 365",
                dataItems = "cancelledItems"
            });
            mockData.features.Add(new FeatureSetModel()
            {
                dataTypes = "Unknown",
                dataTypeIcon = "in-dev",
                dataTypeBanner = "Indevelopment",
                dataTypeCountId = "inDevCount",
                dataCount = 0,
                filteredCount = 0,
                dataDescription = "Features that are missing metadata in this list and need to be updated.",
                dataItems = "inDevItems"
            });


            mockData.filters.Add(new FilterModel("Audience", "audience"));
            mockData.filters.Add(new FilterModel("Feature Scope", "featurescope"));
            mockData.filters.Add(new FilterModel("Feature Type", "featuretype"));

            var CollectionOfFeatures = new List<FeatureSetModel>();

            var fields = new string[]
            {
                "Comments", //Multiline
                "BriefSummary", //Multiline
                "Documentation", // Lookup
                "Audience", // Choice - Multi
                "Feature_x0020_Scope", // Choice - Multi
                "Feature_x0020_Type", // Choice - Single
                "FeatureCategory", // Choice
                "Governance", // Choice - Single
                "Activation_x0020_Date", // Date
                "GovernanceRisk", // MultiLine
                "GovernanceSuggestion", // DropDown
                "Link_x0020_To_x0020_Training", // Hyperlink
                "More_x0020_Information", // Multiline
                "Priority", // Choice - Drop
                "Risk_x0020_to_x0020_EPA_x0020_En", // Choice - Drop
                "Title",
                "Training_x0020_Available_x003f_", // Yes/No
                "Training_x0020_Required", // Yes/No
                "Turned_x0020_on_x0020_at_x0020_E", // Choice
                "TurnedOnDate", // Date
                "URL", // Hyperlink,
                "ActionCategory", // Choice
                "ActionRequired", // Choice
                "HowDoesThisAffectMe", // Multiline
                "HowDoIPrepare", // Multiline
                "LastUpdated", // DateTime
                "MSID", // Text
            };

            var listInSite = this.ClientContext.Web.Lists.GetByTitle("Features");
            this.ClientContext.Load(listInSite, liss => liss.Id, liss => liss.Title, liss => liss.Fields);
            this.ClientContext.ExecuteQuery();

            listInSite.Fields.ToList().ForEach(lField =>
            {
                LogVerbose("Field ID:{0} Name:{1} Internal:{2} Hidden:{3}", lField.Id, lField.Title, lField.InternalName, lField.Hidden);
            });

            var viewXml = CAML.ViewFields(fields.Select(s => CAML.FieldRef(s)).ToArray());
            var orderXml = CAML.OrderBy(new OrderByField("Turned_x0020_on_x0020_at_x0020_E"), new OrderByField("TurnedOnDate", false));


            ListItemCollectionPosition ListItemCollectionPosition = null;
            var camlXml = CAML.ViewQuery(ViewScope.RecursiveAll, string.Empty, orderXml, viewXml, 50);
            var camlQuery = new CamlQuery
            {
                ViewXml = camlXml
            };


            while (true)
            {
                camlQuery.ListItemCollectionPosition = ListItemCollectionPosition;
                var spListItems = listInSite.GetItems(camlQuery);
                this.ClientContext.Load(spListItems);
                this.ClientContext.ExecuteQuery();
                ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                #region Enumerate Feature List
                foreach (var spListItem in spListItems)
                {
                    LogVerbose("ListItem [{0}] will be updated.", spListItem.Id);

                    try
                    {
                        var Comments = spListItem.RetrieveListItemValue("Comments");
                        var BriefSummary = spListItem.RetrieveListItemValue("BriefSummary");
                        var Documentation = spListItem.RetrieveListItemValueAsLookups("Documentation");
                        var Audience = spListItem.RetrieveListItemChoiceValues("Audience");
                        var Feature_x0020_Scope = spListItem.RetrieveListItemChoiceValues("Feature_x0020_Scope");
                        var FeatureCategory = spListItem.RetrieveListItemValue("FeatureCategory");
                        var Feature_x0020_Type = spListItem.RetrieveListItemValue("Feature_x0020_Type");
                        var Governance = spListItem.RetrieveListItemValue("Governance");
                        var Activation_x0020_Date = spListItem.RetrieveListItemValue("Activation_x0020_Date").ToDateTime();
                        var GovernanceRisk = spListItem.RetrieveListItemValue("GovernanceRisk");
                        var GovernanceSuggestion = spListItem.RetrieveListItemValue("GovernanceSuggestion");
                        var Link_x0020_To_x0020_Training = spListItem.RetrieveListItemValueAsHyperlink("Link_x0020_To_x0020_Training");
                        var More_x0020_Information = spListItem.RetrieveListItemValue("More_x0020_Information");
                        var Priority = spListItem.RetrieveListItemValue("Priority");
                        var Risk_x0020_to_x0020_EPA_x0020_En = spListItem.RetrieveListItemValue("Risk_x0020_to_x0020_EPA_x0020_En");
                        var Title = spListItem.RetrieveListItemValue("Title");
                        var Training_x0020_Available_x003f_ = spListItem.RetrieveListItemValue("Training_x0020_Available_x003f_").ToBoolean();
                        var Training_x0020_Required = spListItem.RetrieveListItemValue("Training_x0020_Required").ToBoolean();
                        var Turned_x0020_on_x0020_at_x0020_E = spListItem.RetrieveListItemValue("Turned_x0020_on_x0020_at_x0020_E");
                        var HowDoesThisAffectMe = spListItem.RetrieveListItemValue("HowDoesThisAffectMe");
                        var HowDoIPrepare = spListItem.RetrieveListItemValue("HowDoIPrepare");
                        var TurnedOnDate = spListItem.RetrieveListItemValue("TurnedOnDate").ToNullableDatetime();
                        var LastUpdated = spListItem.RetrieveListItemValue("LastUpdated").ToNullableDatetime();
                        var createdDate = spListItem.RetrieveListItemValue("Created").ToDateTime();
                        var modifieddDate = spListItem.RetrieveListItemValue("Modified").ToDateTime();
                        var URL = spListItem.RetrieveListItemValueAsHyperlink("URL");

                        var recentlyUpdated = (LastUpdated.HasValue && DateTime.Now.Subtract(LastUpdated.Value).TotalDays < 30);
                        var recentlyAdded = (DateTime.Now.Subtract(createdDate).TotalDays < 30);
                        var recentToEnvironment = (TurnedOnDate.HasValue && DateTime.Now.Subtract(TurnedOnDate.Value).TotalDays < 30);

                        var modelFeature = new FeatureItemModel()
                        {
                            createdDate = createdDate,
                            title = Title,
                            description = Comments,
                            assistance = BriefSummary,
                            moreinfo = More_x0020_Information,
                            howItAffectsMe = HowDoesThisAffectMe,
                            howDoIPrepare = HowDoIPrepare,
                            displayFeature = true,
                            id = spListItem.Id,
                            url = (URL != null) ? URL.Url : string.Empty,
                            recentlyUpdated = recentlyUpdated,
                            recentlyAdded = recentlyAdded
                        };


                        var previous = mockData.features.FirstOrDefault(f => f.dataTypes == "Previously released");
                        var available = mockData.features.FirstOrDefault(f => f.dataTypes == "Available");
                        var roadmap = mockData.features.FirstOrDefault(f => f.dataTypes == "Coming Soon");
                        var removedOrCancelled = mockData.features.FirstOrDefault(f => f.dataTypes == "Removed");
                        var unknownOrUntagged = mockData.features.FirstOrDefault(f => f.dataTypes == "Unknown");

                        if (FeatureCategory == "Available"
                            || FeatureCategory == "Previously released")
                        {

                            if ((!TurnedOnDate.HasValue)
                                || (TurnedOnDate.HasValue && DateTime.Now.Subtract(TurnedOnDate.Value).TotalDays < 30))
                            {
                                modelFeature.idlbl = string.Format("L-{0}", modelFeature.id);
                                available.items.Add(modelFeature);
                                available.dataCount += 1;
                                available.filteredCount += 1;
                            }
                            else if ((TurnedOnDate.HasValue && DateTime.Now.Subtract(TurnedOnDate.Value).TotalDays > 30))
                            {
                                modelFeature.idlbl = string.Format("P-{0}", modelFeature.id);
                                previous.items.Add(modelFeature);
                                previous.dataCount += 1;
                                previous.filteredCount += 1;
                            }
                        }
                        else if (FeatureCategory == "Roadmap")
                        {
                            modelFeature.idlbl = string.Format("R-{0}", modelFeature.id);
                            roadmap.items.Add(modelFeature);
                            roadmap.dataCount += 1;
                            roadmap.filteredCount += 1;
                        }
                        else if (FeatureCategory == "Removed")
                        {
                            modelFeature.idlbl = string.Format("C-{0}", modelFeature.id);
                            removedOrCancelled.items.Add(modelFeature);
                            removedOrCancelled.dataCount += 1;
                            removedOrCancelled.filteredCount += 1;
                        }
                        else
                        {
                            modelFeature.idlbl = string.Format("U-{0}", modelFeature.id);
                            unknownOrUntagged.items.Add(modelFeature);
                            unknownOrUntagged.dataCount += 1;
                            unknownOrUntagged.filteredCount += 1;
                        }

                        var audienceFilters = mockData.filters.FirstOrDefault(mf => mf.classid.Equals("audience"));
                        var scopeFilters = mockData.filters.FirstOrDefault(mf => mf.classid.Equals("featurescope"));
                        var typeFilters = mockData.filters.FirstOrDefault(mf => mf.classid.Equals("featuretype"));


                        foreach (var tag in Audience)
                        {
                            if (!audienceFilters.tags.Any(ft => ft.display.Equals(tag, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                var tagModel = new FilterTagModel();
                                tagModel.display = tag;
                                audienceFilters.tags.Add(tagModel);
                            }

                            if (!modelFeature.tags.Any(ft => ft.Equals(tag, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                modelFeature.tags.Add(tag);
                            }
                        }

                        foreach (var tag in Feature_x0020_Scope)
                        {
                            if (!scopeFilters.tags.Any(ft => ft.display.Equals(tag, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                var tagModel = new FilterTagModel();
                                tagModel.display = tag;
                                scopeFilters.tags.Add(tagModel);
                            }

                            if (!modelFeature.tags.Any(ft => ft.Equals(tag, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                modelFeature.tags.Add(tag);
                            }
                        }

                        foreach (var tag in Feature_x0020_Type.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!typeFilters.tags.Any(ft => ft.display.Equals(tag, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                var tagModel = new FilterTagModel();
                                tagModel.display = tag;
                                typeFilters.tags.Add(tagModel);
                            }

                            if (!modelFeature.tags.Any(ft => ft.Equals(tag, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                modelFeature.tags.Add(tag);
                            }
                        }
                    }
                    catch (Exception spex)
                    {
                        LogWarning("Failed to update list item with message {0}", spex.Message);
                    }
                }
                #endregion

                if (ListItemCollectionPosition == null)
                {
                    break;
                }
                else
                {
                    LogVerbose("Found additional rows, executing query with position {0}", ListItemCollectionPosition.PagingInfo);
                }
            }

            LogVerbose("Writing object to memory stream.");
            if (ShouldProcess("Writing file to disc"))
            {
                var jsonPath = $"{Opts.LogDirectory}\\helpdesk-{DateTime.UtcNow:yyyy-MM-dd_HH_mm_ss}.json";
                var modelJson = JsonConvert.SerializeObject(mockData, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 5,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                System.IO.File.WriteAllText(jsonPath, modelJson);
            }

            return 1;
        }
    }
}
