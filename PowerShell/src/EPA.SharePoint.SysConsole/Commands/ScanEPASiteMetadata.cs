using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Framework.Provisioning;
using EPA.SharePoint.SysConsole.Models.Apps;
using EPA.SharePoint.SysConsole.Models.Governance;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using EPA.SharePoint.SysConsole.HttpServices;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEPASiteMetadata", HelpText = "parses site metadata into list items.")]
    public class ScanEPASiteMetadataOptions : TenantCommandOptions
    {
        [Option("process-perms", Required = false)]
        public bool ProcessPermissions { get; set; }
    }

    public static class ScanEPASiteMetadataOptionsExtension
    {
        /// <summary>
        /// Will execute the Scan EPA Site Metadata command, processing for governance
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this ScanEPASiteMetadataOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPASiteMetadata(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class ScanEPASiteMetadata : BaseSpoTenantCommand<ScanEPASiteMetadataOptions>
    {
        public ScanEPASiteMetadata(ScanEPASiteMetadataOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private Variables

        private string RootSiteUrl { get; set; }
        /// <summary>
        /// EPA All users Active Directory claim identifier
        /// </summary>
        private string UgEveryoneClaimIdentifier { get; set; }
        private List<Model_SiteRequestItem> SiteRequests { get; set; }
        private List<Model_MetadataItem> MissingMetadataSites { get; set; }
        private List<string> Exceptionurls { get; set; }

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

            // Add exceptions for non required governance
            Exceptionurls = new List<string>()
            {
                "_apps/",
                "_application/",
                "_applications/",
                "_custom/",
                "_custom2/",
                "_development/",
                "_external/",
                "test/",
                "-public.sharepoint",
                "-mysites.sharepoint",
                "-my.sharepoint",
                "sharepoint.com/sites/akpd", // skip SP Test Sites
                "sharepoint.com/sites/applications",
                "sharepoint.com/sites/dev",
                "sharepoint.com/sites/dropbox",
                "sharepoint.com/sites/d508",
                "sharepoint.com/sites/ediscovery",
                "sharepoint.com/sites/ezforms",
                "sharepoint.com/sites/i2i",
                "sharepoint.com/sites/mastertemplate",
                "sharepoint.com/sites/metadataplayground",
                "sharepoint.com/sites/myworkplace",
                "sharepoint.com/sites/pwa",
                "sharepoint.com/sites/siterequest",
                "sharepoint.com/sites/teatsearch",
                "sharepoint.com/sites/test",
                "sharepoint.com/sites/theme",
                "sharepoint.com/sites/training",
                "sharepoint.com/sites/usepa_apps",
                "sharepoint.com/portals/hub",
                "sharepoint.com/search",
                "-project/",
                "_project/",
                "-projects/",
                "_projects/",
                "sandbox/",
                "-stage/",
                "_stage/",
                "-trial/",
                "_trial/"
            };
        }

        public override int OnRun()
        {
            var sitemetadataurl = Settings.Commands.SPOSiteRequestUrl;
            var EveryoneUGGroupName = Settings.Commands.AzureADEveryoneGroup;

            SiteRequests = new List<Model_SiteRequestItem>();
            MissingMetadataSites = new List<Model_MetadataItem>();

            // Ensure we have the root site URL
            TenantContext.EnsureProperties(ct => ct.RootSiteUrl);
            RootSiteUrl = UrlPattern(TenantContext.RootSiteUrl);


            using var _ctxSiteRequest = TenantContext.Context.Clone(sitemetadataurl);
            Web _siteRequestWeb = _ctxSiteRequest.Web;
            _ctxSiteRequest.Load(_siteRequestWeb);
            _ctxSiteRequest.ExecuteQueryRetry();


            // get the missing metadata list for further examination
            List _listMissingMetadata = _siteRequestWeb.Lists.GetByTitle(Constants_SiteRequest.MissingMetadataListName);
            _siteRequestWeb.Context.Load(_listMissingMetadata);

            // get the site requests
            List _listSites = _siteRequestWeb.Lists.GetByTitle(Constants_SiteRequest.SiteRequestListName);
            _siteRequestWeb.Context.Load(_listSites);

            // Load into memory
            _siteRequestWeb.Context.ExecuteQueryRetry();




            // Initials array with current sites listed under the missing metadata section
            MissingMetadataSites = _ctxSiteRequest.InitMissingMetadataArray(_listMissingMetadata);
            LogVerbose("done initiating array...  count: {0}", MissingMetadataSites.Count);

            // Initalizes the array of site requests
            SiteRequests = _ctxSiteRequest.InitSiteRequestArray(_listSites);
            LogVerbose("done initiating array... count: {0}", SiteRequests.Count);


            // Enumerate thrugh all sites
            var sites = GetSiteCollections(true);


            // Lets exclude all sites that do not require a metadata list
            var governancesites = sites.Where(sw => !Exceptionurls.Any(eu => sw.Url.IndexOf(eu, StringComparison.CurrentCultureIgnoreCase) >= 0) && !sw.Url.Equals(RootSiteUrl, StringComparison.CurrentCultureIgnoreCase));
            foreach (var _site in governancesites)
            {
                var _siteUrl = UrlPattern(_site.Url);
                LogVerbose("Process Site Collection: {0}", _site);

                try
                {
                    // Set Site Admin
                    SetSiteAdmin(_siteUrl, this.CurrentUserName, true);

                    // Process Site Collection
                    ProcessSubSites(_listMissingMetadata, _siteUrl, _siteUrl, true);
                }
                catch (Exception e)
                {
                    LogWarning("Failed to process inside processSiteCollection {0}", e.Message);
                }
            }

            // Delete site no longer needed in the list
            var canBeRemovedCount = MissingMetadataSites.Count(ms => ms.CanBeRemoved);
            LogVerbose("---- Process delete..  detected count: {0}", canBeRemovedCount);

            try
            {
                foreach (var _missingSite in MissingMetadataSites.Where(ms => ms.CanBeRemoved))
                {
                    var msg = string.Format("DELETING MISSING METADATA SITE WITH ID: {0}", _missingSite.Id);
                    LogVerbose(msg);
                    if (ShouldProcess(msg))
                    {
                        // delete item from missing metadata list
                        try
                        {
                            var _dItem = _listMissingMetadata.GetItemById("" + _missingSite.Id);
                            _ctxSiteRequest.Load(_dItem);
                            _ctxSiteRequest.ExecuteQueryRetry();
                            _dItem.DeleteObject();
                            _ctxSiteRequest.ExecuteQueryRetry();
                        }
                        catch (Exception e)
                        {
                            LogWarning("Failed inside DeleteObject {0}", e.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogWarning("Failed inside Delete process {0}", e.Message);
                return -1;
            }

            return 1;
        }
        
        private void ProcessSubSites(List _webSiteMissingMetaList, string _webUrl, string _parentWebUrl, bool rootWeb = false)
        {
            var _siteUrl = UrlPattern(_webUrl);
            var _siteType = GetRegionSiteType(_siteUrl);
            if (_siteType.SiteType == "ADD-IN")
            {
                // This web is an ADD-IN Site Collection [skip the metadata processing]
                LogVerbose("Add-In: {0}; skip processing", _siteUrl);
                return;
            }

            using var ctx = TenantContext.Context.Clone(_siteUrl);
            // Process current site if not the root web
            if (!rootWeb)
            {
                ProcessSite(_webSiteMissingMetaList, ctx, _siteUrl, _parentWebUrl);
            }

            var webs = ctx.Web.Webs;
            IEnumerable<Web> webQuery = ctx.LoadQuery(webs.Include(winc => winc.Url));
            ctx.ExecuteQueryRetry();
            var webUrls = webQuery.Select(s => s.Url);
            foreach (var _inWebUrl in webUrls)
            {
                ProcessSubSites(_webSiteMissingMetaList, _inWebUrl, _siteUrl);
            }
        }

        private void ProcessSite(List _webSiteMissingMetaList, ClientContext ctx, string _siteUrl, string _parentWebUrl)
        {
            LogVerbose("prcessing: {0}", _siteUrl);

            Web _web = ctx.Web;
            ctx.Load(_web,
                s => s.Title,
                s => s.Created,
                s => s.Url);
            ctx.ExecuteQueryRetry();

            var _webUrl = UrlPattern(_web.Url);

            // Set owners
            var _siteOwners = GetOwners(ctx, ctx.Web);

            // Web Title
            var _webTitle = _web.Title.Replace(",", " ");
            var _listExists = false;
            var _listItemCount = 0;
            IEnumerable<List> _listQuery = null; List _list = null;

            try
            {

                _listQuery = ctx.LoadQuery(ctx.Web.Lists
                    .Include(
                        winc => winc.Id,
                        winc => winc.Hidden,
                        winc => winc.HasUniqueRoleAssignments,
                        winc => winc.ItemCount,
                        winc => winc.Title,
                        winc => winc.DefaultViewUrl,
                        winc => winc.IsApplicationList,
                        winc => winc.IsCatalog,
                        winc => winc.IsPrivate,
                        winc => winc.IsSiteAssetsLibrary,
                        winc => winc.IsSystemList,
                        winc => winc.LastItemDeletedDate,
                        winc => winc.LastItemModifiedDate,
                        winc => winc.LastItemUserModifiedDate)
                    .Where(w => !w.IsSystemList));
                ctx.ExecuteQueryRetry();

                if (_listQuery.Any(lq => lq.Title.Equals("metadata", StringComparison.CurrentCultureIgnoreCase)
                     || lq.DefaultViewUrl.IndexOf("lists/metadata", StringComparison.CurrentCultureIgnoreCase) > -1))
                {
                    _list = _listQuery.FirstOrDefault(lq => lq.Title.Equals("metadata", StringComparison.CurrentCultureIgnoreCase)
                        || lq.DefaultViewUrl.IndexOf("lists/metadata", StringComparison.CurrentCultureIgnoreCase) > -1);

                    _listExists = true;
                    _listItemCount = _list.ItemCount;

                    if (_listItemCount > 0 && Opts.ProcessPermissions)
                    {
                        LogVerbose("Validating permissions for the Metadata List {0}", _siteUrl);

                        //Ensure permissions are set up correctly
                        if (_list.HasUniqueRoleAssignments)
                        {
                            LogVerbose("unique permissions ... YES");
                            // check if the everyone or UG-EPA-Employees-Only have the right permissions
                            if (!(CheckListPermissions(ctx, _web, _list)))
                            {
                                //set permissions
                                SetListPermissions(ctx, _web, _list);
                            }
                        }
                        else
                        {
                            LogVerbose("unique permissions ... NO");
                            //break inheritance
                            try
                            {
                                _list.BreakRoleInheritance(true, false);
                            }
                            catch (Exception e)
                            {
                                LogWarning("Failed inside BreakRoleInheritance {0}", e.Message);
                            }

                            //set permissions
                            SetListPermissions(ctx, _web, _list);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning("Failed inside MetaData LoadQuery {0}", ex.Message);
            }

            ProcessIdentifiedSite(_webSiteMissingMetaList, ctx, _siteUrl, _webTitle, _listExists, _listItemCount, _web.Created, _siteOwners);
        }


        private void SetListPermissions(ClientContext ctx, Web _web, List _list)
        {
            LogVerbose("setting permissions");

            try
            {

                User _user = ctx.Web.EnsureUser(UgEveryoneClaimIdentifier);
                ctx.Load(_user);
                ctx.ExecuteQueryRetry();

                if (ShouldProcess("Adding UG-Everyone group to metadata list"))
                {
                    RoleDefinition readDef = ctx.Web.RoleDefinitions.GetByName("Read");
                    RoleDefinitionBindingCollection roleDefCollection = new RoleDefinitionBindingCollection(ctx);
                    ctx.Load(readDef);
                    roleDefCollection.Add(readDef);

                    _list.RoleAssignments.Add(_user, roleDefCollection);
                    ctx.ExecuteQueryRetry();
                }
            }
            catch (Exception e)
            {
                LogWarning("Failed inside setListPermissions {0}", e.Message);
            }
        }


        private bool CheckListPermissions(ClientContext ctx, Web _web, List _list)
        {
            bool _permSet = false;

            try
            {
                IEnumerable<RoleAssignment> _roleAssignments = _list.Context.LoadQuery(_list.RoleAssignments.Include(lra => lra.Member));
                _list.Context.ExecuteQueryRetry();
                foreach (RoleAssignment _role in _roleAssignments
                    .Where(raq => raq.Member.Title.ToLower().Contains("ug-")
                        || raq.Member.LoginName.ToLower().Contains(UgEveryoneClaimIdentifier)))
                {
                    _permSet = true;
                    break;
                }
            }
            catch (Exception e)
            {
                LogWarning("Failed inside checkListPermissions {0}", e.Message);
            }
            return _permSet;
        }

        /// <summary>
        /// Process the site into the missing metadata list
        /// </summary>
        /// <param name="_webSiteMissingMetaList">The missing metadata list for addition/updating</param>
        /// <param name="ctx"></param>
        /// <param name="_siteUrl"></param>
        /// <param name="_siteTitle"></param>
        /// <param name="_metadataListExists"></param>
        /// <param name="_itemCount"></param>
        /// <param name="_dateCreated"></param>
        /// <param name="_siteOwners">Set owners</param>
        private void ProcessIdentifiedSite(List _webSiteMissingMetaList, ClientContext ctx, string _siteUrl, string _siteTitle, bool _metadataListExists, int _itemCount, DateTime _dateCreated, string _siteOwners)
        {
            // ensure the URL is the right pattern to store in SharePoint
            var _sitecontexturl = UrlPattern(_siteUrl);

            // Check if site was already added to the report list
            if (_metadataListExists && _itemCount > 0)
            {
                LogVerbose("The metadata list exists skip processing.");
                if (MissingMetadataSites.Any(mm => mm.SiteUrl.Equals(_sitecontexturl, StringComparison.CurrentCultureIgnoreCase)))
                {
                    // lets update the collection to handle this metadata to be removed
                    MissingMetadataSites.FirstOrDefault(mm => mm.SiteUrl.Equals(_sitecontexturl, StringComparison.CurrentCultureIgnoreCase)).CanBeRemoved = true;
                }
                return;
            }

            // Set region
            var _siteRegion = GetRegionSiteType(_sitecontexturl);

            Model_SiteRequestItem _siteRequestId = null;
            if (SiteRequests.Any(sr => sr.SiteUrl.Equals(_siteUrl, StringComparison.CurrentCultureIgnoreCase)))
            {
                //set the siteRequestId column 
                _siteRequestId = SiteRequests.FirstOrDefault(sr => sr.SiteUrl.Equals(_siteUrl, StringComparison.CurrentCultureIgnoreCase));
            }


            LogVerbose("--->> {0};{1};{2};{3};{4};SiteRequestExists:{5}", _siteUrl, _siteTitle, _dateCreated.ToShortDateString(), _metadataListExists, _itemCount, (_siteRequestId != null));

            if (!MissingMetadataSites.Any(mm => mm.SiteUrl.Equals(_sitecontexturl, StringComparison.CurrentCultureIgnoreCase)))
            {
                // Site is not in missing metadata list
                var _urlwithouttrailingslash = (_sitecontexturl.Length == (_sitecontexturl.LastIndexOf("/") + 1)) ? _sitecontexturl.Substring(0, _sitecontexturl.LastIndexOf("/")) : _sitecontexturl;
                try
                {
                    var newItemId = default(Int32);
                    if (ShouldProcess($"Inserting new row for {_sitecontexturl}"))
                    {
                        var itemCreateInfo = new ListItemCreationInformation();
                        ListItem newItem = _webSiteMissingMetaList.AddItem(itemCreateInfo);
                        newItem[Constants_SiteRequest.MissingMetadataFields.Field_Title] = _urlwithouttrailingslash;
                        newItem[Constants_SiteRequest.MissingMetadataFields.FieldText_SiteTitle] = _siteTitle;
                        newItem[Constants_SiteRequest.MissingMetadataFields.FieldText_Region] = _siteRegion.Region;
                        newItem[Constants_SiteRequest.MissingMetadataFields.FieldText_Type1] = _siteRegion.SiteType;
                        newItem[Constants_SiteRequest.MissingMetadataFields.FieldMultiText_SiteOwners] = _siteOwners;
                        newItem[Constants_SiteRequest.MissingMetadataFields.FieldText_HasMetaDataList] = _metadataListExists;
                        newItem[Constants_SiteRequest.MissingMetadataFields.FieldInteger_NumberofItems] = _itemCount;
                        if (_siteRequestId != null)
                        {
                            newItem[Constants_SiteRequest.MissingMetadataFields.FieldText_SiteRequestID] = _siteRequestId.Id;
                        }

                        newItem.SystemUpdate();
                        _webSiteMissingMetaList.Context.Load(newItem, nctx => nctx.Id);
                        _webSiteMissingMetaList.Context.ExecuteQueryRetry();
                        newItemId = newItem.Id;
                    }

                    MissingMetadataSites.Add(new Model_MetadataItem()
                    {
                        Id = newItemId,
                        SiteUrl = _sitecontexturl,
                        EmailSentFlag = false,
                        HasMetadata = false,
                        PrcInserted = true
                    });
                }
                catch (Exception e)
                {
                    LogWarning("Failed inside processIdentifiedSite=>AddMissingInfoSitetoList {0}", e.Message);
                }
            }
            else
            {
                // Site is in missing metadata list
                var _metadataItem = MissingMetadataSites.FirstOrDefault(mm => mm.SiteUrl.Equals(_sitecontexturl, StringComparison.CurrentCultureIgnoreCase));
                try
                {
                    if (ShouldProcess($"Updating row for {_sitecontexturl}"))
                    {
                        ListItem _missingSiteItem = _webSiteMissingMetaList.GetItemById(_metadataItem.Id.ToString());
                        _missingSiteItem[Constants_SiteRequest.MissingMetadataFields.FieldText_SiteTitle] = _siteTitle;
                        _missingSiteItem[Constants_SiteRequest.MissingMetadataFields.FieldText_HasMetaDataList] = _metadataListExists;
                        _missingSiteItem[Constants_SiteRequest.MissingMetadataFields.FieldInteger_NumberofItems] = _itemCount;
                        _missingSiteItem[Constants_SiteRequest.MissingMetadataFields.FieldMultiText_SiteOwners] = _siteOwners;
                        if (_siteRequestId != null)
                        {
                            _missingSiteItem[Constants_SiteRequest.MissingMetadataFields.FieldText_SiteRequestID] = _siteRequestId.Id;
                        }
                        _missingSiteItem.SystemUpdate();
                        _webSiteMissingMetaList.Context.ExecuteQueryRetry();
                    }
                    _metadataItem.PrcUpdated = true;
                }
                catch (Exception e)
                {
                    LogWarning("Failed inside processIdentifiedSite=>UpdateMissingSiteInfo {0}", e.Message);
                }
            }
        }

        private string GetOwners(ClientContext ctx, Web _web)
        {
            var _ownerStr = string.Empty;
            try
            {
                try
                {
                    var _owners = _web.Context.LoadQuery(_web.AssociatedOwnerGroup.Users.Include(ur => ur.LoginName, utx => utx.Email));
                    _web.Context.ExecuteQueryRetry();
                    foreach (var _user in _owners)
                    {
                        if (_user.LoginName.ToLower().IndexOf("system") < 0)
                        {
                            _ownerStr += RemoveClaimIdentifier(_user.Email) + ";";
                        }
                    }
                }
                catch (Exception e)
                {
                    LogWarning("Failed to retreive Owner Group users {0}", e.Message);
                    ctx.Site.EnsureProperties(ctxs => ctxs.Owner);
                    User _siteOwner = ctx.Site.Owner;
                    _ownerStr = _siteOwner.Email + ";";
                }
            }
            catch (Exception e)
            {
                LogWarning("Failed [getOwners] {0}", e.Message);
            }

            return _ownerStr;
        }

    }
}
