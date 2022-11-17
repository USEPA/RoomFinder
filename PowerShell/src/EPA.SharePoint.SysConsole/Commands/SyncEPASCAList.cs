using CommandLine;
using EPA.Office365.Diagnostics;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Apps;
using EPA.SharePoint.SysConsole.Models.Governance;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("syncEPASCAList", HelpText = "Syncs epa personnel across two site collections.")]
    public class SyncEPASCAListOptions : TenantCommandOptions
    {
        [Option('u', "username", Required = false, HelpText = "if provided will filter list to specific username.")]
        public string UserNameFilter { get; set; }
    }

    public static class SyncEPASCAListOptionsExtensions
    {
        public static int RunGenerateAndReturnExitCode(this SyncEPASCAListOptions opts, IAppSettings appSettings)
        {
            var cmd = new SyncEPASCAList(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Sync the SCA List from one site to the other
    /// </summary>
    /// <remarks>
    ///     Sync-EPASCAList -Verbose
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "COM exceptions unknown")]
    public class SyncEPASCAList : BaseSpoTenantCommand<SyncEPASCAListOptions>
    {

        #region Private Variables 

        internal string ClientId { get; set; }

        internal string ClientSecret { get; set; }

        #endregion

        public SyncEPASCAList(SyncEPASCAListOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var TenantAdminUrl = Settings.Commands.TenantAdminUrl;
            ClientId = Settings.SpoAddInMakeEPASite.ClientId;
            ClientSecret = Settings.SpoAddInMakeEPASite.ClientSecret;

            Settings.AzureAd.SPClientID = ClientId;
            Settings.AzureAd.SPClientSecret = ClientSecret;

            LogVerbose($"AppCredential connecting and setting Add-In keys");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantAdminUrl), null, ClientId, ClientSecret, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {

            // get client/app key model
            if (this.SPConnection.IsAddInCredentials)
            {
                ClientId = this.SPConnection.AddInCredentials.AppId;
                ClientSecret = this.SPConnection.AddInCredentials.AppKey;
            }

            // Initialize collections
            var _SourceSCAs = new List<SCAModel>();
            var _DestinationAdminLKs = new List<DestinationSCAModel>();

            var _scaSiteUrl = Settings.Commands.SiteCollectionAdminUrl;

            // ensure the current app is registered in the app catalog and authorized in the tenant
            var scaSiteAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
            using (var scaContext = scaSiteAuthManager.GetAppOnlyAuthenticatedContext(_scaSiteUrl, ClientId, ClientSecret))
            {
                Web _web = scaContext.Web;
                List _scaList = scaContext.Web.Lists.GetByTitle(Constants_SiteRequest.SCAListName);

                scaContext.Load(_web, wctx => wctx.Id, wctx => wctx.Title, wctx => wctx.Url);
                scaContext.Load(_scaList, lctx => lctx.Id, lctx => lctx.Title, lctx => lctx.ItemCount);
                scaContext.ExecuteQueryRetry();

                // Write log to begin process
                LogVerbose("initSourceSCAListArray ...");

                // Initials array with current sites listed under the missing metadata section
                _SourceSCAs = InitSourceSCAListArray(scaContext, _scaList);

                // Write log of the SCAs in the Source List
                LogVerbose($"done initiating [Source] array... count: {_SourceSCAs.Count()}");
            }

            var _siteRequestUrl = Settings.Commands.SPOSiteRequestUrl;
            var siteAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
            using (var siteContext = siteAuthManager.GetAppOnlyAuthenticatedContext(_siteRequestUrl, ClientId, ClientSecret))
            {
                // Load web for property retrieval
                Web _web = siteContext.Web;
                List _adminLkList = siteContext.Web.Lists.GetByTitle(Constants_SiteRequest.AdminsLK);
                List _listSites = siteContext.Web.Lists.GetByTitle(Constants_SiteRequest.CollectionSiteTypesLK);

                siteContext.Load(_web, wctx => wctx.Id, wctx => wctx.Title, wctx => wctx.Url);
                siteContext.Load(_adminLkList, lctx => lctx.Id, lctx => lctx.Title, lctx => lctx.ItemCount);
                siteContext.Load(_listSites, lctx => lctx.Id, lctx => lctx.Title, lctx => lctx.ItemCount);
                siteContext.ExecuteQueryRetry();

                // Write log to begin process
                LogVerbose("initDestinationSCAListArray ...");

                // Initials array with current sites listed under the missing metadata section
                _DestinationAdminLKs = InitDestinationSCAListArray(siteContext, _adminLkList);

                // Write log of the SCAs in the Destination List
                LogVerbose($"done initiating [Destination] array ...  count: {_DestinationAdminLKs.Count()}");

                // Load the Site Collection Options
                var _siteCollectionObjects = InitSiteCollectionArray(siteContext, _listSites);

                // Write log of the site collection objects
                LogVerbose($"done initiating [SiteCollection] array({_siteCollectionObjects.Count()})");

                // Process new users
                //processSCAs(siteContext);

                // Admin LK
                //      If Users do not exists in Source SCA Listing [Delete]
                //
                //      If Users do exists in Source SCA Listing
                //          Compare Offices
                //              If Office Matches [Stay]
                //              If Office Does not match [Update or Add]
                //
                //  SCA Listing
                //      If Users do not exists in AdminLK [Add]


                // Distinct list of SCA's from the Source List
                var _distinctSCAEmails = _SourceSCAs.Select(s => s.Email).Distinct().ToList();


                // Join the SCA Listing with the Site Collection List
                var scaRecords =
                    from scas in _SourceSCAs
                    join stypes in _siteCollectionObjects on scas.OfficeTitle.ToLower() equals stypes.SiteCollection.ToLower()
                    select new { scas.Email, scas.OfficeTitle, stypes.ListItemId, stypes.SiteTitle, stypes.SiteType, stypes.SiteCollection, stypes.SiteCollectionId };


                // Not in SCA Listing any longer [Delete from AdminLK]
                var notinSource = _DestinationAdminLKs.Where(_DestSCA => !_distinctSCAEmails.Any(_srcSCAEmail => _DestSCA.Email == _srcSCAEmail));
                foreach (var _adminLkItem in notinSource)
                {
                    //Delete SCA
                    LogVerbose("Deleting user: {0} -- {1}", _adminLkItem.Email, _adminLkItem.ListItemId);
                    DeleteSCAfromList(siteContext, _adminLkList, _adminLkItem.ListItemId);
                }

                // In SCA Listing and AdminLK
                var inDestination = _distinctSCAEmails.Where(_srcSCAEmail => _DestinationAdminLKs.Any(_DestSCA => _srcSCAEmail == _DestSCA.Email));
                foreach (var _scaItemEmail in inDestination)
                {
                    // Return the specific records for this user
                    var scaInRecords = scaRecords.Where(scas => scas.Email.Equals(_scaItemEmail, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    if (scaInRecords.Any())
                    {
                        LogVerbose("Matching user {0} -- with organizational records {1}", _scaItemEmail, scaInRecords.Count());

                        try
                        {
                            User _SCAobj = siteContext.Web.EnsureUser("i:0#.f|membership|" + _scaItemEmail);
                            siteContext.Load(_SCAobj);
                            siteContext.ExecuteQueryRetry();


                            var existingRecords = _DestinationAdminLKs.Where(_DestSCA => _scaItemEmail == _DestSCA.Email).ToList();
                            if (existingRecords.Any(er => !scaInRecords.Any(sr => er.Email == sr.Email && er.CollectionSiteTypeId == sr.ListItemId)))
                            {
                                // Existing records that don't overlap with the Site Collection match up with the user
                                var mismatched = existingRecords.Where(er => !scaInRecords.Any(sr => er.Email == sr.Email && er.CollectionSiteTypeId == sr.ListItemId));

                                foreach (var adminItem in mismatched)
                                {
                                    DeleteSCAfromList(siteContext, _adminLkList, adminItem.ListItemId);
                                }
                            }

                            if (scaInRecords.Any(er => !existingRecords.Any(sr => er.Email == sr.Email && sr.CollectionSiteTypeId == er.ListItemId)))
                            {
                                // New records that do not exist in the destination list
                                var newmatches = scaInRecords.Where(er => !existingRecords.Any(sr => er.Email == sr.Email && sr.CollectionSiteTypeId == er.ListItemId));

                                foreach (var siteLookup in newmatches)
                                {
                                    var _office = siteLookup.SiteCollection;
                                    var _officeLookUpId = siteLookup.ListItemId;
                                    var _siteCollectionId = siteLookup.SiteCollectionId;
                                    AddSCAItemtoList(siteContext, _adminLkList, _SCAobj, _office, _officeLookUpId, _siteCollectionId);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.LogError(e, $"Failed in addSCAItemtoList with msg {e.Message}");
                        }
                    }
                }


                // Not in AdminLK list - lets add
                var notInDestination = _distinctSCAEmails.Where(_srcSCAEmail => !_DestinationAdminLKs.Any(_DestSCA => _srcSCAEmail == _DestSCA.Email));
                foreach (var _scaItemEmail in notInDestination)
                {
                    var scaInRecords = scaRecords.Where(scas => scas.Email.Equals(_scaItemEmail, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    if (scaInRecords.Any())
                    {
                        LogVerbose("Non Matching user {0} -- with organizational records {1}", _scaItemEmail, scaInRecords.Count());

                        try
                        {
                            User _SCAobj = siteContext.Web.EnsureUser("i:0#.f|membership|" + _scaItemEmail);
                            siteContext.Load(_SCAobj);
                            siteContext.ExecuteQueryRetry();

                            foreach (var siteLookup in scaInRecords)
                            {
                                var _office = siteLookup.SiteCollection;
                                var _officeLookUpId = siteLookup.ListItemId;
                                var _siteCollectionId = siteLookup.SiteCollectionId;

                                AddSCAItemtoList(siteContext, _adminLkList, _SCAobj, _office, _officeLookUpId, _siteCollectionId);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.LogError(e, $"Failed in addSCAItemtoList with msg {e.Message}");
                        }
                    }
                }

            }

            return 1;
        }


        public override void OnEnd()
        {
            base.OnEnd();

            if (!DisconnectCurrentService())
            {
                LogError(new InvalidOperationException(Office365.CoreResources.NoConnectionToDisconnect), "Failed to disconnect session in SyncEPASCAList Command.");
            }
        }

        #region Helper Method

        private List<CollectionSiteLookup> InitSiteCollectionArray(ClientContext ctx, List _listSites)
        {
            var _LocalSites = new List<CollectionSiteLookup>();
            try
            {
                CamlQuery camlQuery = new CamlQuery
                {
                    ViewXml = CAML.ViewQuery(
                        ViewScope.RecursiveAll,
                        string.Empty,
                        CAML.OrderBy(
                            new OrderByField(Constants_SiteRequest.CollectionSiteTypesLKFields.Field_Title)),
                        CAML.ViewFields((new string[] {
                            Constants_SiteRequest.CollectionSiteTypesLKFields.Field_Title,
                            Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_TemplateName,
                            Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_Name1,
                            Constants_SiteRequest.CollectionSiteTypesLKFields.FieldBoolean_ShowInDropDown,
                            Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_CollectionURL,
                            Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteCollection1,
                            Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteType,
                            Constants_SiteRequest.CollectionSiteTypesLKFields.Field_ID
                        }).Select(s => CAML.FieldRef(s)).ToArray()),
                        100)
                };
                ListItemCollectionPosition itemPosition = null;

                while (true)
                {
                    camlQuery.ListItemCollectionPosition = itemPosition;
                    ListItemCollection listItems = _listSites.GetItems(camlQuery);
                    ctx.Load(listItems);
                    ctx.ExecuteQueryRetry();
                    itemPosition = listItems.ListItemCollectionPosition;

                    foreach (ListItem listItem in listItems)
                    {
                        try
                        {
                            var _SiteCollection1 = listItem.RetrieveListItemValueAsLookup(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteCollection1);
                            var _SiteType = listItem.RetrieveListItemValueAsLookup(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteType);

                            if (_SiteCollection1.LookupValue != null)
                            {
                                _LocalSites.Add(new CollectionSiteLookup()
                                {
                                    ListItemId = listItem.Id,
                                    SiteTitle = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.Field_Title).ToLower(),
                                    TemplateName = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_TemplateName).ToLower(),
                                    TypeTitle = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_Name1).ToLower(),
                                    ShowInDropDown = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldBoolean_ShowInDropDown).ToLower(),
                                    SiteUrl = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_CollectionURL).ToLower(),
                                    SiteCollectionId = _SiteCollection1.LookupId,
                                    SiteCollection = _SiteCollection1.ToLookupValue(),
                                    SiteTypeId = _SiteType.LookupId,
                                    SiteType = _SiteType.ToLookupValue()
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            Log.LogError(e, $"Failed in initSourceSCAListArray with msg {e.Message}");
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
                Log.LogError(e, $"Failed in initSourceSCAListArray with msg {e.Message}");
            }

            return _LocalSites;
        }

        /// <summary>
        /// Build out the collection of SCA's in the source list
        /// </summary>
        /// <param name="scaContext"></param>
        /// <param name="_scaList">The SCA List initialized from the clientcontext</param>
        private List<SCAModel> InitSourceSCAListArray(ClientContext scaContext, List _scaList)
        {
            var _LocalSourceSCAs = new List<SCAModel>();
            try
            {
                var camlWhere = string.Empty;
                if (!string.IsNullOrEmpty(Opts?.UserNameFilter))
                {
                    // Lets filter the results based on a single user for testing purposes or to force a replication
                    User _SCAobj = scaContext.Web.EnsureUser("i:0#.f|membership|" + Opts.UserNameFilter);
                    scaContext.Load(_SCAobj);
                    scaContext.ExecuteQueryRetry();

                    camlWhere = CAML.Where(CAML.Eq(CAML.FieldValue(Constants_SiteRequest.SCAListFields.FieldUserValue_EmployeeName, FieldType.User.ToString("f"), _SCAobj.Id.ToString(), "LookupId='True'")));
                }

                CamlQuery camlQuery = new CamlQuery
                {
                    ViewXml = CAML.ViewQuery(
                        ViewScope.RecursiveAll,
                        camlWhere,
                        CAML.OrderBy(
                            new OrderByField(Constants_SiteRequest.SCAListFields.FieldUserValue_EmployeeName),
                            new OrderByField(Constants_SiteRequest.SCAListFields.Field_Title)
                            ),
                        CAML.ViewFields((new string[] {
                            Constants_SiteRequest.SCAListFields.Field_Title,
                            Constants_SiteRequest.SCAListFields.FieldUserValue_EmployeeName,
                            Constants_SiteRequest.SCAListFields.FieldText_SCARole,
                            Constants_SiteRequest.SCAListFields.FieldText_RegionProgram,
                            Constants_SiteRequest.SCAListFields.Field_ID
                        }).Select(s => CAML.FieldRef(s)).ToArray()),
                        100)
                };
                ListItemCollectionPosition itemPosition = null;

                while (true)
                {
                    camlQuery.ListItemCollectionPosition = itemPosition;
                    ListItemCollection listItems = _scaList.GetItems(camlQuery);
                    scaContext.Load(listItems);
                    scaContext.ExecuteQueryRetry();
                    itemPosition = listItems.ListItemCollectionPosition;

                    foreach (ListItem listItem in listItems)
                    {
                        try
                        {
                            var _user = listItem.RetrieveListItemUserValue(Constants_SiteRequest.SCAListFields.FieldUserValue_EmployeeName);
                            var _userObjEmail = _user.ToUserEmailValue();
                            var _regionOfficeText = listItem.RetrieveListItemValue(Constants_SiteRequest.SCAListFields.FieldText_RegionProgram);

                            _LocalSourceSCAs.Add(new SCAModel()
                            {
                                Email = _userObjEmail.ToLower().Trim(),
                                OfficeTitle = listItem.RetrieveListItemValue(Constants_SiteRequest.SCAListFields.Field_Title).ToLower().Trim(),
                                SCA_x0020_Role = listItem.RetrieveListItemValue(Constants_SiteRequest.SCAListFields.FieldText_SCARole),
                                Region_x0020_or_x0020_Program_x0 = _regionOfficeText
                            });
                        }
                        catch (Exception e)
                        {
                            Log.LogError(e, $"Failed in initSourceSCAListArray with msg {e.Message}");
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
                Log.LogError(e, $"Failed in initSourceSCAListArray with msg {e.Message}");
            }

            return _LocalSourceSCAs;
        }

        /// <summary>
        /// Build out the collection of Destination SCA's in the destination list
        /// </summary>
        /// <param name="siteContext"></param>
        /// <param name="_adminLkList">The AdminLK List initialized from the context</param>
        private List<DestinationSCAModel> InitDestinationSCAListArray(ClientContext siteContext, List _adminLkList)
        {
            var _localDestinationAdminLKs = new List<DestinationSCAModel>();

            try
            {
                var camlWhere = string.Empty;
                if (!string.IsNullOrEmpty(Opts?.UserNameFilter))
                {
                    // Lets filter the results based on a single user for testing purposes or to force a replication
                    User _SCAobj = siteContext.Web.EnsureUser("i:0#.f|membership|" + Opts.UserNameFilter);
                    siteContext.Load(_SCAobj);
                    siteContext.ExecuteQueryRetry();

                    camlWhere = CAML.Where(CAML.Eq(CAML.FieldValue(Constants_SiteRequest.AdminsLKFields.FieldUser_AdminEmailObject, FieldType.Integer.ToString("f"), _SCAobj.Id.ToString(), "LookupId='True'")));
                }

                var fields = new string[] {
                    Constants_SiteRequest.AdminsLKFields.Field_Title,
                    Constants_SiteRequest.AdminsLKFields.FieldUser_AdminEmailObject,
                    Constants_SiteRequest.AdminsLKFields.FieldText_AdminEmail,
                    Constants_SiteRequest.AdminsLKFields.FieldLookup_CollectionSiteType,
                    Constants_SiteRequest.AdminsLKFields.FieldLookup_SiteCollectionName,
                    Constants_SiteRequest.AdminsLKFields.Field_ID
                };

                CamlQuery camlQuery = new CamlQuery
                {
                    ViewXml = CAML.ViewQuery(
                        ViewScope.RecursiveAll,
                        camlWhere,
                        CAML.OrderBy(new OrderByField(Constants_SiteRequest.AdminsLKFields.Field_Title)),
                        CAML.ViewFields(fields.Select(s => CAML.FieldRef(s)).ToArray()),
                        100)
                };
                ListItemCollectionPosition itemPosition = null;

                while (true)
                {
                    camlQuery.ListItemCollectionPosition = itemPosition;

                    ListItemCollection listItems = _adminLkList.GetItems(camlQuery);
                    siteContext.Load(listItems);
                    siteContext.ExecuteQuery();
                    itemPosition = listItems.ListItemCollectionPosition;

                    foreach (ListItem listItem in listItems)
                    {
                        try
                        {
                            var _userPlainText = listItem.RetrieveListItemValue(Constants_SiteRequest.AdminsLKFields.FieldText_AdminEmail);
                            var _user = listItem.RetrieveListItemUserValue(Constants_SiteRequest.AdminsLKFields.FieldUser_AdminEmailObject);
                            var _userEmail = _user.ToUserEmailValue();

                            var collectionSiteType = listItem.RetrieveListItemValueAsLookup(Constants_SiteRequest.AdminsLKFields.FieldLookup_CollectionSiteType);
                            var siteCollectionName = listItem.RetrieveListItemValueAsLookup(Constants_SiteRequest.AdminsLKFields.FieldLookup_SiteCollectionName);

                            _localDestinationAdminLKs.Add(new DestinationSCAModel()
                            {
                                Email = _userEmail.ToLower().Trim(),
                                OfficeTitle = listItem.RetrieveListItemValue(Constants_SiteRequest.AdminsLKFields.Field_Title).ToLower().Trim(),
                                ListItemId = listItem.Id,
                                CollectionSiteTypeId = collectionSiteType.LookupId,
                                CollectionSiteType = collectionSiteType.ToLookupValue().ToLower()
                            });
                        }
                        catch (Exception e)
                        {
                            Log.LogError(e, $"Failed in initDestinationSCAListArray with msg {e.Message}");
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
                Log.LogError(e, $"Failed in initDestinationSCAListArray with msg {e.Message}");
            }

            return _localDestinationAdminLKs;
        }

        /// <summary>
        /// Create new SCAs and update existing SCAs
        /// </summary>
        /// <param name="siteContext"></param>
        /// <param name="_SourceSCAs">SCA Listing </param>
        /// <param name="_DestinationAdminLKs">Admin LK [Lookup list for MakeEPASite]</param>
        private void ProcessSCAs(ClientContext siteContext, List _adminLkList, List<SCAModel> _SourceSCAs, List<DestinationSCAModel> _DestinationAdminLKs, List<CollectionSiteLookup> _siteCollectionObjects)
        {
            // Load web for property retrieval
            foreach (var _SrcSCA in _SourceSCAs)
            {
                LogVerbose(">>> Scanning => {0}", _SrcSCA.ToString());

                bool _found = false;

                foreach (var _DestSCA in _DestinationAdminLKs)
                {
                    if (_DestSCA.Email.Contains(_SrcSCA.Email))
                    {
                        _found = true;

                        // check if user is in the right office, if not update the record
                        if (!(_DestSCA.OfficeTitle.Contains(_SrcSCA.OfficeTitle)))
                        {
                            // update user item
                            LogVerbose("Updating: {0}", _DestSCA.ListItemId);
                            LogVerbose("Destination User: {0} and Office {1}", _DestSCA.Email, _DestSCA.OfficeTitle);
                            LogVerbose("Source User: {0} and Office {1}", _SrcSCA.Email, _SrcSCA.OfficeTitle);

                            string _typeHint = "";
                            if (_DestSCA.CollectionSiteType.ToLower().Contains("work"))
                            {
                                _typeHint = "work";
                            }
                            else if (_DestSCA.CollectionSiteType.ToLower().Contains("community"))
                            {
                                _typeHint = "community";
                            }
                            else if (_DestSCA.CollectionSiteType.ToLower().Contains("organization"))
                            {
                                _typeHint = "organization";
                            }
                            else
                            {
                                _typeHint = "";
                            }

                            // Delete then Add
                            LogVerbose("{0} Office {1} Type {2}", _DestSCA.ListItemId, _SrcSCA.OfficeTitle, _typeHint);
                        }
                    }
                }

                if (_found == false)
                {
                    var _userEmail = _SrcSCA.Email;
                    User _SCAobj = siteContext.Web.EnsureUser("i:0#.f|membership|" + _userEmail);
                    siteContext.Load(_SCAobj);
                    siteContext.ExecuteQueryRetry();

                    //Create user
                    LogVerbose("Adding user: " + _userEmail);
                    var _siteTypes = _siteCollectionObjects.Where(sco => sco.SiteCollection.Equals(_SrcSCA.OfficeTitle, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    if (_siteTypes.Count() > 0)
                    {
                        foreach (var siteLookup in _siteTypes)
                        {
                            var _office = siteLookup.SiteCollection;
                            var _officeLookUpId = siteLookup.ListItemId;
                            var _siteCollectionId = siteLookup.SiteCollectionId;

                            AddSCAItemtoList(siteContext, _adminLkList, _SCAobj, _office, _officeLookUpId, _siteCollectionId);
                        }
                    }
                }
            }


            // Delete SCAs no longer in the list
            foreach (var _DetSCA in _DestinationAdminLKs)
            {
                // LogVerbose(">>> " + _SrcSCA);
                var _found = _SourceSCAs.Any(_SrcSCA => _SrcSCA.Email.Contains(_DetSCA.Email));
                if (_found == false)
                {
                    //Delete SCA
                    LogVerbose("Deleting user: {0} -- {1}", _DetSCA.Email, _DetSCA.ListItemId);
                    DeleteSCAfromList(siteContext, _adminLkList, _DetSCA.ListItemId);
                }
            }

        }


        private bool AddSCAItemtoList(ClientContext siteContext, List _list, User _SCAobj, string _office, int _officeLookUpId, int _siteCollectionId)
        {

            if (ShouldProcess(string.Format("Adding user {0} with {1} - {2}", _SCAobj.Email, _office, _officeLookUpId)))
            {
                try
                {
                    FieldLookupValue ColSiteTypeFV = new FieldLookupValue
                    {
                        LookupId = _officeLookUpId
                    };

                    FieldLookupValue SiteCollectionLookup = new FieldLookupValue
                    {
                        LookupId = _siteCollectionId
                    };

                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem newItem = _list.AddItem(itemCreateInfo);
                    newItem[Constants_SiteRequest.AdminsLKFields.Field_Title] = _office;
                    newItem[Constants_SiteRequest.AdminsLKFields.FieldLookup_CollectionSiteType] = ColSiteTypeFV;
                    newItem[Constants_SiteRequest.AdminsLKFields.FieldLookup_SiteCollectionName] = SiteCollectionLookup;
                    //newItem[Constants_SiteRequest.AdminsLKFields.FieldText_EmailTo] = _SCAobj.Email;
                    newItem[Constants_SiteRequest.AdminsLKFields.FieldText_AdminEmail] = _SCAobj.Email;
                    newItem[Constants_SiteRequest.AdminsLKFields.FieldUser_AdminEmailObject] = _SCAobj;
                    newItem.Update();
                    siteContext.ExecuteQueryRetry();
                    return true;
                }
                catch (Exception e)
                {
                    Log.LogError(e, $"Failed in addSCAItemtoList with msg {e.Message}");
                }
            }
            return false;
        }

        private bool DeleteSCAfromList(ClientContext siteContext, List _list, int _itemId)
        {
            if (ShouldProcess(string.Format("Deleting item {0} from list {1}", _itemId, _list.Title)))
            {
                try
                {
                    ListItem _dItem = _list.GetItemById(_itemId);
                    siteContext.Load(_dItem);
                    _dItem.DeleteObject();
                    siteContext.ExecuteQueryRetry();
                    return true;
                }
                catch (Exception e)
                {
                    Log.LogError(e, $"Failed in deleteSCAfromList with msg {e.Message}");
                }
            }
            return false;
        }

        #endregion
    }
}
