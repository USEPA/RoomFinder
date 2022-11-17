using CommandLine;
using EPA.Office365;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using EPA.SharePoint.SysConsole.Models.Governance;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEPASetEveryoneGroup", HelpText = "Will search for the EVeryone group and replace it with the UG EPA All Employees group")]
    public class ScanEPASetEveryoneGroupOptions : TenantCommandOptions
    {
        [Option("process-eg", Required = false)]
        public bool ProcessEveryoneGroup { get; set; }

        [Option("process-odfb", Required = false)]
        public bool ProcessOneDrive { get; set; }

        [Option("process-shared", Required = false)]
        public bool ShouldProcessSharedWithEveryoneFolder { get; set; }

        /// <summary>
        /// If True process the everyone group
        /// </summary>
        [Option("enumerate-items", Required = false)]
        public bool EnumerateListItems { get; set; }

        [Option("site-urls", Required = false, SetName = "SiteSearch", Separator = ';')]
        public IEnumerable<string> SiteUrls { get; set; }

        [Option("everyone-groups", Required = false, Separator = ';')]
        public IEnumerable<string> EveryoneGroups { get; set; }
    }


    public static class ScanEPASetEveryoneGroupExtensions
    {
        public static int RunGenerateAndReturnExitCode(this ScanEPASetEveryoneGroupOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPASetEveryoneGroup(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// This will search for the EVeryone group and replace it with the UG EPA All Employees group
    /// </summary>
    /// <remarks>
    ///     ScanEPASetEveryoneGroup --site-urls ["https://<tenant>.sharepoint.com/sites/<sitename>"]
    ///     ScanEPASetEveryoneGroup --process-eg --process-shared
    /// </remarks>
    public class ScanEPASetEveryoneGroup : BaseSpoTenantCommand<ScanEPASetEveryoneGroupOptions>
    {
        public ScanEPASetEveryoneGroup(ScanEPASetEveryoneGroupOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private Variables

        /// <summary>
        /// Collections of sites with results
        /// </summary>
        private List<EPASearchGroupModels> _siteActionLog { get; set; }

        /// <summary>
        ///  UG-EPA-Employees-All
        /// </summary>
        private string UgEveryoneClaimIdentifier { get; set; } = ConstantsTenant.EveryoneGroupTenantId;

        private string TenantUrl { get; set; }

        private string MySiteTenantUrl { get; set; }

        private List<string> EveryoneGroups { get; set; } = new List<string>();

        #endregion

        public override void OnInit()
        {
            TenantUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            _siteActionLog = new List<EPASearchGroupModels>();
            var EveryoneUGGroupName = Settings.Commands.AzureADEveryoneGroup;


            LogVerbose("Retreiving Tenant details and Realm...");

            try
            {
                TenantContext.EnsureProperties(tssp => tssp.RootSiteUrl,
                    tssp => tssp.DefaultLinkPermission,
                    tssp => tssp.DefaultSharingLinkType,
                    tssp => tssp.RequireAcceptingAccountMatchInvitedAccount,
                    tssp => tssp.ShowAllUsersClaim,
                    tssp => tssp.ShowEveryoneClaim,
                    tssp => tssp.ShowEveryoneExceptExternalUsersClaim,
                    tssp => tssp.UseFindPeopleInPeoplePicker);
                TenantUrl = TenantContext.RootSiteUrl.EnsureTrailingSlashLowered();
                MySiteTenantUrl = TenantUrl.Replace(".sharepoint.com", "-my.sharepoint.com");

                if (Opts.EveryoneGroups == null || !Opts.EveryoneGroups.Any())
                {
                    // Set the Auth Realm for the Tenant Web Context
                    var siteRequestUrl = $"{TenantUrl}sites/SiteRequest";
                    using var tenantWeb = this.ClientContext.Clone(siteRequestUrl);
                    try
                    {
                        var _users = tenantWeb.LoadQuery(tenantWeb.Web.SiteUsers
                            .Where(wu => wu.LoginName == EveryoneUGGroupName || wu.Title == EveryoneUGGroupName)
                            .Include(sctx => sctx.LoginName, sctx => sctx.Title));
                        tenantWeb.ExecuteQueryRetry();
                        foreach (var _user in _users)
                        {
                            LogVerbose("Found {0} with identity {1}", _user.LoginName, _user.Title);
                            UgEveryoneClaimIdentifier = _user.LoginName;
                        }
                    }
                    catch (Exception e)
                    {
                        LogWarning("Failed to retreive users {0}", e.Message);
                    }

                    var webAuthRealm = tenantWeb.Web.GetAuthenticationRealm();
                    EveryoneGroups.AddRange(new string[]
                    {
                            "everyone except external users",
                            "c:0(.s|true", // everyone claim
                            "c:0!.s|windows", // windows claim
                            "everyone", // everyone title
                            $"c:0-.f|rolemanager|spo-grid-all-users/{webAuthRealm}",
                            "c:0-.f|rolemanager|spo-grid-all-users/d34dcf34-d61d-47ba-b73b-c9f8040e6b6e", // everyone except external users
                            "c:0-.f|rolemanager|spo-grid-all-users/a43df56a-6ea4-44a4-86e2-ac25e0013c5b", // everyone except external users - migration
                            ConstantsTenant.EveryoneGroupTenantId
                    });
                }
                else
                {
                    EveryoneGroups = Opts.EveryoneGroups.ToList();
                }



                LogVerbose("Retreiving sites...");
                var sites = new List<CollectionModel>();
                if (Opts.SiteUrls == null || !Opts.SiteUrls.Any())
                {
                    if (!Opts.ProcessOneDrive)
                    {
                        var spoSites = GetSiteCollections();
                        sites = spoSites.OfType<CollectionModel>().ToList();
                    }
                    else
                    {
                        var odfbSites = GetOneDriveSiteCollections(MySiteTenantUrl, true);
                        sites = odfbSites.OfType<CollectionModel>().ToList();

                    }
                }
                else
                {
                    sites.AddRange(Opts.SiteUrls.Select(s => new CollectionModel() { Url = s.Trim() }));
                }


                LogVerbose($"Processing ##{sites.Count} sites...");
                foreach (var siteCollection in sites)
                {
                    try
                    {
                        var _siteUrl = siteCollection.Url;
                        var _totalWebs = siteCollection.WebsCount;
                        LogVerbose($"Processing {_siteUrl}");

                        SetSiteAdmin(_siteUrl, CurrentUserName, true);

                        ProcessSiteCollection(_siteUrl, true);

                        SetSiteAdmin(_siteUrl, CurrentUserName);
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed in the processing of all site collections {0}", e.Message);
                    }
                }


                if (Opts.ProcessEveryoneGroup)
                {
                    LogVerbose($"Processing ##{sites.Count} sites into everyone report...");
                    AddRecordsToList();
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in ExecuteCmdlet {0}", e.Message);
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// Queries the collection of [everyone] groups
        /// </summary>
        /// <param name="_principal"></param>
        /// <returns></returns>
        private bool IsEveryoneInPrincipal(Principal _principal)
        {
            if (EveryoneGroups.Any(eg =>
                    _principal.Title.Equals(eg, StringComparison.CurrentCultureIgnoreCase)
                    || _principal.LoginName.Equals(eg, StringComparison.CurrentCultureIgnoreCase)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Process Site and recursively call with subweb URLs
        /// </summary>
        /// <param name="_siteUrl"></param>
        /// <param name="isSiteCollection"></param>
        private void ProcessSiteCollection(string _siteUrl, bool isSiteCollection = false)
        {
            try
            {
                var rootSiteIndex = _siteUrl.ToLower().IndexOf(TenantUrl);
                var rootMySiteIndex = _siteUrl.ToLower().IndexOf(MySiteTenantUrl);
                LogVerbose($"Processing Web URL {_siteUrl}");

                if (rootSiteIndex > -1 || rootMySiteIndex > -1)
                {

                    using var ctx = this.ClientContext.Clone(_siteUrl);
                    ctx.Load(ctx.Web, lssp => lssp.Id, wspp => wspp.ServerRelativeUrl, wspp => wspp.Title, spp => spp.HasUniqueRoleAssignments, ctxw => ctxw.Url);
                    ctx.ExecuteQueryRetry();

                    // Process site
                    ProcessSite(ctx, (rootMySiteIndex > -1), isSiteCollection);

                    //Process subsites
                    var _webs = ctx.LoadQuery(ctx.Web.Webs.Include(ctxw => ctxw.Url));
                    ctx.ExecuteQueryRetry();

                    if (_webs.Any())
                    {
                        LogVerbose("Site {0} has webs {1}", _siteUrl, _webs.Count());

                        foreach (Web _inWeb in _webs)
                        {
                            var _webUrl = _inWeb.Url;
                            ProcessSiteCollection(_webUrl);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processSiteCollection {0}", e.Message);
            }
        }


        private void ProcessSite(ClientContext ctx, bool IsMySite = false, bool isSiteCollection = false)
        {
            var _web = ctx.Web;
            var _siteUrl = _web.Url;
            var siteType = GetRegionSiteType(_siteUrl);
            var siteLog = new EPASearchGroupModels()
            {
                Title = ctx.Web.Title,
                URL = ctx.Web.Url,
                TrailingURL = ctx.Web.Url.EnsureTrailingSlashLowered(),
                HasUniquePerms = ctx.Web.HasUniqueRoleAssignments,
                ActivityLog = new List<string>(),
                IsSiteCollection = isSiteCollection,
                Region = siteType.Region,
                SiteType = siteType.SiteType
            };

            // IF the site does not have unique permissions then we will skip processing it for unique permissions
            if (siteLog.HasUniquePerms)
            {
                if (Opts.ShouldProcessSharedWithEveryoneFolder)
                {
                    try
                    {
                        ProcessSharedWithEveryoneFolder(ctx);
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed in processSite processSharedWithEveryoneFolder=>{0}", e.Message);
                    }
                }

                try
                {
                    /* Process Site Owner */
                    try
                    {
                        siteLog.Admins = new List<OfficeDevPnP.Core.Entities.UserEntity>();

                        var admins = ctx.Web.GetAdministrators();
                        siteLog.Admins.AddRange(admins);
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed to retrieve site owners {0}", _web.Url);
                    }


                    // ********** Get site owners
                    try
                    {
                        ctx.Load(ctx.Site, ctsx => ctsx.Owner);
                        ctx.ExecuteQueryRetry();

                        siteLog.SiteOwner = ctx.Site.Owner.Email;
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed in Site.Owner=>{0}", e.Message);
                    }

                    if (!IsMySite)
                    {
                        LogVerbose($"Retrieving Associated User Groups for {_siteUrl}");
                        try
                        {
                            siteLog.AssociatedOwnerGroupMember = new List<string>();

                            var _owners = ctx.LoadQuery(ctx.Web.AssociatedOwnerGroup.Users.Include(ur => ur.LoginName, utx => utx.Email, utx => utx.Id, utcx => utcx.Title, utcx => utcx.PrincipalType));
                            ctx.ExecuteQueryRetry();

                            var _filteredUsers = _owners.Where(v => IsEveryoneInPrincipal(v));
                            LogVerbose($"Users: Owners:##{_owners.Count()} Filtered:##{_filteredUsers.Count()}");
                            if (_filteredUsers != null && _filteredUsers.Any())
                            {
                                foreach (User _user in _filteredUsers)
                                {
                                    LogVerbose("Found {0} with identity {1}", _user.LoginName, _user.Title);
                                    siteLog.AssociatedOwnerGroupMember.Add(_user.Email);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(e, "Failed AssociatedOwnerGroup=>{0}", e.Message);
                        }

                        try
                        {
                            siteLog.AssociatedMemberGroupMember = new List<string>();

                            var _members = ctx.LoadQuery(ctx.Web.AssociatedMemberGroup.Users.Include(ur => ur.LoginName, utx => utx.Email, utx => utx.Id, utcx => utcx.Title, utcx => utcx.PrincipalType));
                            ctx.ExecuteQueryRetry();

                            var _filteredUsers = _members.Where(v => IsEveryoneInPrincipal(v));
                            LogVerbose($"Users: Members:##{_members.Count()} Filtered:##{_filteredUsers.Count()}");
                            foreach (var _user in _filteredUsers)
                            {
                                LogVerbose("Found {0} with identity {1}", _user.LoginName, _user.Title);
                                siteLog.AssociatedMemberGroupMember.Add(_user.Email);
                            }
                        }
                        catch (Exception e)
                        {
                            LogWarning("Failed AssociatedMemberGroup=>{0}", e.Message);
                        }

                        try
                        {
                            siteLog.AssociatedVisitorGroupMember = new List<string>();

                            var _visitors = ctx.LoadQuery(ctx.Web.AssociatedVisitorGroup.Users.Include(ur => ur.LoginName, utx => utx.Email, utx => utx.Id, utcx => utcx.Title, utcx => utcx.PrincipalType));
                            ctx.ExecuteQueryRetry();

                            var _filteredUsers = _visitors.Where(v => IsEveryoneInPrincipal(v));
                            LogVerbose($"Users: Visitors:##{_visitors.Count()} Filtered:##{_filteredUsers.Count()}");
                            foreach (var _user in _filteredUsers)
                            {
                                LogVerbose("Found {0} with identity {1}", _user.LoginName, _user.Title);
                                siteLog.AssociatedVisitorGroupMember.Add(_user.Email);
                            }
                        }
                        catch (Exception e)
                        {
                            LogWarning("Failed AssociatedVisitorGroup=>{0}", e.Message);
                        }
                    }


                    // ********** Process Site user
                    try
                    {
                        siteLog.Users = new List<SPPrincipalModel>();
                        ctx.Web.EnsureProperties(spp => spp.SiteUsers.Include(stx => stx.Id, stx => stx.IsHiddenInUI, stx => stx.Title, stx => stx.LoginName, stx => stx.PrincipalType));

                        var temporaryUsers = new List<User>();
                        var filteredUsers = ctx.Web.SiteUsers.Where(w => IsEveryoneInPrincipal(w));
                        LogVerbose($"Site {_siteUrl} SiteUsers:##{ctx.Web.SiteUsers.Count()} Filtered:##{filteredUsers.Count()}");
                        foreach (User _user in filteredUsers)
                        {
                            siteLog.Users.Add(new SPPrincipalModel()
                            {
                                Id = _user.Id,
                                IsHiddenInUI = _user.IsHiddenInUI,
                                Title = _user.Title,
                                LoginName = _user.LoginName,
                                PrincipalType = _user.PrincipalType
                            });

                            temporaryUsers.Add(_user);
                        }

                        // Will remove user from the SiteUsers
                        if (Opts.ProcessEveryoneGroup)
                        {
                            LogVerbose($"Removing Users:##{temporaryUsers.Count()}");
                            foreach (var _user in temporaryUsers)
                            {
                                var removed = RemoveUserFromSiteUsers(ctx, _user);

                                siteLog.ActivityLog.Add("Removed the everyone group from siteusers");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed processUsers enumerating site users");
                    }

                    // ********** Process Site groups
                    try
                    {
                        siteLog.Groups = new List<SPGroupPrincipalModel>();
                        ctx.Web.EnsureProperties(sapp => sapp.SiteGroups.Include(ussp => ussp.Id, ussp => ussp.Title, ussp => ussp.LoginName, ussp => ussp.Users));

                        var filteredGroups = ctx.Web.SiteGroups;
                        foreach (Group _group in filteredGroups)
                        {
                            if (IsEveryoneInPrincipal(_group))
                            {
                                LogVerbose($"Found {_group.Title} in SiteGroups.");
                                siteLog.Groups.Add(new SPGroupPrincipalModel()
                                {
                                    GroupId = _group.Id,
                                    Id = _group.Id,
                                    Title = _group.Title,
                                    LoginName = _group.LoginName,
                                    PrincipalType = _group.PrincipalType
                                });
                            }

                            var _temporaryUsers = new List<User>();
                            var _users = _group.Users.Where(_user => IsEveryoneInPrincipal(_user));
                            LogVerbose($"Group {_group.Title} has Users:##{_group.Users.Count()} Filtered:##{_users.Count()}");
                            foreach (User _xUser in _users)
                            {
                                siteLog.Groups.Add(new SPGroupPrincipalModel()
                                {
                                    GroupId = _group.Id,
                                    Id = _xUser.Id,
                                    IsHiddenInUI = _xUser.IsHiddenInUI,
                                    Title = _xUser.Title,
                                    LoginName = _xUser.LoginName,
                                    PrincipalType = _xUser.PrincipalType
                                });

                                _temporaryUsers.Add(_xUser);
                            }


                            if (Opts.ProcessEveryoneGroup && _temporaryUsers.Any())
                            {
                                foreach (var _xUser in _temporaryUsers)
                                {
                                    if (RemoveUserFromGroup(ctx, _group, _xUser))
                                    {
                                        siteLog.ActivityLog.Add("Removed the everyone group from " + _group.Title);
                                    }

                                    if (AddUGEveryoneToGroup(ctx, _group))
                                    {
                                        siteLog.ActivityLog.Add("Added UG-EPA-Employees-All to " + _group.Title);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed in process Groups");
                    }


                    // ********** Process Lists
                    siteLog.SkippedLists = new List<EPAListSkippedDefinition>();
                    siteLog.Lists = new List<EPAListDefinition>();

                    ListCollection _lists = ctx.Web.Lists;
                    ctx.Load(_lists, spp => spp.Include(
                        sppi => sppi.Id,
                        sppi => sppi.Title,
                        sppi => sppi.RootFolder.ServerRelativeUrl,
                        sppi => sppi.HasUniqueRoleAssignments,
                        sppi => sppi.Hidden,
                        sppi => sppi.IsSystemList,
                        sppi => sppi.IsPrivate,
                        sppi => sppi.IsApplicationList,
                        sppi => sppi.IsCatalog,
                        sppi => sppi.IsSiteAssetsLibrary,
                        spix => spix.BaseTemplate,
                        spix => spix.BaseType,
                        spix => spix.ItemCount,
                        spix => spix.LastItemUserModifiedDate,
                        spix => spix.LastItemModifiedDate,
                        spix => spix.Created,
                        spix => spix.SchemaXml));
                    ctx.ExecuteQueryRetry();

                    // Restrict to natural lists or custom lists
                    foreach (List _list in _lists.Where(sppi => sppi.HasUniqueRoleAssignments))
                    {
                        // ********** Process List
                        siteLog = ProcessList(ctx, _web, _list, siteLog);
                    }
                }
                catch (Exception e)
                {
                    LogError(e, "Failed to processSite {0}", e.Message);
                }

            }

            // add site to collection
            _siteActionLog.Add(siteLog);
        }


        private void ProcessSharedWithEveryoneFolder(ClientContext ctx)
        {
            Folder _folder = null;

            try
            {
                _folder = ctx.Web.GetFolderByServerRelativeUrl(ctx.Web.ServerRelativeUrl + "/Documents/Shared with Everyone/");
                ctx.Load(_folder);
                ctx.ExecuteQueryRetry();

                ListItem _item = _folder.ListItemAllFields;
                ctx.Load(_item, s => s["Title"], s => s["FileLeafRef"], s => s["Modified"], s => s["Editor"]);
                ctx.ExecuteQueryRetry();

                try
                {
                    //Rename folder
                    _item["Title"] = "Shared with EPA Employees Only";
                    _item["FileLeafRef"] = "Shared with EPA Employees Only";
                    _item.SystemUpdate();
                    ctx.ExecuteQueryRetry();
                }
                catch (Exception e)
                {
                    LogError(e, "Failed in processSharedWithEveryoneFolder Update Title=>{0}", e.Message);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, $"Failed to retreive Shared With Everyone folder {ex.Message}");
            }


            try
            {
                _folder = ctx.Web.GetFolderByServerRelativeUrl(ctx.Web.ServerRelativeUrl + "/Documents/Shared with EPA Employees Only/");
                ctx.Load(_folder);
                ctx.ExecuteQueryRetry();


                ListItem _item = _folder.ListItemAllFields;
                ctx.Load(_item, s => s.HasUniqueRoleAssignments, s => s["Title"], s => s["FileLeafRef"], s => s["Modified"], s => s["Editor"]);
                ctx.Load(_item, s => s.RoleAssignments.Include(rct => rct.Member));
                ctx.ExecuteQueryRetry();


                FieldUserValue _lastEditor = null;
                User _userLastEditor = null;
                DateTime _lastModified = DateTime.Now;

                try
                {
                    _lastModified = (DateTime)_item["Modified"];
                    _lastEditor = (FieldUserValue)_item["Editor"];
                    _userLastEditor = ctx.Web.GetUserById(_lastEditor.LookupId);
                    ctx.Load(_userLastEditor);
                    ctx.ExecuteQueryRetry();
                }
                catch (Exception e)
                {
                    LogError(e, "Failed in processSharedWithEveryoneFolder _lastEditor{0}", e.Message);
                }

                // Add epa employes group  
                if (_item.HasUniqueRoleAssignments == false)
                {
                    try
                    {
                        _item.BreakRoleInheritance(true, false);
                        _item.SystemUpdate();
                        _item.Context.ExecuteQueryRetry();
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed in addEpaEmployeeGroup Break Inheritance=>{0}", e.Message);
                    }
                }


                // If exisit: Replace "everyone except external users" group with the "ug-epa-employees-only" group
                var roleAssignment = new List<RoleDefinitionBindingCollection>();
                var filteredRoles = _item.RoleAssignments.Where(r => IsEveryoneInPrincipal(r.Member));
                foreach (RoleAssignment ra in filteredRoles)
                {
                    Principal _user = ra.Member;
                    LogVerbose(_user.Title);
                    RoleDefinitionBindingCollection rdc = ra.RoleDefinitionBindings;
                    ctx.Load(rdc);
                    ctx.ExecuteQueryRetry();

                    roleAssignment.Add(rdc);

                    // remove everyone except external users
                    try
                    {
                        _item.RoleAssignments.GetByPrincipal(_user).DeleteObject();
                        _item.SystemUpdate();
                        _item.Context.ExecuteQueryRetry();
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed in removeEveryoneEEUgroup {0}", e.Message);
                    }
                }

                foreach (var rdc in roleAssignment)
                {
                    try
                    {
                        Principal _newP = ctx.Web.EnsureUser(UgEveryoneClaimIdentifier);
                        ctx.Load(_newP);
                        ctx.ExecuteQueryRetry();

                        _item.RoleAssignments.Add(_newP, rdc);
                        _item.SystemUpdate();
                        _item.Context.ExecuteQueryRetry();

                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed in addEpaEmployeeGroup EnsureUser=>{0}", e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processSharedWithEveryoneFolder {0}", e.Message);
            }
        }


        private EPASearchGroupModels ProcessList(ClientContext ctx, Web _web, List _list, EPASearchGroupModels siteLog)
        {

            try
            {
                var serverRelativeUrl = _list.RootFolder.ServerRelativeUrl;
                var listTemplateType = _list.GetListTemplateType();
                var listHidden = _list.Hidden;

                LogVerbose($"List {serverRelativeUrl} Hidden:{listHidden} Base:{_list.BaseTemplate} Template:{listTemplateType:f}");

                if ((listTemplateType == ListTemplateType.DocumentLibrary
                    || listTemplateType == ListTemplateType.HomePageLibrary
                    || listTemplateType == ListTemplateType.MySiteDocumentLibrary
                    || listTemplateType == ListTemplateType.PictureLibrary
                    || listTemplateType == ListTemplateType.WebPageLibrary
                    || listTemplateType == ListTemplateType.XMLForm))
                {

                    var listModel = new EPAListDefinition()
                    {
                        Id = _list.Id,
                        HasUniquePermission = _list.HasUniqueRoleAssignments,
                        ListName = _list.Title,
                        ServerRelativeUrl = _list.RootFolder.ServerRelativeUrl,
                        Hidden = listHidden,
                        IsSystemList = _list.IsSystemList,
                        IsPrivate = _list.IsPrivate,
                        IsApplicationList = _list.IsApplicationList,
                        IsCatalog = _list.IsCatalog,
                        IsSiteAssetsLibrary = _list.IsSiteAssetsLibrary,
                        BaseTemplate = _list.BaseTemplate,
                        ListTemplate = listTemplateType,
                        RoleBindings = new List<SPPrincipalModel>(),
                        ItemCount = _list.ItemCount,
                        Created = _list.Created,
                        LastItemModifiedDate = _list.LastItemModifiedDate,
                        LastItemUserModifiedDate = _list.LastItemUserModifiedDate
                    };

                    ctx.Load(_list, ctxl => ctxl.RoleAssignments.Include(ctml => ctml.Member));
                    ctx.ExecuteQueryRetry();

                    var _tmpRoles = new List<RoleAssignment>();
                    var filteredRoles = _list.RoleAssignments.Where(w => w.Member != null && IsEveryoneInPrincipal(w.Member));
                    LogVerbose($"List {listModel.ServerRelativeUrl} Permissions:##{_list.RoleAssignments.Count()} Filtered:##{filteredRoles.Count()}");
                    foreach (RoleAssignment _role in filteredRoles)
                    {
                        listModel.RoleBindings.Add(new SPPrincipalModel()
                        {
                            Id = _role.Member.Id,
                            Title = _role.Member.Title,
                            LoginName = _role.Member.LoginName,
                            PrincipalType = _role.Member.PrincipalType
                        });

                        _tmpRoles.Add(_role);
                    }

                    if (_tmpRoles.Any() && Opts.ProcessEveryoneGroup)
                    {
                        foreach (var _role in _tmpRoles)
                        {
                            // enumerate the permission level for this association
                            RoleDefinitionBindingCollection _roleBC = _role.RoleDefinitionBindings;
                            ctx.Load(_roleBC);
                            ctx.ExecuteQueryRetry();
                            foreach (var _locrole in _roleBC)
                            {
                                LogVerbose($"List {listModel.ServerRelativeUrl} -- Role {_role.Member.Title} -- Perm {_locrole.BasePermissions}");
                            }

                            // add new user/group
                            if (AddUGEveryoneGrouptoList(ctx, _list, _roleBC))
                            {
                                siteLog.ActivityLog.Add("Added UG-EPA-Employees-All to " + _list.Title);
                            }

                            // remove user/group's permission   
                            try
                            {
                                _role.DeleteObject();
                                _list.Update();
                                ctx.ExecuteQueryRetry();
                                siteLog.ActivityLog.Add("Removed the everyone group from " + _list.Title);
                            }
                            catch (Exception e)
                            {
                                LogError(e, "Failed to processList DeleteObject=>{0}", e.Message);
                            }
                        }
                    }

                    if (Opts.EnumerateListItems)
                    {

                        siteLog = ProcessListItems(ctx, _web, _list, siteLog, listModel);

                    }

                    if (!string.IsNullOrEmpty(_list.SchemaXml))
                    {
                        var author = string.Empty;
                        var xdoc = XDocument.Parse(_list.SchemaXml, LoadOptions.PreserveWhitespace);
                        var schemaXml = xdoc.Element("List");
                        if (schemaXml.Attribute("Author") != null)
                        {
                            var authorId = schemaXml.Attribute("Author").Value;

                            var listAuthor = ctx.Web.GetUserById(int.Parse(authorId));
                            ctx.Load(listAuthor);
                            ctx.ExecuteQueryRetry();

                            listModel.CreatedBy = new SPPrincipalModel()
                            {
                                Id = listAuthor.Id,
                                Title = listAuthor.Title,
                                LoginName = listAuthor.LoginName,
                                PrincipalType = listAuthor.PrincipalType
                            };
                        }
                    }

                    siteLog.Lists.Add(listModel);
                }
                else
                {

                    var skippedlistModel = new EPAListSkippedDefinition()
                    {
                        Id = _list.Id,
                        HasUniquePermission = _list.HasUniqueRoleAssignments,
                        ListName = _list.Title,
                        ServerRelativeUrl = _list.RootFolder.ServerRelativeUrl,
                        Hidden = _list.Hidden,
                        ListTemplate = listTemplateType,
                        ItemCount = _list.ItemCount,
                        Created = _list.Created
                    };

                    siteLog.SkippedLists.Add(skippedlistModel);
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed to processList {0}", e.Message);
            }

            return siteLog;
        }

        private EPASearchGroupModels ProcessListItems(ClientContext ctx, Web _web, List _list, EPASearchGroupModels siteLog, EPAListDefinition listModel)
        {
            try
            {
                listModel.ListItems = new List<EPAListItemDefinition>();


                var camlEnumeration = _list.SafeCamlClauseFromThreshold(1000);
                camlEnumeration.ForEach((caml) =>
                {
                    ListItemCollectionPosition itemPosition = null;
                    var camlView = CAML.ViewFields((new string[] { "ID", "Title" }).Select(s => CAML.FieldRef(s)).ToArray());
                    var camlWhere = (string.IsNullOrEmpty(caml)) ? string.Empty : CAML.Where(caml);
                    var camlQuery = new CamlQuery
                    {
                        ListItemCollectionPosition = itemPosition,
                        ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, camlWhere, camlView, string.Empty, 50)
                    };

                    do
                    {
                        ListItemCollection listItems = _list.GetItems(camlQuery);
                        ctx.Load(listItems, ctxi => ctxi.Include(ctxii => ctxii.HasUniqueRoleAssignments));
                        ctx.ExecuteQueryRetry();
                        camlQuery.ListItemCollectionPosition = listItems.ListItemCollectionPosition;

                        var filteredItems = listItems.Where(listItem => listItem.HasUniqueRoleAssignments);
                        LogVerbose($"List {listModel.ServerRelativeUrl} Items:##{listItems.Count()} Filtered:##{filteredItems.Count()}");
                        foreach (ListItem listItem in filteredItems)
                        {
                            var listItemTitle = listItem.RetrieveListItemValue("Title");
                            var listItemModel = new EPAListItemDefinition()
                            {
                                Id = listItem.Id,
                                HasUniquePermission = listItem.HasUniqueRoleAssignments,
                                Title = listItem.DisplayName
                            };

                            var _tmpRoleAssignment = new List<RoleAssignment>();

                            ctx.Load(listItem, ltx => ltx.RoleAssignments.Include(inc => inc.Member));
                            ctx.ExecuteQueryRetry();

                            var filteredRoleAssignments = listItem.RoleAssignments.Where(_role => _role.Member != null && IsEveryoneInPrincipal(_role.Member));
                            LogVerbose($"ListItem {listItemTitle} Permissions:##{listItem.RoleAssignments.Count()} Filtered:##{filteredRoleAssignments.Count()}");
                            foreach (RoleAssignment _role in filteredRoleAssignments)
                            {
                                _tmpRoleAssignment.Add(_role);

                                var minrole = new SPPrincipalModel()
                                {
                                    Id = _role.Member.Id,
                                    LoginName = _role.Member.LoginName,
                                    Title = _role.Member.Title,
                                    PrincipalType = _role.Member.PrincipalType
                                };
                                listItemModel.RoleBindings.Add(minrole);
                            }


                            if (_tmpRoleAssignment.Any() && Opts.ProcessEveryoneGroup)
                            {
                                foreach (var _role in _tmpRoleAssignment)
                                {
                                    ctx.Load(_role, ltx => ltx.RoleDefinitionBindings);
                                    ctx.ExecuteQueryRetry();

                                    // Add new user/group
                                    if (AddUGEveryoneGroupToListItem(ctx, _list, listItem, _role.RoleDefinitionBindings))
                                    {
                                        siteLog.ActivityLog.Add($"Added UG-EPA-Employees-All to ListItem {listItemTitle} in {_list.Title}");
                                    }

                                    //remove user/group's permission   
                                    try
                                    {
                                        _role.DeleteObject();
                                        listItem.Update();
                                        ctx.ExecuteQueryRetry();
                                        siteLog.ActivityLog.Add($"Removed the everyone group from ListItem {listItem.RetrieveListItemValue("Title")} in {_list.Title}");
                                    }
                                    catch (Exception e)
                                    {
                                        LogError(e, "Failed to processListItems Removing everyone group {0}", e.Message);
                                    }
                                }
                            }

                            listModel.ListItems.Add(listItemModel);
                        }
                    }
                    while (camlQuery.ListItemCollectionPosition != null);

                });
            }
            catch (Exception e)
            {
                LogError(e, $"Failed to processListItems {e.Message}");
            }

            return siteLog;
        }

        private bool AddUGEveryoneToGroup(ClientContext _ctx, Group _group)
        {
            var _groupTitle = _group.Title;
            try
            {
                if (ShouldProcess($"AddUGEveryoneToGroup({_groupTitle})"))
                {
                    UserCreationInformation userCreationInfo = new UserCreationInformation
                    {
                        LoginName = UgEveryoneClaimIdentifier
                    };

                    User oUser = _group.Users.Add(userCreationInfo);
                    _group.Update();
                    _ctx.ExecuteQueryRetry();
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e, $"Failed AddUGEveryoneToGroup {_groupTitle} with MSG {e.Message}");
            }
            return false;
        }


        private bool AddUGEveryoneGroupToListItem(ClientContext _ctx, List _list, ListItem _item, RoleDefinitionBindingCollection _roles)
        {
            try
            {
                if (ShouldProcess($">>> AddUGEveryoneGroupToListItem(ID:{_item.Id})"))
                {
                    User _user = _ctx.Web.EnsureUser(UgEveryoneClaimIdentifier);
                    _ctx.Load(_user);
                    _ctx.ExecuteQueryRetry();

                    _item.RoleAssignments.Add(_user, _roles);
                    _item.Update();
                    _ctx.ExecuteQueryRetry();
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e, $"Failed AddUGEveryoneGroupToListItem(ID:{_item.Id}) with MSG {e.Message}");
            }
            return false;
        }

        private bool AddUGEveryoneGrouptoList(ClientContext _ctx, List _list, RoleDefinitionBindingCollection _roles)
        {
            try
            {
                if (ShouldProcess($"AddUGEveryoneGrouptoList({_list.Title})"))
                {
                    User _user = _ctx.Web.EnsureUser(UgEveryoneClaimIdentifier);
                    _ctx.Load(_user);
                    _ctx.ExecuteQueryRetry();

                    _list.RoleAssignments.Add(_user, _roles);
                    _list.Update();
                    _ctx.ExecuteQueryRetry();
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e, $"Failed to AddUGEveryoneGrouptoList {e.Message}");
            }
            return false;
        }


        private void RemoveAndAddGroup(SecurableObject _item, Principal _principal, Principal _groupToAdd = null, RoleDefinitionBindingCollection _rdc = null)
        {
            if (_groupToAdd != null && _rdc == null)
            {
                throw new ArgumentNullException("_rdc", "A role definition must be supplied in order to ensure this principal permissions");
            }

            if (_item.HasUniqueRoleAssignments == false)
            {
                try
                {
                    _item.BreakRoleInheritance(true, false);
                    _item.Context.ExecuteQueryRetry();
                }
                catch (Exception e)
                {
                    LogError(e, "Failed to break role inheritance");
                }
            }

            try
            {
                _item.RemovePermissionLevelFromPrincipal(_principal, RoleType.None, true);
            }
            catch (Exception e)
            {
                LogError(e, "Failed in remove roup");
            }

            if (_groupToAdd != null)
            {
                try
                {
                    Web web = _item.GetAssociatedWeb();

                    foreach (var role in _rdc)
                    {
                        _item.AddPermissionLevelToPrincipal(_groupToAdd, role.Name, false);
                    }
                }
                catch (Exception e)
                {
                    LogError(e, "Faied in ensuring rolemanager sid");
                }
            }
        }

        private bool RemoveUserFromGroup(ClientContext _ctx, Group _group, User _user)
        {
            try
            {
                if (ShouldProcess($"RemoveUserFromGroup({_group.Title})"))
                {
                    _group.Users.Remove(_user);
                    _group.Update();
                    _ctx.ExecuteQueryRetry();
                }
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"Failed RemoveUserFromGroup {_group.Title} with msg {ex.Message}");
            }
            return false;
        }

        private bool RemoveUserFromSiteUsers(ClientContext _ctx, User _user)
        {
            try
            {
                if (ShouldProcess($"Removing {_user.Title} everyone group from site"))
                {
                    _ctx.Web.SiteUsers.Remove(_user);
                    _ctx.Web.Update();
                    _ctx.ExecuteQueryRetry();
                }
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"Failed RemoveUserFromSiteUsers {_user.Title} with msg {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Write the site logs to the SharePoint tracking list
        /// </summary>
        private void AddRecordsToList()
        {

            var url = $"{TenantUrl}sites/oei_custom/EveryoneGroupReport";
            using var sitecontext = this.ClientContext.Clone(url);
            var _SitesReportList = sitecontext.Web.GetListByTitle("SitesWithEveryone");
            sitecontext.Load(_SitesReportList);
            sitecontext.ExecuteQueryRetry();

            foreach (var siteactivity in _siteActionLog)
            {
                //create list item in SP report
                var _siteUrl = siteactivity.URL;
                var _sitelogs = siteactivity.ActivityLog;

                LogVerbose($"Adding >> _activityLog >> {_siteUrl}");

                try
                {
                    var _usersValues = new List<FieldUserValue>();

                    if (siteactivity?.AssociatedOwnerGroupMember.Any() == true)
                    {
                        foreach (var _userOwnerLoginName in siteactivity?.AssociatedOwnerGroupMember)
                        {
                            User _user = sitecontext.Web.EnsureUser(_userOwnerLoginName);
                            sitecontext.Load(_user);
                            sitecontext.ExecuteQueryRetry();

                            FieldUserValue _ufv = new FieldUserValue
                            {
                                LookupId = _user.Id
                            };
                            _usersValues.Add(_ufv);
                        }
                    }


                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem newItem = _SitesReportList.AddItem(itemCreateInfo);
                    newItem["Title"] = _siteUrl.Trim().ToLower();
                    newItem["Activity_x0020_Log"] = string.Join(Environment.NewLine, siteactivity.ActivityLog);

                    if (_usersValues.Any())
                    {
                        newItem["Site_x0020_Owners"] = _usersValues;
                    }

                    newItem.Update();
                    sitecontext.ExecuteQueryRetry();

                }
                catch (Exception e)
                {
                    LogError(e, $"Failed AddRecordtoList({_siteUrl}) with msg {e.Message}");
                }
            }
        }


    }
}
