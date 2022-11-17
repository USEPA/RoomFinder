using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Apps;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("SetEPAListBreakPermissions", HelpText = "Process the list items and breaks permissions.  Supports should process, if WhatIf is used then no change is made")]
    public class SetEPAListBreakPermissionsOptions : CommonOptions
    {
        /// <summary>
        /// The site to search and query Term Sets
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// The name of the list/library
        /// </summary>
        [Option("library-name", Required = true, HelpText = "The name of the Library/List to break permissions")]
        public string LibraryName { get; set; }

        /// <summary>
        /// Contains the permissions to assert
        /// </summary>
        /// <remarks>
        /// 
        /// Example for Parameter
        /// $groups = @()
        /// $groups += @[pscustomobject]{ 
        ///     name = sharepointgroup-user, 
        ///     roletype = RoleType.Contributor, 
        ///     role = "Contribute w_o deleting"
        /// }
        /// $groups += @[pscustomobject]{ 
        ///     name = sharepointgroup-member, 
        ///     roletype = RoleType.Contributor
        /// }
        /// -SharePointGroups $groups
        /// </remarks>
        [Option("sharepointgroups", Required = true)]
        public List<GroupRoleDefinition> SharePointGroups { get; set; }

        /// <summary>
        /// Represents a custom roledefinition EX: "Contribute w_o deleting"
        /// </summary>
        [Option("custom-rolename", Required = true, HelpText = "A custom role definition to be bound to the rolebindings")]
        public string ContributeRoleWithoutDelete { get; set; }

        /// <summary>
        /// Should we run against all requests
        /// </summary>
        /// <remarks>Exists: All forms || Not Exists: Only in flight forms</remarks>
        [Option("only-completed", Required = false, HelpText = "Determines if the process should run against all forms")]
        public bool OnlyCompleted { get; set; }

        /// <summary>
        /// Determines the specific list item id to modify
        /// </summary>
        [Option("list-itemid", Required = false, HelpText = "Determines the specific list item id to modify")]
        public int? ListItemId { get; set; }

        /// <summary>
        /// Determines the specific list item id from which to advance
        /// </summary>
        [Option("filter-gt", Required = false, HelpText = "Determines the specific list items by date from which to advance")]
        public DateTime? GreaterThanDate { get; set; }

        /// <summary>
        /// OPTIONAL: If specified it will remove the existing rolebindings and apply the new RoleDefinition
        /// </summary>
        [Option("override", Required = false, HelpText = "OPTIONAL: If specified it will remove the existing rolebindings and apply the new RoleDefinition")]
        public bool Override { get; set; }
    }

    public static class SetEPAListBreakPermissionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this SetEPAListBreakPermissionsOptions opts, IAppSettings appSettings)
        {
            var cmd = new SetEPAListBreakPermissions(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Process the list items and breaks permissions.  Supports should process, if WhatIf is used then no change is made
    ///     Documentation: https://github.com/OfficeDev/PnP-Sites-Core/blob/master/Core/OfficeDevPnP.Core/AppModelExtensions/SecurityExtensions.cs
    /// </summary>
    /// <remarks>Query the list and apply specific permissions</remarks>
    public class SetEPAListBreakPermissions : BaseSpoCommand<SetEPAListBreakPermissionsOptions>
    {
        public SetEPAListBreakPermissions(SetEPAListBreakPermissionsOptions opts, IAppSettings settings)
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
            var camlFieldRefs = new string[] { "ID", "Status", "Author", "Title", "AssignedTo" };
            var camlViewClause = CAML.ViewFields(camlFieldRefs.ToList().Select(s => CAML.FieldRef(s)).ToArray());

            var removeExistingPermissionLevels = Opts.Override;

            // Load groups
            Opts.SharePointGroups.ForEach(group =>
                {
                    var groupId = ClientContext.Web.GetGroupID(group.name);
                    group.id = groupId;
                });


            //Load list
            var siteUrl = ClientContext.Url;
            var listInSite = ClientContext.Web.Lists.GetByTitle(Opts.LibraryName);
            ClientContext.Load(listInSite);
            ClientContext.ExecuteQueryRetry();


            var camlWhereClause = string.Empty;

            if (Opts.ListItemId.HasValue)
            {
                camlWhereClause = CAML.Where(CAML.Eq(CAML.FieldValue("ID", FieldType.Integer.ToString("f"), Opts.ListItemId.Value.ToString())));
            }
            else
            {
                var camlAndClause = string.Empty;
                if (Opts.OnlyCompleted)
                {
                    camlAndClause = CAML.Eq(CAML.FieldValue("Status", "Text", "Completed"));
                }
                else
                {
                    camlAndClause = CAML.Neq(CAML.FieldValue("Status", "Text", "Completed"));
                }

                if (Opts.GreaterThanDate.HasValue)
                {
                    var utcFormattedIsoDate = Opts.GreaterThanDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    camlAndClause = CAML.And(camlAndClause,
                        CAML.Geq(string.Format("{0}<Value Type='{1}' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{2}</Value>", CAML.FieldRef("Modified"), FieldType.DateTime.ToString("f"), utcFormattedIsoDate)));
                }

                camlWhereClause = CAML.Where(camlAndClause);
            }

            ListItemCollectionPosition ListItemCollectionPosition = null;
            var camlQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, camlWhereClause, "", camlViewClause, 100)
            };


            while (true)
            {
                camlQuery.ListItemCollectionPosition = ListItemCollectionPosition;
                var spListItems = listInSite.GetItems(camlQuery);
                ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                ClientContext.ExecuteQueryRetry();
                ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    try
                    {
                        LogVerbose("Querying request: {0}", requestItem.Id);
                        var requestStatus = requestItem.RetrieveListItemValue("Status");
                        var requestAssignedTo = requestItem.RetrieveListItemUserValue("AssignedTo");
                        var requestAuthor = requestItem.RetrieveListItemUserValue("Author");
                        var userLoginName = string.Format("{0}|{1}", ClaimIdentifier, requestAuthor.Email);

                        var itemDetails = listInSite.GetItemById(requestItem.Id);
                        ClientContext.Load(itemDetails, Inc => Inc.HasUniqueRoleAssignments, intc => intc.RoleAssignments.Include(intcr => intcr.PrincipalId));
                        ClientContext.ExecuteQueryRetry();


                        if (ShouldProcess(string.Format("request {0} is inheriting permission from the list.", requestItem.Id)))
                        {
                            if (!itemDetails.HasUniqueRoleAssignments)
                            {
                                itemDetails.BreakRoleInheritance(false, true);
                                ClientContext.ExecuteQueryRetry();

                                // grant the sharepoint groups access to the request
                                // these are the same set of permissions for each status check
                                Opts.SharePointGroups.ForEach(kvp =>
                                {
                                    if (string.IsNullOrEmpty(kvp.role))
                                    {
                                        LogVerbose("ITEM:{0} Adding {1} with permission {2}", requestItem.Id, kvp.name, kvp.roletype.ToString("f"));
                                        itemDetails.AddPermissionLevelToGroup(kvp.name, kvp.roletype, removeExistingPermissionLevels);
                                    }
                                    else
                                    {
                                        LogVerbose("ITEM:{0} Adding {1} with permission {2}", requestItem.Id, kvp.name, kvp.role);
                                        itemDetails.AddPermissionLevelToGroup(kvp.name, kvp.role, removeExistingPermissionLevels);
                                    }
                                });

                                // Assuming these are new requests Open/InProgress and permissions have not been broken
                                if (!requestStatus.Equals("COMPLETED", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    // grant the author access to the request [ensure they can modify it]
                                    LogVerbose("ITEM:{0} Granting {1} permission {2}", requestItem.Id, userLoginName, RoleType.Contributor.ToString("f"));
                                    itemDetails.AddPermissionLevelToUser(userLoginName, RoleType.Contributor, removeExistingPermissionLevels);
                                }
                            }

                            // assuming these are completed forms and we are removing the users contribute permissions
                            if (requestStatus.Equals("COMPLETED", StringComparison.CurrentCultureIgnoreCase))
                            {
                                // switch out roles to ensure the author can view but not edit their request
                                LogVerbose("ITEM:{0} Granting {1} permission {2}, removing existing roles", requestItem.Id, userLoginName, RoleType.Reader.ToString("f"));
                                itemDetails.AddPermissionLevelToUser(userLoginName, RoleType.Reader, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Break permissions failed MSG:{0}", ex.Message);
                    }
                }

                if (ListItemCollectionPosition == null)
                {
                    break;
                }
                else
                {
                    LogVerbose("Found additional rows, executing query with position {0}", ListItemCollectionPosition.PagingInfo);
                }
            }

            return 1;
        }
    }
}
