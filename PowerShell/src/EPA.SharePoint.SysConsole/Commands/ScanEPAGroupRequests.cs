using CommandLine;
using EPA.Office365;
using EPA.Office365.Extensions;
using EPA.Office365.Graph;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Apps;
using EPA.SharePoint.SysConsole.Models.Governance;
using EPA.SharePoint.SysConsole.Models.Groups;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("ScanEPAGroupRequests", HelpText = "scan site requests and move to archive folder.")]
    public class ScanEPAGroupRequestsOptions : TenantCommandOptions
    {
        [Option("beta-endpoint", Required = false, HelpText = "Should the process use the Graph Beta endpoint for processing.")]
        public bool BetaEndPoint { get; set; }

        [Option('o', "script-option", Required = true)]
        public GroupRequestOption ScriptOption { get; set; }

        [Option('d', "log-directory", Required = true)]
        public string LogDirectory { get; set; }
    }

    public enum GroupRequestOption
    {
        CreateGroups,
        UpdateGroups,
        CleanupGroups
    }

    public static class ScanEPAGroupRequestsExtension
    {
        public static int RunGenerateAndReturnExitCode(this ScanEPAGroupRequestsOptions opts, IAppSettings appSettings, Serilog.ILogger logger)
        {
            var cmd = new ScanEPAGroupRequests(opts, appSettings, logger);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will scan group requests, create/update o365 groups, update sharepoint
    /// </summary>

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handling rare exceptions.")]
    public class ScanEPAGroupRequests : BaseSpoTenantCommand<ScanEPAGroupRequestsOptions>
    {
        public ScanEPAGroupRequests(ScanEPAGroupRequestsOptions opts, IAppSettings settings, Serilog.ILogger logger)
            : base(opts, settings)
        {
            RunningUserAgent = $"{CoreResources.USEPA_UserAgent}/1.0.2";
            TraceLogger = logger;
        }

        #region Private variables

        internal Serilog.ILogger TraceLogger { get; }
        internal string TenantAdminUrl { get; set; }
        internal string GroupListName { get; set; }
        internal string GraphEndpoint { get; set; }
        internal GraphServiceClient GraphClient { get; set; }
        internal AzureADv2TokenCache TokenCache { get; set; }
        internal string RunningUserAgent { get; }
        internal string EmailDomain { get; set; }
        internal string CloudDomain { get; set; }

        #endregion

        public override void OnInit()
        {
            var useInteractiveLogin = false;
            var scopes = new string[] { Settings.Graph.DefaultScope };
            Settings.AzureAd.ClientId = Settings.SpoEPAAdalGovernance.ClientId;
            Settings.AzureAd.ClientSecret = Settings.SpoEPAAdalGovernance.ClientSecret;
            Settings.AzureAd.PostLogoutRedirectURI = ConstantsAuthentication.GraphResourceId;
            Settings.AzureAd.MSALScopes = scopes.ToArray();
            EmailDomain = Settings.Commands.Domain;
            CloudDomain = Settings.Commands.CloudDomain;

            GraphEndpoint = Settings.Graph.DefaultEndpoint;
            TokenCache = new AzureADv2TokenCache(Settings, TraceLogger, useInteractiveLogin);
            GraphClient = new GraphServiceClient(baseUrl: GraphEndpoint, authenticationProvider: TokenCache.AuthenticationProvider);

            TenantAdminUrl = Settings.Commands.TenantAdminUrl;
            var userName = Settings.SpoEpaCredentials.Username;
            var userSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"spoepaonline credentials");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantAdminUrl), userName, userSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();
            if (!System.IO.Directory.Exists(Opts.LogDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"Directory {Opts.LogDirectory} not found.");
            }
        }

        public override int OnRun()
        {
            var siteRequestUrl = Settings.Commands.SPOSiteRequestUrl;
            GroupListName = Settings.Commands.GroupRequestsListName;

            using var siteCtx = ClientContext.Clone(siteRequestUrl);

            var currentUser = CurrentUserName;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using HttpClient client = GraphClientFactory.Create(TokenCache.AuthenticationProvider);

            var utility = new GraphUtility(TokenCache, TraceLogger);
            if (Opts.ScriptOption == GroupRequestOption.CreateGroups)
            {
                utility.CreateGraphClient(10, delay: 1000);
                var approvedGroups = SpoGetGroupRequests(siteCtx);
                approvedGroups.ToList().ForEach((groupRequest) =>
                {
                    groupRequest = ProcessO365Group(utility, client, groupRequest, currentUser).ConfigureAwait(false).GetAwaiter().GetResult();
                    
                    SpoCompleteGroupRequest(siteCtx, groupRequest);
                });
            }
            else if (Opts.ScriptOption == GroupRequestOption.UpdateGroups)
            {
                utility.CreateGraphClient(retryCount: 15);
                UpdateGroupsIntoSharepoint(siteCtx, utility).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else if (Opts.ScriptOption == GroupRequestOption.CleanupGroups)
            {
                utility.CreateGraphClient(retryCount: 15);
                CleanupGroupsIntoSharepoint(siteCtx, utility).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            return 1;
        }

        private void SpoCompleteGroupRequest(ClientContext siteContext, O365GroupRequestModel groupRequest)
        {
            var _listSites = siteContext.Web.GetList(GroupListName, lctx => lctx.Title, lctx => lctx.Id);
            var spListItems = _listSites.GetItemById(groupRequest.ListItemId);
            _listSites.Context.Load(spListItems);

            LogVerbose($"ProcessGroupRequests... List => {_listSites.Title} ID => {groupRequest.ListItemId}");
            spListItems[Constants_GroupRequest.GroupRequestFields.FieldText_RequestCompletedFlag] = groupRequest.RequestCompletedFlag;
            spListItems[Constants_GroupRequest.GroupRequestFields.FieldText_EmailAddress] = groupRequest.EmailAddress;
            spListItems[Constants_GroupRequest.GroupRequestFields.FieldText_SharePointDocumentsUrl] = groupRequest.SharePointDocumentsUrl;
            spListItems[Constants_GroupRequest.GroupRequestFields.FieldText_SharePointSiteUrl] = groupRequest.SharePointSiteUrl;
            spListItems[Constants_GroupRequest.GroupRequestFields.FieldDate_GroupCreatedDate] = groupRequest.WhenCreated;
            spListItems.Update(); // update the record

            _listSites.Context.ExecuteQueryRetry();
        }

        public Collection<O365GroupRequestModel> SpoGetGroupRequests(ClientContext siteContext)
        {
            var results = new Collection<O365GroupRequestModel>();
            var illegalCharacters = CleanIllegalCharacters();

            var _listSites = siteContext.Web.GetList(GroupListName, lctx => lctx.Title, lctx => lctx.Id);


            var viewFields = new string[] {
                ConstantsFields.Field_ID,
                ConstantsFields.Field_Title,
                Constants_GroupRequest.GroupRequestFields.FieldChoice_PublicFlag,
                Constants_GroupRequest.GroupRequestFields.FieldText_GroupName,
                Constants_GroupRequest.GroupRequestFields.FieldText_GroupOwner,
                Constants_GroupRequest.GroupRequestFields.FieldMultiText_Description
            };
            var camlViewClause = CAML.ViewFields(viewFields.Select(s => CAML.FieldRef(s)).ToArray());
            var camlWhereClause = CAML.Where(CAML.And(
                    CAML.Eq(CAML.FieldValue(Constants_GroupRequest.GroupRequestFields.FieldText_RequestApprovedFlag, FieldType.Text.ToString("f"), "Yes")),
                    CAML.Neq(CAML.FieldValue(Constants_GroupRequest.GroupRequestFields.FieldText_RequestCompletedFlag, FieldType.Text.ToString("f"), "Yes"))
                ));

            LogVerbose($"ProcessGroupRequests... List => {_listSites.Title}");

            var camlQuery = new CamlQuery()
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, camlWhereClause, string.Empty, camlViewClause, 100)
            };

            ListItemCollectionPosition itemPosition = null;
            while (true)
            {
                camlQuery.ListItemCollectionPosition = itemPosition;
                var spListItems = _listSites.GetItems(camlQuery);
                _listSites.Context.Load(spListItems, lictx => lictx.ListItemCollectionPosition);
                _listSites.Context.ExecuteQueryRetry();
                itemPosition = spListItems.ListItemCollectionPosition;

                foreach (var spItem in spListItems)
                {
                    var groupName = spItem.RetrieveListItemValue(Constants_GroupRequest.GroupRequestFields.FieldText_GroupName);
                    var alias = GetCleanEmailAlias(illegalCharacters, groupName);

                    results.Add(new O365GroupRequestModel()
                    {
                        ListItemId = spItem.Id,
                        Title = spItem.RetrieveListItemValue(ConstantsFields.Field_Title),
                        Description = spItem.RetrieveListItemValue(Constants_GroupRequest.GroupRequestFields.FieldMultiText_Description),
                        PublicFlag = spItem.RetrieveListItemValue(Constants_GroupRequest.GroupRequestFields.FieldChoice_PublicFlag),
                        GroupName = groupName,
                        GroupOwner = spItem.RetrieveListItemValue(Constants_GroupRequest.GroupRequestFields.FieldText_GroupOwner),
                        Alias = groupName,
                        CleanAlias = alias
                    });
                }

                if (itemPosition == null)
                {
                    break;
                }
            }

            return results;
        }

        public async Task<O365GroupRequestModel> ProcessO365Group(GraphUtility utility, HttpClient client, O365GroupRequestModel groupItem, string defaultUser)
        {
            var groupTitle = groupItem.Title;
            var groupName = groupItem.Alias;
            var groupAlias = groupItem.CleanAlias;
            var groupIsPrivate = groupItem.PublicFlag?.Trim().Equals("Private", StringComparison.OrdinalIgnoreCase) ?? false;

            LogDebugging($"Processing: {groupTitle} cleaned:=>{groupAlias} for Name:=>{groupName}");
            var groupExists = false;
            var unifiedGroup = await GetUnifiedGroupResults(client, groupAlias);
            if (string.IsNullOrEmpty(unifiedGroup?.Id))
            {
                var groupOwner = await utility.GetUserId(groupItem.GroupOwner);
                var spoAdminUser = await utility.GetUserId(defaultUser);
                var members = new Microsoft.Graph.User[] { groupOwner };
                var owners = new Microsoft.Graph.User[] { groupOwner, spoAdminUser };

                var group = await utility.CreateUnifiedGroup(displayName: groupTitle, groupItem.Description,
                    groupAlias, owners, members, groupLogo: null, isPrivate: groupIsPrivate, createTeam: false, retryCount: 10, delay: 750);
                if (!string.IsNullOrEmpty(group?.Id))
                {
                    // Removing Owner || Member (SPOAdmin)
                    await utility.GroupRemoveOwner(spoAdminUser, group);
                    await utility.GroupRemoveMember(spoAdminUser, group);

                    groupItem.EmailAddress = group.Mail;
                    groupItem.SharePointDocumentsUrl = group.DocumentsUrl;
                    groupItem.SharePointSiteUrl = group.SiteUrl;
                    groupItem.WhenCreated = $"{group.CreatedDateTime?.UtcDateTime}";
                    groupExists = true;
                }
            }
            else
            {
                var group = await utility.GetUnifiedGroup(unifiedGroup.Id, includeSite: true);
                if (!string.IsNullOrEmpty(group?.Id))
                {
                    groupItem.EmailAddress = group.Mail;
                    groupItem.SharePointDocumentsUrl = group.DocumentsUrl;
                    groupItem.SharePointSiteUrl = group.SiteUrl;
                    groupItem.WhenCreated = $"{group.CreatedDateTime?.UtcDateTime}";
                    groupExists = true;
                }
            }

            // Update sharepoint request model
            groupItem.RequestCompletedFlag = groupExists ? "Yes" : "No";
            groupItem.Success = groupExists;

            return groupItem;
        }

        /// <summary>
        /// Whether a unified, security, or distribution group with the nickname parameter already exists.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mailNickname"></param>
        /// <returns></returns>
        public async Task<Microsoft.Graph.Group> GetUnifiedGroupResults(HttpClient client, string mailNickname)
        {
            // concat to provide 'mail' property
            var groupFilter = $"mail eq '{mailNickname}@{EmailDomain}' or mail eq '{mailNickname}@{CloudDomain}'";

            // Check if group already exists
            var graphUri = ToUrl($"{GraphEndpoint}groups?$filter=groupTypes/any(c:c+eq+'Unified') and ({groupFilter})");
            var response = await graphUri.InvokePostAsync(async () =>
            {
                var request = await client.GetAsync(graphUri, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                var groupRequestSearch = await request.DeserializeResult<JSONAuditCollection<Microsoft.Graph.Group>>().ConfigureAwait(false);
                return groupRequestSearch;
            });

            if (response == null || response.Results?.Any() == false || string.IsNullOrEmpty(response.Results.FirstOrDefault()?.Id))
            {
                graphUri = ToUrl($"{GraphEndpoint}groups?$filter=({groupFilter})");
                response = await graphUri.InvokePostAsync(async () =>
                {
                    var request = await client.GetAsync(graphUri, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                    var groupRequestSearch = await request.DeserializeResult<JSONAuditCollection<Microsoft.Graph.Group>>().ConfigureAwait(false);
                    return groupRequestSearch;
                });
            }

            return response?.Results.FirstOrDefault();
        }

        public async Task UpdateGroupsIntoSharepoint(ClientContext siteContext, GraphUtility utility)
        {
            var groupRequestList = siteContext.Web.GetList(GroupListName, lctx => lctx.Title, lctx => lctx.Id);
            var UnifiedGroups = await utility.ListUnifiedGroups(includeSite: true, includeOwner: true, includeMembership: true);
            foreach (var group in UnifiedGroups)
            {
                try
                {
                    SetGroupInList(groupRequestList, group);
                }
                catch (Exception ex)
                {
                    EPA.Office365.Diagnostics.Log.LogError(ex, $"Failed to push Unified Group ({group.Mail}) into Governance List {ex.Message}");
                }
            }
        }

        private void SetGroupInList(Microsoft.SharePoint.Client.List groupList, UnifiedGroupEntity group)
        {

            var itemId = 0;
            var groupEmails = group.ProxyAddresses;
            var camlFilter = "<In><FieldRef Name='EmailAddress'/><Values>";

            foreach (var groupEmail in groupEmails)
            {
                var tmpGroupEmail = groupEmail.ToLower();
                tmpGroupEmail = tmpGroupEmail.Replace("smtp:", "");
                tmpGroupEmail = tmpGroupEmail.Replace("spo:", "");
                camlFilter += "<Value Type='Text'>" + tmpGroupEmail.Trim() + "</Value>";
            }

            camlFilter += "</Values></In>";

            var camlQuery = CAML.ViewQuery(ViewScope.RecursiveAll,
                CAML.Where(camlFilter),
                string.Empty,
                CAML.ViewFields((new string[] { "Modified", "Title", "ID" }).Select(s => CAML.FieldRef(s)).ToArray()),
                1);

            var spQuery = new CamlQuery()
            {
                ViewXml = camlQuery
            };

            var spListItems = groupList.GetItems(spQuery);
            groupList.Context.Load(spListItems);
            groupList.Context.ExecuteQueryRetry(15, 1000, userAgent: RunningUserAgent);

            foreach (var listItem in spListItems)
            {
                var itemDate = listItem.RetrieveListItemValue("Modified").ToDateTime();
                itemId = listItem.Id;
                break;
            }

            var strGroupOwners = string.Join(";", group?.GroupOwners?.Select(s => s.UserPrincipalName));
            var strGroupOwnerNames = string.Join(";", group?.GroupOwners?.Select(s => s.DisplayName));
            var strGroupMembers = string.Join(";", group?.GroupMembers?.Select(s => s.UserPrincipalName));

            var operation = itemId == 0 ? "Add" : "Update";
            LogVerbose($@"{operation} item
                itemId: {itemId}
                group.Alias => { group.MailNickname}
                group.PrimarySmtpAddress => { group.Mail}
                strGroupOwners => { strGroupOwners}
                strGroupMembers => { strGroupMembers}
                SharePointDocumentsUrl => { group.DocumentsUrl}
                SharePointSiteUrl => { group.SiteUrl}
                Notes => { group.Description}
                AccessType => { group.Visibility}");

            if (itemId == 0)
            {
                // This scenario should no longer occur as group creation can only happen through the request process
                LogWarning($"Create item {group.Mail} SharePointSiteUrl {group.SiteUrl}");
                var itemCreateInfo = new Microsoft.SharePoint.Client.ListItemCreationInformation();
                var spListItem = groupList.AddItem(itemCreateInfo);

                // add item
                spListItem[ConstantsFields.Field_Title] = group.MailNickname;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_GroupName] = group.DisplayName;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_EmailAddress] = group.Mail;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_GroupOwner] = strGroupOwners;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_GroupOwnerName] = strGroupOwnerNames;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_GroupMembers] = strGroupMembers;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_SharePointDocumentsUrl] = group.DocumentsUrl;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_SharePointSiteUrl] = group.SiteUrl;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldChoice_PublicFlag] = group.Visibility;
                spListItem.Update();
                groupList.Context.ExecuteQueryRetry(15, 1000, userAgent: RunningUserAgent);
            }
            else
            {
                // Get item to be updated
                var spListItem = groupList.GetItemById(itemId);
                groupList.Context.Load(spListItem);
                groupList.Context.ExecuteQueryRetry(15, 1000, userAgent: RunningUserAgent);

                // update item
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_EmailAddress] = group.Mail;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_GroupOwner] = strGroupOwners;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_GroupOwnerName] = strGroupOwnerNames;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_GroupMembers] = strGroupMembers;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_SharePointDocumentsUrl] = group.DocumentsUrl;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldText_SharePointSiteUrl] = group.SiteUrl;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldChoice_PublicFlag] = group.Visibility;
                spListItem[Constants_GroupRequest.GroupRequestFields.FieldDate_GroupCreatedDate] = group.CreatedDateTime?.UtcDateTime;
                spListItem.Update();
                groupList.Context.ExecuteQueryRetry(15, 1000, userAgent: RunningUserAgent);
            }
        }


        public async Task CleanupGroupsIntoSharepoint(ClientContext siteContext, GraphUtility utility)
        {
            var groupRequestList = siteContext.Web.GetList(GroupListName, lctx => lctx.Title, lctx => lctx.Id);

            var UnifiedGroups = await utility.ListUnifiedGroups(includeSite: true);
            var groupListFieldRefs = (new string[] {
                Constants_GroupRequest.GroupRequestFields.FieldText_EmailAddress,
                Constants_GroupRequest.GroupRequestFields.FieldText_RequestCompletedFlag,
                ConstantsFields.Field_Title
            }).Select(s => CAML.FieldRef(s)).ToArray();
            LogVerbose($"Total Groups {UnifiedGroups.Count()}");
            foreach (var group in UnifiedGroups)
            {
                CleanupGroupInList(groupRequestList, group, groupListFieldRefs);
            }
        }

        private void CleanupGroupInList(Microsoft.SharePoint.Client.List groupList, UnifiedGroupEntity group, string[] groupListFieldRefs)
        {
            var groupsEmails = new List<string>();

            var tmpGroupEmail = string.Empty;
            foreach (var groupEmail in group.ProxyAddresses)
            {
                tmpGroupEmail = groupEmail.ToLower();
                tmpGroupEmail = tmpGroupEmail.Replace("smtp:", "");
                tmpGroupEmail = tmpGroupEmail.Replace("spo:", "");
                groupsEmails.Add(tmpGroupEmail);

                if (tmpGroupEmail.IndexOf(EmailDomain, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var flipEmail = tmpGroupEmail.Replace(EmailDomain, CloudDomain);
                    groupsEmails.Add(flipEmail);
                }

                if (tmpGroupEmail.IndexOf(CloudDomain, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var flipEmail = tmpGroupEmail.Replace(CloudDomain, EmailDomain);
                    groupsEmails.Add(flipEmail);
                }
            }

            var cleanupItemsArray = new List<int>();

            // Retreive all "Approved" | "Completed" requests which have an Email Address
            var spQuery = new CamlQuery()
            {
                ViewXml = CAML.ViewQuery(
                    ViewScope.RecursiveAll,
                    CAML.Where(CAML.Eq(CAML.FieldValue(Constants_GroupRequest.GroupRequestFields.FieldText_RequestCompletedFlag, FieldType.Text.ToString("f"), "Yes"))),
                    string.Empty,
                    CAML.ViewFields(groupListFieldRefs),
                    20),
                ListItemCollectionPosition = null
            };

            var emails = new List<EmailItems>();

            do
            {
                var spListItems = groupList.GetItems(spQuery);
                this.ClientContext.Load(spListItems);
                this.ClientContext.ExecuteQuery();
                spQuery.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                // Query the SP ListItem to verify if the current UnifiedMailboxes email addresses match the ListItem Email Address
                // If no match was found then [1. The Email Address was changed or 2. The Email address was deleted]
                foreach (var listItem in spListItems)
                {
                    var emailAddress = listItem.RetrieveListItemValue(Constants_GroupRequest.GroupRequestFields.FieldText_EmailAddress);
                    if (!string.IsNullOrEmpty(emailAddress))
                    {
                        emails.Add(new EmailItems
                        {
                            Id = listItem.Id,
                            EmailAddress = emailAddress.Trim()
                        });
                    }
                }
            }
            while (spQuery.ListItemCollectionPosition != null);


            LogVerbose($"Completed Groups {emails.Count()}");

            foreach (var listItem in emails)
            {
                var emailAddress = listItem.EmailAddress;
                var foundEmailAddress = groupsEmails.Any(ge => ge.IndexOf(emailAddress, StringComparison.CurrentCultureIgnoreCase) > -1);
                if (!foundEmailAddress)
                {
                    LogVerbose($"{emailAddress} was not found in O365 Email/Mailboxes");
                    cleanupItemsArray.Add(listItem.Id);
                }
                else
                {
                    LogVerbose($"{emailAddress} was found in O365 Email/Mailboxes");
                }
            }

            var ratioOfGroups = (cleanupItemsArray.Count() / groupsEmails.Count());
            LogVerbose($"cleanupItemsArray: {cleanupItemsArray.Count()} and total Groups Array {groupsEmails.Count()}");

            foreach (var itemId in cleanupItemsArray)
            {

                LogVerbose($"Deleting item: {itemId}");
                if (this.ShouldProcess($"Deleting item: {itemId}"))
                {
                    var spListItem = groupList.GetItemById(itemId);
                    this.ClientContext.Load(spListItem);
                    this.ClientContext.ExecuteQuery();
                    spListItem.DeleteObject();
                    this.ClientContext.ExecuteQuery();
                }
            }
        }

        private static string GetCleanEmailAlias(IEnumerable<char> illegalCharacters, string groupName)
        {
            var alias = groupName.Replace(" ", "_").Trim().Trim('_');
            illegalCharacters.ToList().ForEach(illegal =>
            {
                var escaped = Regex.Escape(illegal.ToString());
                if (Regex.IsMatch(alias, escaped))
                {
                    alias = Regex.Replace(alias, escaped, "");
                }
            });

            return alias;
        }

        private IEnumerable<char> CleanIllegalCharacters()
        {
            var illegalCharacters = new List<char>();

            for (var idx = 0; idx <= 34; idx++)
            {
                illegalCharacters.Add((char)idx);
            }

            illegalCharacters.Add((char)40);
            illegalCharacters.Add((char)41);
            illegalCharacters.Add((char)44);
            illegalCharacters.Add((char)47);

            for (var idx = 58; idx <= 60; idx++)
            {
                illegalCharacters.Add((char)idx);
            }

            illegalCharacters.Add((char)62);
            illegalCharacters.Add((char)64);

            for (var idx = 91; idx <= 93; idx++)
            {
                illegalCharacters.Add((char)idx);
            }

            for (var idx = 127; idx <= 160; idx++)
            {
                illegalCharacters.Add((char)idx);
            }

            return illegalCharacters;
        }

        private Uri ToUrl(string graphUrl)
        {
            // JSON Format not supported by V1.0 endpoint
            var versionuri = (Opts.BetaEndPoint) ? "beta" : "v1.0";

            var uri = new Uri(string.Format(graphUrl, versionuri));
            return uri;
        }
    }
}
