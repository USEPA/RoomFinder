using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEPAExternalRecertifyUsers", HelpText = "Catalogs external users and descrepancies for the sharepoint site")]
    public class ScanEPAExternalRecertifyUsersOptions : TenantCommandOptions
    {
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        [Option("process-option", Required = true, Default = ScanEPAExternalRecertifyUsersAction.TestConnection)]
        public ScanEPAExternalRecertifyUsersAction ExternalProcessOption { get; set; }
    }

    public enum ScanEPAExternalRecertifyUsersAction
    {
        TestConnection,
        Expired,
        RemoveExpired,
        NotifyExpired
    }

    public static class ScanEPAExternalRecertifyUsersOptionsExtensions
    {
        public static int RunGenerateAndReturnExitCode(this ScanEPAExternalRecertifyUsersOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPAExternalRecertifyUsers(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Catalogs external users and descrepancies for the sharepoint site
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Too much effort to handle exceptions.")]
    public class ScanEPAExternalRecertifyUsers : BaseSpoTenantCommand<ScanEPAExternalRecertifyUsersOptions>
    {
        public ScanEPAExternalRecertifyUsers(ScanEPAExternalRecertifyUsersOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private Variables

        private List<string[]> SitesWithExpiredUsers { get; } = new List<string[]> { };
        private List<string[]> SiteExternalUsers { get; } = new List<string[]> { };
        private List<string> SiteExternalUsersTmp { get; } = new List<string> { };
        private List<string[]> SCAsList { get; } = new List<string[]> { };

        //Set OOB libraries list
        private string[] OobDocLibs { get; } = {
            "Device Channels",
            "Converted Forms",
            "Content type publishing error log",
            "Content and Structure Reports",
            "Cache Profiles",
            "appdata",
            "Access Requests",
            "Community Members",
            "Categories",
            "MicroFeed",
            "Composed Looks",
            "Categories",
            "Badges",
            "Maintenance Log Library",
            "Master Page Gallery",
            "Reporting Templates",
            "Drop Off Library",
            "Form Templates",
            "wfpub",
            "Workflows",
            "Images",
            "App Packages",
            "Request Forms",
            "Forms"
        };

        //Argument flags
        private bool _processRemoveExpired = false;

        private readonly string externalUserListName = "External Users";
        private readonly string _extUserListName = "Expired External Users";
        private string TenantHostAuthority { get; set; }
        private string TenantMySiteAuthority { get; set; }

        #endregion

        public override void OnInit()
        {
            var TenantUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();

            if (Opts.ExternalProcessOption == ScanEPAExternalRecertifyUsersAction.RemoveExpired)
            {
                _processRemoveExpired = true;
            }
        }

        public override int OnRun()
        {
            try
            {
                var tenantShortName = Settings.Commands.TenantShortName;
                TenantHostAuthority = $"https://{tenantShortName}.";
                TenantMySiteAuthority = $"https://{tenantShortName}-my.";

                var _tenantUrl = Settings.Commands.TenantAdminUrl;
                var externalSiteUrl = Settings.Commands.ExternalUsersSiteUrl;
                var externalSiteListName = Settings.Commands.ExternalSiteListName;

                LogVerbose("Started: {0} ..........", DateTime.Now);


                if (Opts.ExternalProcessOption == ScanEPAExternalRecertifyUsersAction.Expired)
                {
                    LogVerbose(">>Get Expired Users {0}", DateTime.Now);
                    //Compile a list of the expied users + sites they have rights to
                    using var thisContext = this.ClientContext.Clone(externalSiteUrl);
                    CompileExpiredUsersSites(thisContext);
                }

                if ((Opts.ExternalProcessOption == ScanEPAExternalRecertifyUsersAction.NotifyExpired) 
                    || (_processRemoveExpired))
                {
                    if (_processRemoveExpired)
                    {
                        LogVerbose("Deleting Expired Users {0}", DateTime.Now);
                    }
                    else
                    {
                        LogVerbose("Notify Expired Users {0}", DateTime.Now);
                    }

                    using var _extCtx = this.ClientContext.Clone(externalSiteUrl);
                    Process_ExternalUserCertification(_extCtx, externalUserListName);
                }

                if (Opts.ExternalProcessOption == ScanEPAExternalRecertifyUsersAction.TestConnection)
                {
                    using var thisContext = this.ClientContext.Clone(externalSiteUrl);
                    TestSiteQueries(thisContext, externalSiteListName);
                }

                LogVerbose($"Completed {DateTime.Now} ................. ");
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed in RemoveExternalUsers");
                return -1;
            }

            return 1;
        }

        public enum UserStatus
        {
            New,
            Exist,
            Admin,
            ExceedsMismatch
        }

        UserStatus ExternalUserAlreadyRevoked(ClientContext reportContext, string _AcceptedAs, string _InvitedAs)
        {
            var _externalUserExists = UserStatus.New;
            var mismatchTimeInterval = Settings.Commands.MismatchTimeInterval;
            var mismatchCount = Settings.Commands.MismatchCount;
            LogVerbose("Checking user: {0} on {1}", _AcceptedAs, _InvitedAs);

            try
            {
                var _webExtUsers = reportContext.Web;
                reportContext.Load(_webExtUsers);
                reportContext.ExecuteQuery();


                var _ExternalUsersReportList = reportContext.Web.Lists.GetByTitle(externalUserListName);
                reportContext.Load(_ExternalUsersReportList);
                reportContext.ExecuteQuery();


                //Set time stamp for x hours earlier
                int _timeDiff = Convert.ToInt32(mismatchTimeInterval);
                DateTime dt = DateTime.Now.Subtract(new TimeSpan(0, _timeDiff, 0, 0));
                //format time to iso8906
                string _timeStamp = dt.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");

                ListItemCollectionPosition itemPosition = null;

                while (true)
                {

                    CamlQuery camlQuery = new CamlQuery
                    {
                        ListItemCollectionPosition = itemPosition
                    };
                    string _camlQuery = "<View>";
                    _camlQuery += "<OrderBy><FieldRef Name='ID' Ascending='FALSE'/></OrderBy>";
                    _camlQuery += "<Query><Where><And><Geq><FieldRef Name='Created' /><Value Type='DateTime' IncludeTimeValue='True'>" + _timeStamp + "</Value></Geq>";
                    _camlQuery += "<And><Eq><FieldRef Name='Accepted_x0020_As' /><Value Type='Text'>" + _AcceptedAs + "</Value></Eq>";
                    _camlQuery += "<Eq><FieldRef Name='Invited_x0020_As' /><Value Type='Text'>" + _InvitedAs + "</Value></Eq>";
                    _camlQuery += "</And></And></Where><RowLimit>50</RowLimit></Query></View>";

                    camlQuery.ViewXml = _camlQuery;
                    ListItemCollection listItems = _ExternalUsersReportList.GetItems(camlQuery);
                    reportContext.Load(listItems);
                    reportContext.ExecuteQuery();

                    itemPosition = listItems.ListItemCollectionPosition;

                    if (listItems.Count == Convert.ToInt32(mismatchCount))
                    {
                        _externalUserExists = UserStatus.Admin;
                    }
                    else if (listItems.Count > Convert.ToInt32(mismatchCount))
                    {
                        var totalCount = listItems.Count.ToString();
                        _externalUserExists = UserStatus.ExceedsMismatch;
                    }
                    else
                    {
                        foreach (ListItem listItem in listItems)
                        {
                            LogDebugging("{0} -- {1}", listItem.Id, listItem["Created"].ToString());
                            _externalUserExists = UserStatus.Exist;
                            break;
                        }
                    }

                    if (itemPosition == null)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Does external user exist check.");
            }

            return _externalUserExists;
        }

        private void RevokeExternalUser(string[] _externalUserArr, string _ExternalUsetEmail)
        {
            LogDebugging("Revoking: {0} [{1}]", _ExternalUsetEmail, _externalUserArr[0]);
            try
            {
                this.OfficeTenantContext.RemoveExternalUsers(_externalUserArr);
                this.ClientContext.ExecuteQuery();
            }
            catch (Exception e)
            {
                LogError(e, "Failed on revokeExternalUser {0}", _ExternalUsetEmail);
            }
        }

        /// <summary>
        /// Compile Expired External Users
        /// </summary>
        /// <param name="reportContext"></param>
        private void CompileExpiredUsersSites(ClientContext reportContext)
        {

            LogVerbose(">> Compiling External users for {0}", reportContext.Url);
            try
            {
                //Compile SCA list
                CompileSCAs_ExpiredUsersSitesList();

                var sites = GetSiteCollections();

                foreach (var site in sites)
                {
                    LogVerbose(site.Url);
                    SetSiteAdmin(site.Url, CurrentUserName, true);
                    ProcessSiteCollectionForExternalUsers(site.Url);
                    SetSiteAdmin(site.Url, CurrentUserName);
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in compile_ExpiredUsersSites {0}", reportContext.Url);
            }
        }

        private bool IsScannableSite(string siteUrl)
        {
            return ((siteUrl.ToLower().IndexOf(TenantHostAuthority) > -1)
                            || (siteUrl.ToLower().IndexOf(TenantMySiteAuthority) > -1));
        }

        private void ProcessSiteCollectionForExternalUsers(string _siteUrl)
        {
            try
            {
                //WriteVerbose(" --> " + _siteUrl);
                using var ctx = this.ClientContext.Clone(_siteUrl);
                Web _web = ctx.Web;

                ctx.Load(_web);
                ctx.Load(_web.Webs);
                ctx.ExecuteQuery();

                ProcessSite(ctx, _web);

                foreach (Web _inWeb in _web.Webs)
                {
                    if (IsScannableSite(_inWeb.Url))
                    {
                        ProcessSubSitesFoExternalUsers(ctx, _inWeb);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processSiteCollectionForExternalUsers {0}", _siteUrl);
            }
        }

        private void ProcessSubSitesFoExternalUsers(ClientContext ctx, Web _web)
        {
            try
            {
                ctx.Load(_web.Webs);
                ctx.ExecuteQuery();

                ctx.Load(_web.Webs);
                ctx.ExecuteQuery();

                //Process root site
                ProcessSite(ctx, _web);

                //Process sub-sites
                foreach (Web _inWeb in _web.Webs)
                {
                    //check if site is an app
                    if (IsScannableSite(_inWeb.Url))
                    {
                        ProcessSubSitesFoExternalUsers(ctx, _inWeb);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processSubSitesFoExternalUsers {0}", ctx.Url);
            }

        }

        private void ProcessSite(ClientContext ctx, Web _web)
        {
            //clear array
            SiteExternalUsers.Clear();
            SiteExternalUsersTmp.Clear();

            LogVerbose("Processing Site: {0}", _web.Url);

            try
            {
                // ********** Process Lists
                ListCollection _lists = _web.Lists;
                ctx.Load(_lists);
                ctx.ExecuteQuery();

                foreach (List _list in _lists)
                {
                    if (!OobDocLibs.Contains(_list.Title))
                    {

                        bool _ctFound = false;
                        ContentTypeCollection _CTs = _list.ContentTypes;

                        ctx.Load(_CTs);
                        ctx.ExecuteQuery();

                        //Check if the list is of document content type
                        foreach (ContentType _CT in _CTs)
                        {
                            LogVerbose("--> " + _CT.Name);
                            if (_CT.Name == "Document")
                            {
                                _ctFound = true;
                                break;
                            }
                        }

                        if (_ctFound)
                        {
                            ProcessListItems(ctx, _list);
                        }
                    }

                    ProcessList(ctx, _list);
                }

                // ********** Process Site groups
                ctx.Load(_web, i => i.HasUniqueRoleAssignments);
                ctx.ExecuteQuery();

                //only process if site has unique permissions
                if (_web.HasUniqueRoleAssignments)
                {
                    LogVerbose(">>> Processing Groups");
                    ProcessGroups(ctx, _web);
                }

                // ********** Process Site users
                Process_UserHasPermissions(ctx, _web);

                //Process collected data
                if (SiteExternalUsers.Count > 0)
                {
                    string _siteOwners = GetOwners(_web.Url);
                    foreach (string[] _extUser in SiteExternalUsers)
                    {
                        if (!(SiteExternalUsersTmp.Contains(_extUser[0])))
                        {
                            SiteExternalUsersTmp.Add(_extUser[0]);
                            ProcessExternalUsersList(_extUser[0], _siteOwners, _web.Url);
                        }
                    }
                }
                else
                {
                    LogVerbose("External Users Count: " + SitesWithExpiredUsers.Count);
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processSite {0}", ctx.Url);
            }
        }

        private void ProcessExternalUsersList(string _ExternalUser, string _siteOwners, string _siteUrl)
        {
            string _userAccessList = "";
            string _displayName = "";

            foreach (string[] _extUser in SiteExternalUsers)
            {
                if (_extUser[0] == _ExternalUser)
                {
                    if (string.IsNullOrEmpty(_displayName))
                    {
                        _displayName = _extUser[1];
                    }

                    _userAccessList += _extUser[2] + ": " + _extUser[3] + "<br/>";
                }
            }

            if (string.IsNullOrEmpty(_userAccessList))
            {
                _ExternalUser = RemoveClaimIdentifier(_ExternalUser);

                if (!DoesExpiredUserExistsinList(_ExternalUser, _siteUrl))
                {
                    LogVerbose(">>-->>-->> " + _ExternalUser + " -- " + _displayName + " -- " + _userAccessList);
                    AddExpiredUsertoList(_ExternalUser, _displayName, _siteUrl, _siteOwners, _userAccessList);
                }
                else
                {
                    LogVerbose(">> Exisit >> " + _ExternalUser + " -- " + _displayName + " -- " + _userAccessList);
                }
            }
        }

        private void ProcessGroups(ClientContext ctx, Web _web)
        {
            // ********** Process Groups
            LogVerbose(">>> Processing Groups");
            try
            {
                RoleAssignmentCollection _roleAssignments = _web.RoleAssignments;
                ctx.Load(_roleAssignments);
                ctx.ExecuteQuery();

                foreach (RoleAssignment _role in _roleAssignments)
                {
                    try
                    {
                        ctx.Load(_role, i => i.Member);
                        ctx.ExecuteQuery();

                        if (_role.Member.PrincipalType.ToString() == "SharePointGroup")
                        {
                            Group _group = _web.SiteGroups.GetById(_role.Member.Id);
                            UserCollection _users = _group.Users;

                            ctx.Load(_group);
                            ctx.Load(_users);
                            ctx.ExecuteQuery();

                            foreach (User _user in _users)
                            {
                                if (_user.PrincipalType == Microsoft.SharePoint.Client.Utilities.PrincipalType.User)
                                {
                                    if (IsExternalUserFilter(_user.LoginName))
                                    {
                                        LogVerbose("---> Adding: " + _user.LoginName.ToLower() + " -- " + _role.Member.Title);
                                        SiteExternalUsers.Add(new string[] { _user.LoginName, _user.Title, "Group", _role.Member.Title });

                                    }
                                }
                            }
                        }
                        else
                        {
                            LogVerbose(_role.Member.Title + " -+- " + _role.Member.PrincipalType);//_web.Title);

                            if (IsExternalUserFilter(_role.Member.LoginName))
                            {
                                LogVerbose("---> Adding: " + _role.Member.LoginName.ToLower() + " -- " + _web.Url);
                                SiteExternalUsers.Add(new string[] { _role.Member.LoginName, _role.Member.Title, "Site", _web.Url });
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processGroups");
            }

        }

        private void ProcessList(ClientContext ctx, List _list)
        {
            try
            {
                ctx.Load(_list, l => l.HasUniqueRoleAssignments);
                ctx.ExecuteQuery();

                if (_list.HasUniqueRoleAssignments)
                {
                    LogVerbose(">>> Processing List: " + _list.Title);

                    RoleAssignmentCollection _roleAssignments = _list.RoleAssignments;
                    ctx.Load(_roleAssignments);
                    ctx.ExecuteQuery();


                    foreach (RoleAssignment _role in _roleAssignments)
                    {

                        ctx.Load(_role, i => i.Member);
                        ctx.ExecuteQuery();

                        if (IsExternalUserFilter(_role.Member.Title))
                        {
                            SiteExternalUsers.Add(new string[] { _role.Member.LoginName.ToString(), _role.Member.Title.ToString(), "list", _list.Title });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processList");
            }

        }

        private void ProcessListItems(ClientContext ctx, List _list)
        {
            try
            {
                ListItemCollectionPosition itemPosition = null;

                while (true)
                {
                    CamlQuery camlQuery = new CamlQuery
                    {
                        ListItemCollectionPosition = itemPosition,
                        ViewXml = @"<Query><RowLimit>50</RowLimit></Query>"
                    };
                    ListItemCollection listItems = _list.GetItems(camlQuery);
                    ctx.Load(listItems);
                    ctx.ExecuteQuery();
                    itemPosition = listItems.ListItemCollectionPosition;

                    foreach (ListItem listItem in listItems)
                    {
                        ctx.Load(listItem, l => l.HasUniqueRoleAssignments);
                    }

                    ctx.ExecuteQuery();

                    foreach (ListItem listItem in listItems)
                    {
                        if (listItem.HasUniqueRoleAssignments)
                        {
                            LogVerbose(">>> Processing Item: " + listItem["ID"]);

                            RoleAssignmentCollection _roleAssignments = listItem.RoleAssignments;
                            ctx.Load(_roleAssignments);
                            ctx.ExecuteQuery();


                            foreach (RoleAssignment _role in _roleAssignments)
                            {

                                ctx.Load(_role, i => i.Member);
                                ctx.ExecuteQuery();

                                if (IsExternalUserFilter(_role.Member.LoginName))
                                {
                                    LogVerbose(">>>>> " + _role.Member.Title + " in item: " + listItem["FileRef"] + " in list: " + _list.Title);
                                    SiteExternalUsers.Add(new string[] { _role.Member.LoginName.ToString(), _role.Member.Title.ToString(), "Item", listItem["FileRef"].ToString() });
                                }
                            }
                        }
                    }

                    if (itemPosition == null)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processListItems");
            }
        }

        private void Process_UserHasPermissions(ClientContext ctx, Web _web)
        {
            UserCollection _SiteUsers = _web.SiteUsers;
            ctx.Load(_SiteUsers);
            ctx.ExecuteQuery();

            foreach (User _user in _SiteUsers)
            {
                if (IsExternalUserFilter(_user.LoginName))
                {
                    try
                    {
                        ClientResult<BasePermissions> _perms = _web.GetUserEffectivePermissions(_user.LoginName);

                        //if external user has access.. add to list
                        if (_perms.Value.Has(PermissionKind.ViewPages))
                        {
                            LogVerbose("Adding," + _user.LoginName.ToLower() + " -- " + _web.Url.ToLower() + " -- " + _user.Title);
                            SiteExternalUsers.Add(new string[] { _user.LoginName, _user.Title, "Site", _user.Id.ToString() });
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed in process_UserHasPermissions {0}, {1}", _web.Url.ToLower(), _user.LoginName);
                    }

                }
            }
        }


        private void CompileSCAs_ExpiredUsersSitesList()
        {
            LogVerbose("Setting up site collection administrators ...");
            var SCASiteUrl = Settings.Commands.SiteCollectionAdminUrl;
            var eternalUsersSiteUrl = Settings.Commands.ExternalUsersSiteUrl;
            var externalSiteSCAList = Settings.Commands.ExternalSiteCAListName;

            try
            {
                //Build SCA/Region list
                var _scaCtx = this.ClientContext.Clone(SCASiteUrl);

                List _scaList = _scaCtx.Web.Lists.GetByTitle(externalSiteSCAList);
                CamlQuery _scaCAML = new CamlQuery
                {
                    ViewXml = @"<View><ViewFields>
                                <FieldRef Name='Title'/>
                                <FieldRef Name='Region_x0020_or_x0020_Program_x0'/>
                                <FieldRef Name='Employee_x0020_Name'/>
                                </ViewFields></View>"
                };

                ListItemCollection _SCAs = _scaList.GetItems(_scaCAML);
                _scaCtx.Load(_scaList);
                _scaCtx.Load(_SCAs);
                _scaCtx.ExecuteQuery();

                foreach (ListItem _SCA in _SCAs)
                {
                    FieldUserValue _sca = (FieldUserValue)_SCA["Employee_x0020_Name"];
                    LogVerbose(_SCA["Title"] + " -- " + _SCA["Region_x0020_or_x0020_Program_x0"] + " -- " + _SCA["Employee_x0020_Name"] + " -- " + _sca.Email + " -- " + _sca.LookupValue);// + " -- " + _sca.Email);
                    string[] _tmpSCAArr = new string[] { _SCA["Region_x0020_or_x0020_Program_x0"].ToString(), _sca.Email };
                    SCAsList.Add(_tmpSCAArr);
                }



            }
            catch (Exception e)
            {
                LogError(e, "Failed in compileSCAs_ExpiredUsersSitesList {0}", eternalUsersSiteUrl);
            }

        }

        private string GetSCAforRegion(string _strRegion)
        {
            string _scaEmail = "";

            foreach (string[] _tmpSCA in SCAsList)
            {
                if (_strRegion.ToLower() == _tmpSCA[0].ToLower())
                {
                    if (!string.IsNullOrEmpty(_scaEmail))
                    {
                        _scaEmail += ";" + _tmpSCA[1];
                    }
                    else
                    {
                        _scaEmail = _tmpSCA[1];
                    }
                }
            }

            return _scaEmail;
        }

        private bool IsExternalUserNew(ClientContext reportContext, string _extUser, string _siteUrl)
        {
            LogVerbose(">>> " + _extUser);
                        bool _extUserIsNew = true;

            CamlQuery _query = new CamlQuery
            {
                ViewXml = @"<View><Query>" +
                                "<Where>" +
                                    "<Eq>" +
                                        "<FieldRef Name=\"External_x0020_User\" />" +
                                        "<Value Type=\"Text\">" + _extUser + "</Value>" +
                                    "</Eq>" +
                                "</Where>" +
                                 "<ViewFields>" +
                                    "<FieldRef Name=\"External_x0020_User\"/>" +
                                    "<FieldRef Name=\"Site_x0020_Url\" />" +
                                 "</ViewFields>" +
                            "</Query></View>"
            };
            ListItemCollection _extUserItemItems = reportContext.Web.Lists.GetByTitle(_extUserListName).GetItems(_query);
            reportContext.Load(_extUserItemItems);
            reportContext.ExecuteQuery();

            foreach (ListItem _extUserItem in _extUserItemItems)
            {
                if (_extUserItem["Site_x0020_Url"].ToString().ToLower().Trim() == _siteUrl.ToLower().Trim())
                {
                    _extUserIsNew = false;
                    break;
                }
            }
            return _extUserIsNew;
        }

        private void AddExpiredUsertoList(string _ExpiredExternalUser, string _ExpiredExternalUserDisplayName, string _siteUrl, string _siteOwners, string _accessResult)
        {

            LogVerbose($">> Adding >> {_ExpiredExternalUser} -- {_ExpiredExternalUserDisplayName} -- {_siteUrl}");
            var externalUsersSiteUrl = Settings.Commands.ExternalUsersSiteUrl;

            try
            {
                var _ctxExtUsers = this.ClientContext.Clone(externalUsersSiteUrl);
                var _webExtUsers = _ctxExtUsers.Web;
                _ctxExtUsers.Load(_webExtUsers);
                _ctxExtUsers.ExecuteQuery();

                string[] _ownersArr = _siteOwners.Split(new char[] { ';' });

                LogVerbose($"owners array: {_siteOwners} owners count: {_ownersArr.Length}");

                FieldUserValue[] _usersValues = new FieldUserValue[_ownersArr.Length - 1];

                int _count = 0;
                if (_siteOwners != null)
                {
                    foreach (string _userOwner in _ownersArr)
                    {
                        LogVerbose(">>--->> " + _userOwner);

                        try
                        {
                            if ((_userOwner.IndexOf("System") < 0) && (!(string.IsNullOrEmpty(_userOwner))))
                            {
                                User _user = _webExtUsers.EnsureUser(_userOwner);
                                _ctxExtUsers.Load(_user);
                                _ctxExtUsers.ExecuteQuery();

                                FieldUserValue _ufv = new FieldUserValue
                                {
                                    LookupId = _user.Id
                                };

                                LogVerbose(">>--+->> " + _user.LoginName);

                                _usersValues[_count] = _ufv;
                                _count++;
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(e, "Failed in AddExpiredUserToList {0}", _ExpiredExternalUser);
                        }
                    }
                }


                var _ExternalUsersReportList = _ctxExtUsers.Web.Lists.GetByTitle(externalUserListName);
                _ctxExtUsers.Load(_ExternalUsersReportList);
                _ctxExtUsers.ExecuteQuery();


                var _regionSiteType = GetRegionSiteType(_siteUrl);
                var _region = _regionSiteType.Region;
                string _siteCollectionUrl = GetSiteCollectionRootUrl(_siteUrl);

                //get SCA for region
                bool _scafoundFlag = false;
                string _tmpSCAs = GetSCAforRegion(_region);
                string[] _tmpSCAsArr = _tmpSCAs.Split(new char[] { ';' });
                FieldUserValue[] _SCAusers = new FieldUserValue[_tmpSCAsArr.Length];
                int _scacount = 0;

                try
                {
                    LogVerbose("_tmpSCAsArr: " + _tmpSCAsArr.Length);

                    foreach (string _userSCA in _tmpSCAsArr)
                    {
                        //  LogVerbose(_scacount + " : i: 0#.f|membership|" + _userSCA);
                        User _SCA1 = _ctxExtUsers.Web.EnsureUser("i:0#.f|membership|" + _userSCA);
                        _ctxExtUsers.Load(_SCA1);
                        _ctxExtUsers.ExecuteQuery();
                        //   LogVerbose("_SCA1.Id.ToString() : " + _SCA1.Id.ToString());
                        FieldUserValue _scaufv = new FieldUserValue
                        {
                            LookupId = _SCA1.Id
                        };
                        _SCAusers[_scacount] = _scaufv;
                        _scafoundFlag = true;
                        _scacount++;
                    }

                }
                catch (Exception e)
                {
                    LogVerbose(e.ToString());
                }
                ///////////////////////////

                LogVerbose("Adding:");
                LogVerbose(">> " + _ExpiredExternalUser.Trim());
                LogVerbose(">> " + _ExpiredExternalUserDisplayName.Trim());
                LogVerbose(">> " + _accessResult);
                LogVerbose(">> " + _siteUrl.Trim().ToLower());
                LogVerbose(">> " + _region.ToUpper().Trim());
                LogVerbose(">> " + _siteCollectionUrl.ToLower().Trim());

                if (_siteOwners != null)
                {
                    LogVerbose(">> " + _usersValues);
                }

                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = _ExternalUsersReportList.AddItem(itemCreateInfo);
                newItem["External_x0020_User"] = _ExpiredExternalUser.Trim();
                newItem["Title"] = _ExpiredExternalUserDisplayName.Trim();
                newItem["User_x0020_Access"] = _accessResult;
                newItem["Site"] = _siteUrl.Trim().ToLower();
                newItem["Region"] = _region.ToUpper().Trim();
                newItem["Site_x0020_Collection"] = _siteCollectionUrl.ToLower().Trim();

                if (_scafoundFlag)
                {
                    newItem["Site_x0020_Collection_x0020_Admi"] = _SCAusers;
                }

                if (_siteOwners != null)
                {
                    newItem["Owners"] = _usersValues;
                }
                // newItem["Created_x0020_Date0"] = Convert.ToDateTime(_ExpiredExternalUserCreatedDate);
                newItem.Update();
                _ctxExtUsers.ExecuteQuery();

            }
            catch (Exception e)
            {
                LogError(e, "Failed in ADdExpiredUserToList URL:{0}", _siteUrl);
            }
        }

        private void AddExpiredUsertoList(string _ExpiredExternalUser, string _ExpiredExternalUserCreatedDate, string _siteUrl, UserCollection _siteOwners)
        {

            LogVerbose(">> Adidng >> " + _ExpiredExternalUser + " -- " + _ExpiredExternalUserCreatedDate + " -- " + _siteUrl);

            //Determin site's region
            string[] _tmpSiteUrl = _siteUrl.Replace($"{TenantHostAuthority}sharepoint.com/sites/", "").Split(new char[] { '/' });
            string _siteCollectionUrl = $"{TenantHostAuthority}sharepoint.com/sites/" + _tmpSiteUrl[0];
            string[] _region = _tmpSiteUrl[0].Split(new char[] { '_' });
            var externalUserSiteUrl = Settings.Commands.ExternalUsersSiteUrl;

            try
            {
                var _ctxExtUsers = this.ClientContext.Clone(externalUserSiteUrl);
                var _webExtUsers = _ctxExtUsers.Web;
                _ctxExtUsers.Load(_webExtUsers);
                _ctxExtUsers.ExecuteQuery();

                FieldUserValue[] _usersValues = new FieldUserValue[_siteOwners.Count];

                int _count = 0;
                if (_siteOwners != null)
                {
                    foreach (User _userOwner in _siteOwners)
                    {
                        User _user = _webExtUsers.EnsureUser(_userOwner.LoginName);
                        _ctxExtUsers.Load(_user);
                        _ctxExtUsers.ExecuteQuery();

                        FieldUserValue _ufv = new FieldUserValue
                        {
                            LookupId = _user.Id
                        };

                        _usersValues[_count] = _ufv;
                        _count++;
                    }
                }

                var _ExternalUsersReportList = _ctxExtUsers.Web.Lists.GetByTitle(externalUserListName);
                _ctxExtUsers.Load(_ExternalUsersReportList);
                _ctxExtUsers.ExecuteQuery();


                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = _ExternalUsersReportList.AddItem(itemCreateInfo);
                newItem["External_x0020_User"] = _ExpiredExternalUser.Trim();
                newItem["Site"] = _siteUrl.Trim().ToLower();
                newItem["Region"] = _region[0].ToUpper().Trim();
                newItem["Site_x0020_Collection"] = _siteCollectionUrl.ToLower().Trim();
                if (_siteOwners != null)
                {
                    newItem["Owners"] = _usersValues;
                }
                // newItem["Created_x0020_Date0"] = Convert.ToDateTime(_ExpiredExternalUserCreatedDate);
                newItem.Update();
                _ctxExtUsers.ExecuteQuery();

            }
            catch (Exception e)
            {
                LogError(e, "Failed in AddExpiredUserToList {0}", _ExpiredExternalUser);
            }
        }

        private bool DoesExpiredUserExistsinList(string _ExpiredExternalUser, string _siteUrl)
        {
            bool _externalUserExists = false;
            var externalUsersSiteUrl = Settings.Commands.ExternalUsersSiteUrl;

            LogVerbose("Checking user: " + _ExpiredExternalUser + " on " + _siteUrl);

            try
            {
                var _ctxExtUsers = new ClientContext(externalUsersSiteUrl);
                var _webExtUsers = _ctxExtUsers.Web;
                _ctxExtUsers.Load(_webExtUsers);
                _ctxExtUsers.ExecuteQuery();

                var _ExternalUsersReportList = _ctxExtUsers.Web.Lists.GetByTitle(externalUserListName);
                _ctxExtUsers.Load(_ExternalUsersReportList);
                _ctxExtUsers.ExecuteQuery();


                ListItemCollectionPosition itemPosition = null;

                while (true)
                {

                    CamlQuery camlQuery = new CamlQuery
                    {
                        ListItemCollectionPosition = itemPosition,
                        ViewXml = @"<View><Query><Where><And><Eq><FieldRef Name='External_x0020_User' /><Value Type='Text'>"
                        + _ExpiredExternalUser + "</Value></Eq><Eq><FieldRef Name='Site' /><Value Type='Text'>"
                        + _siteUrl + "</Value></Eq></And></Where><RowLimit>50</RowLimit></Query></View>"
                    };

                    ListItemCollection listItems = _ExternalUsersReportList.GetItems(camlQuery);
                    _ctxExtUsers.Load(listItems);
                    _ctxExtUsers.ExecuteQuery();

                    itemPosition = listItems.ListItemCollectionPosition;

                    foreach (ListItem listItem in listItems)
                    {
                        //WriteVerbose(listItem["Owners"]);
                        _externalUserExists = true;
                        break;
                    }

                    if (itemPosition == null)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "External User exists check {0}", _ExpiredExternalUser);
            }

            return _externalUserExists;
        }

        private string RemoveExternalUser(string _siteUrl, string _ExtUser)
        {

            string _loginName = "";
            string _loginInfo = "";

            try
            {
                using var ctx = this.ClientContext.Clone(_siteUrl);
                Web _web = ctx.Web;

                UserCollection _users = _web.SiteUsers;

                ctx.Load(_web);
                ctx.Load(_users);
                ctx.ExecuteQuery();

                foreach (User _user in _users)
                {
                    if (_user.LoginName.ToLower().IndexOf(_ExtUser.ToLower()) > -1)
                    {
                        _loginName = _user.LoginName;
                        _loginInfo = _loginName;
                        GroupCollection _groups = _user.Groups;
                        ctx.Load(_groups);
                        ctx.ExecuteQuery();

                        foreach (Group _group in _groups)
                        {
                            _loginInfo += "+" + _group.Title;
                        }
                        break;
                    }
                }

                //Remove user from site collection
                _web.SiteUsers.RemoveByLoginName(_loginName);
                _web.Update();
                ctx.ExecuteQuery();
            }
            catch (Exception e)
            {
                LogError(e, "Failed in RemoveExternalUser {0} {1}", _siteUrl, _ExtUser);
            }

            return _loginInfo;
        }

        /// <summary>
        /// Process Expired External Users
        /// </summary>
        /// <param name="_extCtx"></param>
        /// <param name="externalUserListName"></param>
        private void Process_ExternalUserCertification(ClientContext _extCtx, string externalUserListName)
        {
            try
            {
                List _extList = _extCtx.Web.Lists.GetByTitle(externalUserListName);

                _extCtx.Load(_extList);
                _extCtx.ExecuteQuery();
                ListItemCollectionPosition itemPosition = null;

                while (true)
                {
                    CamlQuery EXTcamlQuery = new CamlQuery
                    {
                        ListItemCollectionPosition = itemPosition,
                        ViewXml = @"<View><Query>
                                    <Where><Eq>
                                            <FieldRef Name='Approve' /><Value Type='Boolean'>FALSE</Value>
                                    </Eq></Where>                                            
                                    <GroupBy> 
                                        <FieldRef Name='Site' /> 
                                        </GroupBy>                                           
                                    <ViewFields>
                                        <FieldRef Name='Title'/>
                                        <FieldRef Name='Owners'/>
                                        <FieldRef Name='Approve'/>
                                        <FieldRef Name='External_x0020_User'/>
                                        <FieldRef Name='Site_x0020_Collection_x0020_Admi'/>
                                        <FieldRef Name='User_x0020_Access'/>
                                        <FieldRef Name='Site'/>
                                    </ViewFields>
                                    <RowLimit>50</RowLimit>
                                </Query></View>"
                    };
                    ListItemCollection listItems = _extList.GetItems(EXTcamlQuery);
                    _extCtx.Load(listItems);
                    _extCtx.ExecuteQuery();
                    itemPosition = listItems.ListItemCollectionPosition;

                    List<string> _SiteExternalUsersList = new List<string> { };
                    FieldUserValue[] _tmpOwners = null;
                    FieldUserValue[] _tmpSCAs = null;

                    string _tmpSite = "";
                    int _countItems = 0;

                    LogVerbose("Current list item count {0}", listItems.Count);
                    foreach (ListItem listItem in listItems)
                    {
                        LogVerbose(">>++>> " + listItem["Site"].ToString());
                        _countItems++;

                        if (true)
                        {
                            if (string.IsNullOrWhiteSpace(_tmpSite))
                            {
                                //start first batch
                                LogVerbose("start first batch ...");
                                _tmpSite = listItem["Site"].ToString();

                            }

                            if ((listItem["Site"].ToString() != _tmpSite) && (_countItems != listItems.Count))
                            {
                                //Process previous batch
                                ProcessSiteExternalUsersCertification(_extCtx, _SiteExternalUsersList, _tmpSite, _tmpOwners, _tmpSCAs);
                                //Reset for new batch
                                LogVerbose("Reset for new batch...");
                                _tmpSite = listItem["Site"].ToString();
                                _SiteExternalUsersList.Clear();
                            }
                            else if (_countItems == listItems.Count)
                            {
                                //last item is in new batch
                                if (listItem["Site"].ToString() != _tmpSite)
                                {
                                    //Process previous batch
                                    ProcessSiteExternalUsersCertification(_extCtx, _SiteExternalUsersList, _tmpSite, _tmpOwners, _tmpSCAs);
                                    //Reset for new batch
                                    LogVerbose("Reset for new batch...");
                                    _tmpSite = listItem["Site"].ToString();
                                    _SiteExternalUsersList.Clear();

                                    //Process last item batch
                                    _SiteExternalUsersList.Add(listItem["Title"].ToString() + ", " + listItem["External_x0020_User"].ToString() + "," + listItem["User_x0020_Access"]);
                                    _tmpOwners = (FieldUserValue[])listItem["Owners"];
                                    _tmpSCAs = (FieldUserValue[])listItem["Site_x0020_Collection_x0020_Admi"];
                                    ProcessSiteExternalUsersCertification(_extCtx, _SiteExternalUsersList, _tmpSite, _tmpOwners, _tmpSCAs);

                                }
                                else
                                {
                                    _SiteExternalUsersList.Add(listItem["Title"].ToString() + ", " + listItem["External_x0020_User"].ToString() + "," + listItem["User_x0020_Access"]);
                                    _tmpOwners = (FieldUserValue[])listItem["Owners"];
                                    _tmpSCAs = (FieldUserValue[])listItem["Site_x0020_Collection_x0020_Admi"];

                                    //process last batch
                                    ProcessSiteExternalUsersCertification(_extCtx, _SiteExternalUsersList, _tmpSite, _tmpOwners, _tmpSCAs);
                                    //Reset for new batch
                                    LogVerbose("Reset for new batch...");
                                    _tmpSite = listItem["Site"].ToString();
                                    _SiteExternalUsersList.Clear();
                                }
                            }


                            _SiteExternalUsersList.Add(listItem["Title"].ToString() + ", " + listItem["External_x0020_User"].ToString() + "," + listItem["User_x0020_Access"]);
                            _tmpOwners = (FieldUserValue[])listItem["Owners"];
                            _tmpSCAs = (FieldUserValue[])listItem["Site_x0020_Collection_x0020_Admi"];
                        }

                    }

                    if (itemPosition == null)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                WriteConsole($"Failed process external user {e}");
                LogError(e, "Failed in process_ExternalUserCertification");
            }
        }

        private void RemoveSiteExternalUsers(List<string> _ExternalUsersList, string _site)
        {
            using var _ctxTarget = this.ClientContext.Clone(_site);
            WriteConsole("Url: " + _ctxTarget.Url);
            foreach (string _extUser in _ExternalUsersList)
            {
                string[] _extuserArray = _extUser.Split(new char[] { ',' });
                LogVerbose($"{_extuserArray[0]} -- {_extuserArray[1]}");
            }
        }

        private void ProcessSiteExternalUsersCertification(ClientContext _ctxExtUsers, List<string> _ExternalUsersList, string _site, FieldUserValue[] _owners, FieldUserValue[] _SCAs)
        {
            RemoveSiteExternalUsers(_ExternalUsersList, _site);

            using (var _ctxTarget = ClientContext.Clone(_site))
            {
                LogVerbose("Url: " + _ctxTarget.Url);

                foreach (string _extUser in _ExternalUsersList)
                {
                    string[] _extuserArray = _extUser.Split(new char[] { ',' });
                    LogVerbose("External User 1:{0} 2:{1}", _extuserArray[0], _extuserArray[1]);
                }
            }

            string _extUsersList = "";
            foreach (string _extUser in _ExternalUsersList)
            {
                LogVerbose(_extUser);
                _extUsersList += _extUser + "\n";
            }


            var _EmailNotificationList = _ctxExtUsers.Web.Lists.GetByTitle("Email Notification");
            _ctxExtUsers.Load(_EmailNotificationList);
            _ctxExtUsers.ExecuteQuery();
            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            ListItem newItem = _EmailNotificationList.AddItem(itemCreateInfo);
            newItem["Site"] = _site.Trim().ToLower();
            newItem["Owners"] = _owners;
            newItem["External_x0020_Users"] = _extUsersList;
            newItem["Site_x0020_Collection_x0020_Admi"] = _SCAs;
            newItem["Notice"] = "Second";

            newItem.Update();
            _ctxExtUsers.ExecuteQuery();
        }

        /// <summary>
        /// Retrieves a list connection to test connectivity
        /// </summary>
        /// <param name="reportContext"></param>
        /// <param name="siteListName">The display name of the external site list</param>
        private void TestSiteQueries(ClientContext reportContext, string siteListName)
        {
            List _listSites = reportContext.Web.Lists.GetByTitle(siteListName);

            reportContext.Load(_listSites, lc => lc.ItemCount);
            reportContext.ExecuteQuery();
            LogVerbose("The list {0} found total item count {1}", siteListName, _listSites.ItemCount);
        }

        /// <summary>
        /// return the root site's url
        /// </summary>
        /// <param name="_siteUrl"></param>
        /// <returns></returns>
        private string GetSiteCollectionRootUrl(string _siteUrl)
        {
            string[] _tmpSiteUrl = _siteUrl.Replace($"{TenantHostAuthority}sharepoint.com/sites/", "").Split(new char[] { '/' });
            // Determine site's region
            string _siteCollectionUrl = $"{TenantHostAuthority}sharepoint.com/sites/" + _tmpSiteUrl[0];
            return _siteCollectionUrl;

        }

        /// <summary>
        /// return the site's owners
        /// </summary>
        /// <param name="_siteUrl"></param>
        /// <returns></returns>
        private string GetOwners(string _siteUrl)
        {
            string _ownerStr = "";

            try
            {
                using var ctx = this.ClientContext.Clone(_siteUrl);
                Web _web = ctx.Web;
                ctx.Load(_web);
                ctx.ExecuteQuery();

                try
                {
                    Group _oGroup = _web.AssociatedOwnerGroup;
                    UserCollection _owners = _oGroup.Users;

                    ctx.Load(_oGroup);
                    ctx.Load(_owners);
                    ctx.ExecuteQuery();

                    foreach (User _user in _owners)
                    {
                        if (_user.LoginName.ToLower().IndexOf("system") < 0)
                        {
                            _ownerStr += RemoveClaimIdentifier(_user.LoginName) + ";";
                        }
                    }
                }
                catch
                {
                    User _siteOwner = ctx.Site.Owner;
                    ctx.Load(_siteOwner);
                    ctx.ExecuteQuery();
                    _ownerStr = _siteOwner.LoginName + ";";

                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in GetOwners {0}", _siteUrl);
            }

            return _ownerStr;
        }
    }
}
