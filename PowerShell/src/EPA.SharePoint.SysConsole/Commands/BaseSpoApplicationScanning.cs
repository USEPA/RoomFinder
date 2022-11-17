using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Models.Scan;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.WorkflowServices;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
    public abstract class BaseSpoApplicationScanning<T> : BaseSpoTenantCommand<T> where T : IBaseApplicationScanningOptions
    {
        protected BaseSpoApplicationScanning(T opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private Variables 

        internal string _rootSiteUrl { get; set; }

        private List<string> _exList { get; set; }

        private List<string> _inList { get; set; }

        private List<string> _addInTokens { get; set; }

        private DirectoryInfo _resultLogDirectory { get; set; }

        /// <summary>
        /// The Tenant Application Catalog 
        ///     EX: https://usepa.sharepoint.com/sites/USEPA_APPS/
        /// </summary>
        internal string _appCatalogUrl { get; set; }

        /// <summary>
        /// The application site url where the SHARP is processed
        ///     EX: https://usepa.sharepoint.com/sites/oei_Work/SharePointPM/ApplicationReview"
        /// </summary>
        internal string _appSiteUrl { get; set; }

        #endregion

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();


            _exList = new List<string> {
                "cache profiles",
                "master page gallery",
                "site notebook",
                "workflows",
                "wfsvc",
                "_catalogs",
                "_private",
                "_vti_pvt"
            };

            _inList = new List<string> {
                "pages",
                "site pages",
                "site assets"
            };

            _addInTokens = new List<string> {
                "~appWebUrl",
                "~controlTemplates",
                "~hostUrl",
                "~layouts",
                "~remoteAppUrl",
                "~site",
                "~sitecollection"
            };


            _appCatalogUrl = TokenHelper.EnsureTrailingSlash(Settings.Commands.AppCatalogUrl);
            _appSiteUrl = TokenHelper.EnsureTrailingSlash(Settings.Commands.AppScanSiteUrl);

            // build the directory structure in the users AppData for file download/upload
            var appScanFolderPath = Settings.Commands.AppScanResultsFolder;
            if (string.IsNullOrEmpty(appScanFolderPath))
            {
                appScanFolderPath = Path.GetTempPath();
            }

            var appScanFolderDirectory = new System.IO.DirectoryInfo(appScanFolderPath);
            if (!appScanFolderDirectory.Exists)
            {
                appScanFolderDirectory = System.IO.Directory.CreateDirectory(appScanFolderPath);
            }
            _resultLogDirectory = appScanFolderDirectory.CreateSubdirectory("epaappscanlogs", appScanFolderDirectory.GetAccessControl());
        }

        internal ScanResults ProcessSiteCollection(ScanAddInModel scannedAddIn, string addInWebUrl, string appInstanceId)
        {
            if (string.IsNullOrEmpty(scannedAddIn?.AppWebFullUrl))
            {
                var scanResult = new ScanResults(addInWebUrl)
                {
                    SPAddIn = true
                };
                scanResult.Messages.Add(new ScanLog($"Unable to Process App Title: {scannedAddIn?.Title}"));
                scanResult.Messages.Add(new ScanLog($"Unable to Process App ID: {scannedAddIn?.Id} with InstanceId {appInstanceId}"));
                scanResult.Messages.Add(new ScanLog($"The App Web Full URL Does Not Exist For: {scannedAddIn?.Title}."));
                LogWarning($"The App Web Full URL Does Not Exist For {scannedAddIn?.Title} with APP ID {scannedAddIn?.Id}");
                return scanResult;
            }
            return ProcessSiteCollection(scannedAddIn.AppWebFullUrl);
        }

        internal ScanResults ProcessSiteCollection(string _siteUrl)
        {
            ScanResults scanResult = null;

            _siteUrl = _siteUrl.ToLower().Trim();

            var rootSiteFromUrl = new Uri(_siteUrl);
            var _siteAppDash = _siteUrl.IndexOf('-');
            if (_siteAppDash > -1)
            {
                rootSiteFromUrl = new Uri(_siteUrl.Replace(_siteUrl.Substring(_siteAppDash, _siteUrl.IndexOf('.') - _siteAppDash), ""));
            }


            try
            {
                // ensure we have the root site URL
                _rootSiteUrl = TokenHelper.EnsureTrailingSlash(rootSiteFromUrl.GetLeftPart(UriPartial.Authority));
                LogVerbose($"Tenant Root: {_rootSiteUrl} ==> processSiteCollection: {_siteUrl}");

                // set the running as the tenant app
                var webmanager = new OfficeDevPnP.Core.AuthenticationManager();
                using var _siteContext = webmanager.GetAppOnlyAuthenticatedContext(_siteUrl, Settings.AzureAd.SPClientID, Settings.AzureAd.SPClientSecret);
                var _web = _siteContext.Web;
                var _site = _siteContext.Site;
                _siteContext.Load(_web);
                _siteContext.Load(_site);
                _siteContext.ExecuteQueryRetry();

                // Process the site collection
                scanResult = ProcessSite(_siteContext, _web.Url, true);

                // Retreive Site Owners
                if (scanResult.SiteTenant == AddInTenantTypeEnum.Production)
                {
                    GetOwners(_site, scanResult);
                }

                if (Opts.Deepscan)
                {
                    // grab sub webs that aren't AppInstances
                    var _webs = _web.Context.LoadQuery(_web.Webs.Include(s => s.Url));
                    _web.Context.ExecuteQueryRetry();

                    // Process subsites
                    foreach (Web _inWeb in _webs)
                    {
                        var scanCollection = ProcessSubSite(_siteContext, _inWeb);
                        if (scanCollection.Any())
                        {
                            scanResult.SubSites.AddRange(scanCollection);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogWarning("Failed to process site {0} with message {1}", _siteUrl, e.Message);
            }

            return scanResult;
        }

        internal List<ScanResults> ProcessSubSite(ClientContext _siteContext, Web _web)
        {
            var scanCollection = new List<ScanResults>();

            try
            {
                var scanResult = ProcessSite(_siteContext, _web.Url);
                scanCollection.Add(scanResult);

                if (Opts.Deepscan)
                {
                    // grab sub webs that aren't AppInstances
                    var _webs = _web.Context.LoadQuery(_web.Webs.Include(s => s.Url));
                    _web.Context.ExecuteQueryRetry();

                    // Process subsites
                    foreach (Web _inWeb in _webs)
                    {
                        var _webcollection = ProcessSubSite(_siteContext, _inWeb);
                        scanCollection.AddRange(_webcollection);
                    }
                }
            }
            catch (Exception e)
            {
                LogWarning("Failed to process site with message {1}", e.Message);
            }

            return scanCollection;
        }

        internal ScanResults ProcessSite(ClientContext _siteContext, string _webUrl, bool siteCollection = false)
        {
            _webUrl = _webUrl.ToLower().Trim();
            var scanResult = new ScanResults(_webUrl);

            if (siteCollection)
            {
                try
                {
                    _siteContext.Site.EnsureProperties(appid => appid.Id);
                    scanResult.SiteId = _siteContext.Site.Id;
                }
                catch (Exception ex)
                {
                    LogWarning("Failed to process SITE properties {0}", ex);
                }

                try
                {
                    // process any custom scripting
                    ProcessCustomAction(_siteContext, _siteContext.Site, scanResult);
                }
                catch (Exception ex)
                {
                    LogWarning("Failed to process SITE custom action {0}", ex);
                }
            }

            // set the running as the tenant app
            var webmanager = new OfficeDevPnP.Core.AuthenticationManager();
            using var _webContext = webmanager.GetAppOnlyAuthenticatedContext(_webUrl, Settings.AzureAd.SPClientID, Settings.AzureAd.SPClientSecret);
            var _web = _webContext.Web;
            _webContext.Load(_web);
            _webContext.ExecuteQueryRetry();

            try
            {
                scanResult.WebId = _web.Id;
                scanResult.ServerRelativeUrl = _web.ServerRelativeUrl;

                var _siteTitle = _web.Title;
                if (!string.IsNullOrEmpty(_siteTitle))
                {
                    scanResult.Title = _siteTitle;
                }
            }
            catch (Exception ex)
            {
                LogWarning("Failed to retrieve title {0}", ex);
            }

            try
            {
                // process any custom scripting
                ProcessCustomAction(_webContext, _web, scanResult);
            }
            catch (Exception ex)
            {
                LogWarning("Failed to process WEB custom action {0}", ex);
            }

            try
            {
                _web.EnsureProperties(appid => appid.AppInstanceId);
                if (_web.AppInstanceId != null
                    && _web.AppInstanceId != Guid.Empty)
                {
                    // process app instance in this web
                    // first we need to open the parentweb
                    _web.EnsureProperties(appweb => appweb.ParentWeb);
                    var uri = new Uri(new Uri(_rootSiteUrl), _web.ParentWeb.ServerRelativeUrl);

                    // set the running as the tenant app
                    var appWebManager = new OfficeDevPnP.Core.AuthenticationManager();
                    using var parentWebContext = appWebManager.GetAppOnlyAuthenticatedContext(uri.ToString(), Settings.AzureAd.SPClientID, Settings.AzureAd.SPClientSecret);
                    var parentWeb = parentWebContext.Web;
                    parentWebContext.Load(parentWeb);
                    parentWebContext.ExecuteQueryRetry();

                    ProcessAddinInstance(parentWeb, _web.AppInstanceId, scanResult);

                    // add the add-in into the collection
                    scanResult.SPAddIn = true;
                }
            }
            catch (Exception aex)
            {
                LogWarning("Failed to process app instance ID {0}", aex);
            }

            LogVerbose("Processing: {0}", _web.Url);
            scanResult.Messages.Add(new ScanLog("Processing: " + _web.Url));
            scanResult.Messages.Add(new ScanLog($"  Web ServerRelativeUrl: {_web.ServerRelativeUrl}"));
            scanResult.Messages.Add(new ScanLog($"  Web GUID: {_web.Id}"));

            ///////////////////////// Owners
            if (scanResult.SiteTenant == AddInTenantTypeEnum.Production)
            {
                GetOwners(_web, scanResult);
            }

            ///////////////////////// Parse the sites' aspx anf js files fo flags..
            LogVerbose("Checking local solutions ... " + _webUrl);
            scanResult.Messages.Add(new ScanLog("Checking local solutions ... " + _webUrl));

            ParseLists(_webContext, scanResult);


            var violations = scanResult?.Violations.Count() ?? 0;
            LogVerbose("--------------------------------");
            LogVerbose($"Violations {violations}");
            scanResult.Messages.Add(new ScanLog("--------------------------------"));
            scanResult.Messages.Add(new ScanLog($"Violations {violations}"));


            return scanResult;
        }

        /// <summary>
        /// Pulls the site owner and adds to scan results
        /// </summary>
        /// <param name="_site"></param>
        /// <param name="scanResult"></param>
        internal void GetOwners(Site _site, ScanResults scanResult)
        {
            try
            {
                var _siteOwner = _site.Owner;
                _site.Context.Load(_siteOwner);
                _site.Context.ExecuteQueryRetry();

                if (_siteOwner != null)
                {
                    var _userOwner = _siteOwner.LoginName.ToLower();
                    if (_userOwner.IndexOf("system") < 0 && (!(string.IsNullOrEmpty(_userOwner))))
                    {
                        var _cleanUserOwner = RemoveClaimIdentifier(_userOwner);
                        LogVerbose(">>Site Owner>> {0}", _cleanUserOwner);
                        scanResult.SiteOwners.Add(_cleanUserOwner);
                        scanResult.Messages.Add(new ScanLog(">>Site Owner>> " + _cleanUserOwner));
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning("Failed to retreive [site] owners {0}", ex.Message);
            }
        }

        /// <summary>
        /// Pulls the web associated owner group and adds to scan results
        /// </summary>
        /// <param name="_web"></param>
        /// <param name="scanResult"></param>
        internal void GetOwners(Web _web, ScanResults scanResult)
        {
            try
            {
                _web.Context.Load(_web, ctxw => ctxw.AssociatedOwnerGroup, ctxw => ctxw.AssociatedOwnerGroup.Users);
                _web.Context.ExecuteQueryRetry();

                foreach (var _user in _web.AssociatedOwnerGroup.Users)
                {
                    var _userOwner = _user.LoginName.ToLower();
                    if (_userOwner.IndexOf("system") < 0 && (!(string.IsNullOrEmpty(_userOwner))))
                    {
                        var _cleanUserOwner = RemoveClaimIdentifier(_userOwner);
                        LogVerbose(">>Web Owner>> {0}", _cleanUserOwner);
                        scanResult.SiteOwners.Add(_cleanUserOwner);
                        scanResult.Messages.Add(new ScanLog(">>Web Owner>> " + _cleanUserOwner));
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning("Failed to retreive [web] owners {0}", ex.Message);
            }
        }

        /// <summary>
        /// Process an oaml file
        /// </summary>
        /// <param name="_web"></param>
        /// <param name="scanning"></param>
        internal void ProcessWorkflows(ClientContext clientContext, Web _web, ScanResults scanning)
        {
            try
            {
                ListCollection _lists = _web.Lists;
                clientContext.Load(_web);
                clientContext.Load(_lists);
                clientContext.ExecuteQueryRetry();

                WorkflowServicesManager _wfMn = new WorkflowServicesManager(clientContext, _web);
                WorkflowSubscriptionService _wfSubSrv = _wfMn.GetWorkflowSubscriptionService();
                WorkflowDeploymentService _wfDepSrv = _wfMn.GetWorkflowDeploymentService();
                WorkflowInstanceService _wfInstSrv = _wfMn.GetWorkflowInstanceService();


                foreach (List _list in _lists)
                {
                    WorkflowSubscriptionCollection _wfSubs = _wfSubSrv.EnumerateSubscriptionsByList(_list.Id);

                    clientContext.Load(_wfSubs);
                    clientContext.ExecuteQueryRetry();

                    foreach (WorkflowSubscription _wfSub in _wfSubs)
                    {
                        LogVerbose("Name {0} Id {1} Tag {2}", _wfSub.Name, _wfSub.Id, _wfSub.Tag);

                        WorkflowDefinition _wfDef = _wfDepSrv.GetDefinition(_wfSub.DefinitionId);
                        clientContext.Load(_wfDef);
                        clientContext.ExecuteQueryRetry();

                        //LogVerbose(_wfDef.Xaml);
                    }

                    if (_list.Title.ToLowerInvariant().IndexOf("workflows") > -1)
                    {
                        FileCollection _files = _list.RootFolder.Files;
                        FolderCollection _folders = _list.RootFolder.Folders;
                        clientContext.Load(_list, s => s.RootFolder);
                        clientContext.Load(_files);
                        clientContext.Load(_folders);
                        clientContext.ExecuteQueryRetry();


                        LogVerbose(_list.RootFolder.ServerRelativeUrl);

                        foreach (Microsoft.SharePoint.Client.File _fileVV in _files)
                        {
                            var isXomlFile = _fileVV.Name.IndexOf(".xoml") > -1;
                            LogVerbose($">> processWorkflows [file] ==> {_fileVV.Name} is XOML file == ${isXomlFile}");
                            if (isXomlFile)
                            {
                                ProcessPage(clientContext, _fileVV.Name, scanning, true);
                            }
                        }

                        foreach (Microsoft.SharePoint.Client.Folder _fl in _folders)
                        {
                            FileCollection _filesVV = _fl.Files;
                            clientContext.Load(_filesVV);
                            clientContext.ExecuteQueryRetry();

                            foreach (Microsoft.SharePoint.Client.File _fileVV in _filesVV)
                            {
                                var isXomlFile = _fileVV.Name.IndexOf(".xoml") > -1;
                                if (isXomlFile)
                                {
                                    LogVerbose($">> processWorkflows [file] ==> {_fileVV.Name} is XOML file == ${isXomlFile}");
                                    ProcessPage(clientContext, _fileVV.Name, scanning, true);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogWarning("Failed to process workflow {0}", e.Message);
            }
        }

        /// <summary>
        /// This requires <paramref name="addInWebUrl"/> which is the URL for the SharePoint Hosted Add-Ins
        /// </summary>
        /// <param name="addInWebUrl">The add-in address</param>
        /// <param name="appInstanceId">The app instance to pull from the store</param>
        internal ScanResults ProcessAddinsRequest(string addInWebUrl, string appInstanceId)
        {
            var _webUrl = TokenHelper.EnsureTrailingSlash(addInWebUrl);
            LogVerbose("Checking installed apps .. {0} .. ", addInWebUrl);
            ScanResults scanResult = null;

            // the application must be registered in the Tenant Admin area and have access to the app catalog
            var addInAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
            using (var addInSiteContext = addInAuthManager.GetAppOnlyAuthenticatedContext(addInWebUrl, Settings.AzureAd.SPClientID, Settings.AzureAd.SPClientSecret))
            {
                var _web = addInSiteContext.Web;
                addInSiteContext.Load(_web);
                addInSiteContext.ExecuteQueryRetry();

                var appInstanceGuid = Guid.Parse(appInstanceId);

                var scannedAddIn = ProcessAddinInstance(_web, appInstanceGuid, true);
                if (scannedAddIn.HostedType == AddInEnum.ProviderHosted)
                {
                    // This is provider hosted and can't be scanned but we can update its information
                    scanResult = new ScanResults(scannedAddIn.AppRedirectUrl)
                    {
                        SPAddIn = true
                    };
                    scanResult.AddIns.Add(scannedAddIn);
                    if (scannedAddIn.HasPermissions)
                    {
                        var scanModel = new ScanModels(AddInObjectTypeEnum.AppInstance);
                        var pdx = 0;
                        foreach (var permission in scannedAddIn.AppPermissions)
                        {
                            pdx++;
                            scanModel.SetPermissionLines(pdx, permission);
                        }
                        scanResult.Scanned.Add(scanModel);
                    }
                }
                else
                {
                    scanResult = ProcessSiteCollection(scannedAddIn, addInWebUrl, appInstanceId);
                }

            }

            return scanResult;
        }

        /// <summary>
        /// Process the appinstance
        /// </summary>
        /// <param name="_web"></param>
        /// <param name="appInstanceId"></param>
        internal ScanAddInModel ProcessAddinInstance(Web _web, Guid appInstanceId, bool evaluateFullDepth = false)
        {
            var _webUrl = TokenHelper.EnsureTrailingSlash(_web.Url);
            LogVerbose($"Checking installed app .. {_webUrl} .. {appInstanceId}");

            AppInstance appInstance = _web.GetAppInstanceById(appInstanceId);
            _web.Context.Load(appInstance);
            _web.Context.ExecuteQueryRetry();


            var appDetails = new ScanAddInModel(_webUrl, appInstance);

            if (!string.IsNullOrEmpty(appInstance.RemoteAppUrl)
                || (!string.IsNullOrEmpty(appInstance.StartPage) && !_addInTokens.Any(tok => appInstance.StartPage.IndexOf(tok, StringComparison.InvariantCultureIgnoreCase) > -1)))
            {
                appDetails.HostedType = AddInEnum.ProviderHosted;
            }
            else
            {
                appDetails.HostedType = AddInEnum.SharePointHosted;
            }

            if (evaluateFullDepth)
            {
                try
                {
                    // Pulls from the Store and App Catalog
                    var appInstanceDetails = AppCatalog.GetAppDetails(_web.Context, _web, appInstance);
                    _web.Context.Load(appInstanceDetails, actx => actx.EulaUrl, actx => actx.PrivacyUrl, actx => actx.Publisher, actx => actx.ShortDescription, actx => actx.SupportUrl);
                    _web.Context.ExecuteQueryRetry();

                    if (appInstanceDetails != null && appInstanceDetails.ServerObjectIsNull == false)
                    {
                        appDetails.LoadAppDetails(appInstanceDetails);
                    }
                }
                catch (Exception appDetailEx)
                {
                    LogWarning("Failed to retreive app {0} instance details {1}", appInstanceId, appDetailEx.Message);
                }

                try
                {
                    // Pulls from App Instance Permission XML
                    var appInstancePermissions = AppCatalog.GetAppPermissionDescriptions(_web.Context, _web, appInstance);
                    _web.Context.ExecuteQueryRetry();

                    if (appInstancePermissions != null && appInstancePermissions.Value != null)
                    {
                        appDetails.LoadAppPermissions(appInstancePermissions.Value);
                    }
                }
                catch (Exception appPermEx)
                {
                    LogWarning("Failed to retreive app {0} instance permissions {1}", appInstanceId, appPermEx.Message);
                }
            }

            LogVerbose("App Details {0}", appDetails.ToString());
            return appDetails;
        }

        /// <summary>
        /// Process the appinstance
        /// </summary>
        /// <param name="_web"></param>
        /// <param name="appInstanceId"></param>
        internal void ProcessAddinInstance(Web _web, Guid appInstanceId, ScanResults scanning)
        {
            var _webUrl = TokenHelper.EnsureTrailingSlash(_web.Url);
            var _pagrPath = string.Format("{0}AppInstance/{1}", _webUrl, appInstanceId.ToString("N")).ToLower();

            if (scanning.Scanned.Any(sm => sm.ObjectUrl == _pagrPath))
            {
                // we previously scanned this file, lets skip it
                return;
            }

            // build the scanning result
            var scannedModel = new ScanModels(AddInObjectTypeEnum.AppInstance, _pagrPath);

            try
            {
                // scan the app instance for full set of details and permissions
                var appDetails = ProcessAddinInstance(_web, appInstanceId, true);
                if (appDetails.HasPermissions)
                {
                    var adx = 0;
                    foreach (var appInstancePermission in appDetails.AppPermissions)
                    {
                        adx++;
                        var appEvaluation = EvaluatePermissionsFlag(appInstancePermission);
                        if (!string.IsNullOrEmpty(appEvaluation) && scannedModel.SetPermissionLines(adx, appEvaluation))
                        {

                        }
                    }

                    if (scannedModel.Violation || scannedModel.Evaluation || scannedModel.Permission)
                    {
                        LogWarning(scannedModel.ToString());
                    }
                    else
                    {
                        LogVerbose(scannedModel.ToString());
                    }

                    scanning.Scanned.Add(scannedModel);
                }

                scanning.AddIns.Add(appDetails);
            }
            catch (Exception e)
            {
                LogWarning("Error processing add-in instance {0}", e.Message);
                scanning.Messages.Add(new ScanLog(e.Message, true));
            }
        }

        /// <summary>
        /// Process the custom actions for the site
        /// </summary>
        /// <param name="webctx"></param>
        /// <param name="_site"></param>
        /// <param name="scanning"></param>
        internal void ProcessCustomAction(ClientContext webctx, Site _site, ScanResults scanning)
        {
            var _webUrl = TokenHelper.EnsureTrailingSlash(_site.Url);
            LogVerbose("Checking custom actions ..SITE.. {0} .. ", _webUrl);

            var customActions = _site.GetCustomActions();
            foreach (var customAction in customActions)
            {
                ProcessCustomAction(webctx, _webUrl, scanning, customAction, AddInScopeEnum.Site);
            }
        }

        /// <summary>
        /// Process the custom actions for the web
        /// </summary>
        /// <param name="webctx"></param>
        /// <param name="_web"></param>
        /// <param name="scanning"></param>
        internal void ProcessCustomAction(ClientContext webctx, Web _web, ScanResults scanning)
        {
            var _webUrl = TokenHelper.EnsureTrailingSlash(_web.Url);
            LogVerbose("Checking custom actions ..WEB.. {0} .. ", _webUrl);

            var customActions = _web.GetCustomActions();
            foreach (var customAction in customActions)
            {
                ProcessCustomAction(webctx, _webUrl, scanning, customAction, AddInScopeEnum.Web);
            }
        }

        /// <summary>
        /// Evaluates the custom action ScriptBlock | ScriptSrc for proper results
        /// </summary>
        /// <param name="webctx"></param>
        /// <param name="_webUrl"></param>
        /// <param name="scanning"></param>
        /// <param name="customAction"></param>
        /// <param name="scope"></param>
        internal void ProcessCustomAction(ClientContext webctx, string _webUrl, ScanResults scanning, UserCustomAction customAction, AddInScopeEnum scope)
        {
            var _pagrPath = string.Format("{0}CustomAction/{1}", _webUrl, customAction.Id.ToString("N")).ToLower();

            if (scanning.Scanned.Any(sm => sm.ObjectUrl == _pagrPath))
            {
                // we previously scanned this file, lets skip it
                return;
            }

            // build the scanning result
            var scannedModel = new ScanModels(AddInObjectTypeEnum.CustomAction, _pagrPath);
            var dbcustomAction = new ScanCustomActionModel(customAction, scope);
            var uri = new Uri(_webUrl);

            try
            {
                if (!string.IsNullOrEmpty(customAction.ScriptBlock))
                {
                    using (StringReader _st = new StringReader(customAction.ScriptBlock))
                    {
                        scannedModel = ProcessPageParsing(_st, scannedModel, false);
                    }

                    if (scannedModel.Violation || scannedModel.Evaluation || scannedModel.Permission)
                    {
                        LogWarning(scannedModel.ToString());
                        scanning.HasClientCode = true;
                    }
                    else
                    {
                        LogVerbose(scannedModel.ToString());
                    }

                    scanning.Scanned.Add(scannedModel);
                }

                if (!string.IsNullOrEmpty(customAction.ScriptSrc))
                {
                    // grab the file and parse it
                    var scriptUri = new Uri(customAction.ScriptSrc);
                    ProcessPage(webctx, scriptUri.MakeRelativeUri(uri).ToString(), scanning);
                }
            }
            catch (Exception e)
            {
                LogWarning("Error processing custom actions {0}", e.Message);
                scanning.Messages.Add(new ScanLog(e.Message, true));
            }

            scanning.CustomActions.Add(dbcustomAction);
        }

        /// <summary>
        /// Enumerate through all lists and libraries.. parse through the aspx and .js file and look for flags
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="scanning"></param>
        internal void ParseLists(ClientContext _webContext, ScanResults scanning)
        {
            ///////////////////////// Pages
            List<string> _processFolders = new List<string> { };

            var _web = _webContext.Web;
            ListCollection _lists = _web.Lists;
            FolderCollection _folders = _web.RootFolder.Folders;
            FileCollection _files = _web.RootFolder.Files;
            _webContext.Load(_web);
            _webContext.Load(_lists);
            _webContext.Load(_folders);
            _webContext.Load(_files);
            _webContext.ExecuteQueryRetry();


            foreach (List _list in _lists)
            {
                var _listTitle = _list.Title.ToLower();
                ContentTypeCollection _cts = _list.ContentTypes;
                _webContext.Load(_cts);
                _webContext.ExecuteQueryRetry();

                bool _ctFound = false;

                LogVerbose("Checking list ... {0}", _listTitle);
                scanning.Messages.Add(new ScanLog("Checking list ... " + _listTitle));

                if (_inList.Contains(_listTitle))
                {
                    _ctFound = true;
                }
                else if (!_exList.Contains(_listTitle))
                {
                    foreach (ContentType _ct in _cts)
                    {
                        if (_ct.Name.ToLower().IndexOf("page") > -1)
                        {
                            _ctFound = true;
                            break;
                        }
                    }
                }

                if (_ctFound)
                {
                    _processFolders.Add(_list.Title.ToLower());
                    ProcessDocLib(_webContext, _list, scanning);
                }
            }


            foreach (Folder _folder in _folders)
            {
                var _folderName = _folder.Name.ToLower();
                LogVerbose("Checking folder ... {0}", _folderName);
                if ((!_exList.Contains(_folderName) && (!_processFolders.Contains(_folderName))))
                {
                    _processFolders.Add(_folderName);
                    ProcessFolder(_webContext, _folder, scanning);
                }
            }

            foreach (Microsoft.SharePoint.Client.File _file in _files)
            {
                ProcessPage(_webContext, _file.ServerRelativeUrl, scanning);
            }
        }

        internal void ProcessFilesAndFolders(ClientContext _webContext, ScanResults scanning)
        {
            var _web = _webContext.Web;
            FolderCollection _folders = _web.RootFolder.Folders;
            FileCollection _files = _web.RootFolder.Files;
            _webContext.Load(_folders);
            _webContext.Load(_files);
            _webContext.ExecuteQueryRetry();

            foreach (Microsoft.SharePoint.Client.File _file in _files)
            {
                ProcessPage(_webContext, _file.ServerRelativeUrl, scanning);
            }

            foreach (Folder _folder in _folders)
            {
                if (!_exList.Contains(_folder.Name.ToLower()))
                {
                    ProcessFolder(_webContext, _folder, scanning);
                }
            }
        }

        internal void ProcessFolder(ClientContext _webContext, Folder _folder, ScanResults scanning)
        {
            var _files = _folder.Files;
            var _folders = _folder.Folders;
            _webContext.Load(_files);
            _webContext.Load(_folders);
            _webContext.ExecuteQueryRetry();

            foreach (var _file in _files)
            {
                ProcessPage(_webContext, _file.ServerRelativeUrl, scanning);
            }

            foreach (var _folderX in _folders)
            {
                if (!_exList.Contains(_folder.Name.ToLower()))
                {
                    ProcessFolder(_webContext, _folderX, scanning);
                }
            }

        }

        /// <summary>
        /// Process the document library and read from its root folder
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="_docLib"></param>
        /// <param name="scanning"></param>
        internal void ProcessDocLib(ClientContext _webContext, List _docLib, ScanResults scanning)
        {
            _webContext.Load(_docLib, s => s.RootFolder);
            _webContext.ExecuteQueryRetry();

            var _msg = string.Format(">> processDocLib: {0} -- {1}", _docLib.Title, _docLib.RootFolder.ServerRelativeUrl);
            LogVerbose(_msg);
            scanning.Messages.Add(new ScanLog(_msg));

            FileCollection _files = _docLib.RootFolder.Files;
            FolderCollection _folders = _docLib.RootFolder.Folders;
            _webContext.Load(_files);
            _webContext.Load(_folders);
            _webContext.ExecuteQueryRetry();

            foreach (Microsoft.SharePoint.Client.File _file in _files)
            {
                ProcessPage(_webContext, _file.ServerRelativeUrl, scanning);
            }

            foreach (Microsoft.SharePoint.Client.Folder _folder in _folders)
            {
                ProcessFolders(_webContext, _docLib, _folder, scanning);
            }
        }

        /// <summary>
        /// Process the folder and any child folders
        /// </summary>
        /// <param name="_webContext"></param>
        /// <param name="_docLib"></param>
        /// <param name="_folder"></param>
        /// <param name="scanning"></param>
        internal void ProcessFolders(ClientContext _webContext, List _docLib, Folder _folder, ScanResults scanning)
        {

            FolderCollection _folders = _folder.Folders;
            FileCollection _files = _folder.Files;
            _webContext.Load(_docLib, s => s.RootFolder);
            _webContext.Load(_folders);
            _webContext.Load(_files);
            _webContext.ExecuteQueryRetry();

            foreach (Microsoft.SharePoint.Client.File _file in _files)
            {
                ProcessPage(_webContext, _file.ServerRelativeUrl, scanning);
            }

            foreach (Microsoft.SharePoint.Client.Folder _folderX in _folders)
            {
                ProcessFolders(_webContext, _docLib, _folderX, scanning);
            }

        }

        /// <summary>
        /// Parse the page for scripting or other utilities
        /// </summary>
        /// <param name="_webContext"></param>
        /// <param name="_pagrPath"></param>
        /// <param name="scanning"></param>
        /// <returns></returns>
        internal bool ProcessPage(ClientContext _webContext, string _pagrPath, ScanResults scanning, bool evaluatePermissions = false)
        {
            _pagrPath = _pagrPath.ToLower();

            if (scanning.Scanned.Any(sm => sm.ObjectUrl == _pagrPath))
            {
                // we previously scanned this file, lets skip it
                return true;
            }

            // construct directory path to which we'll write the files
            var logDir = new System.IO.DirectoryInfo(_resultLogDirectory.FullName);

            // build scanning model
            var scannedModel = new ScanModels(AddInObjectTypeEnum.File, _pagrPath);
            scanning.Messages.Add(new ScanLog("Checking file ... " + _pagrPath));

            if (Opts.EvaluateLibraries && IsViolationFile(_pagrPath))
            {
                // we want to flag libraries which allow a developer to make web service calls
                scannedModel.SetEvaluationLines(0, Path.GetFileName(_pagrPath));
                LogWarning(scannedModel.ToString());
                scanning.Scanned.Add(scannedModel);
            }
            else if (IsExcludedFile(_pagrPath))
            {
                scannedModel.IsExcluded = true;

                LogVerbose(scannedModel.ToString());
                scanning.Scanned.Add(scannedModel);
                return false;
            }
            else
            {
                try
                {
                    if (!string.IsNullOrEmpty(Settings.AzureAd.SPClientID))
                    {
                        // App Key safe client context
                        var pageFile = _webContext.Web.GetFileByServerRelativeUrl(_pagrPath);
                        _webContext.Load(pageFile);
                        var binaryStream = pageFile.OpenBinaryStream();
                        _webContext.ExecuteQueryRetry();


                        if (Opts.WriteFilesToDisk)
                        {
                            var baseDir = _pagrPath.ToLower().Replace(scanning.Url.ToLower(), string.Empty).Replace(scanning.ServerRelativeUrl.ToLower(), string.Empty);
                            var spfile = new System.IO.FileInfo($"{_resultLogDirectory}\\{scanning.WebId}{baseDir.Replace(@"/", @"\")}");
                            var filePath = System.IO.Directory.CreateDirectory(spfile.DirectoryName, logDir.GetAccessControl());

                            using var fileStream = new System.IO.FileStream(spfile.FullName, FileMode.Create, FileAccess.Write);
                            binaryStream.Value.Seek(0, SeekOrigin.Begin);
                            binaryStream.Value.CopyTo(fileStream);

                            binaryStream.Value.Seek(0, SeekOrigin.Begin);
                            using StreamReader _st = new StreamReader(binaryStream.Value);
                            scannedModel = ProcessPageParsing(_st, scannedModel, evaluatePermissions);
                        }
                        else
                        {
                            binaryStream.Value.Seek(0, SeekOrigin.Begin);
                            using StreamReader _st = new StreamReader(binaryStream.Value);
                            scannedModel = ProcessPageParsing(_st, scannedModel, evaluatePermissions);
                        }
                    }
                    else
                    {
                        using FileInformation fInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(_webContext, _pagrPath);
                        using StreamReader _st = new StreamReader(fInfo.Stream);
                        scannedModel = ProcessPageParsing(_st, scannedModel, evaluatePermissions);
                    }


                    if (scannedModel.Violation || scannedModel.Evaluation || scannedModel.Permission)
                    {
                        LogWarning(scannedModel.ToString());
                        scanning.HasClientCode = true;
                    }
                    else
                    {
                        LogVerbose(scannedModel.ToString());
                    }

                    scanning.Scanned.Add(scannedModel);
                }
                catch (Exception e)
                {
                    LogWarning("Error processing pages {0}", e.Message);
                    scanning.Messages.Add(new ScanLog(e.Message, true));
                }
            }

            // return true if either of these is true
            return (scannedModel.Violation || scannedModel.Evaluation);
        }

        /// <summary>
        /// Parse the lines of the page
        /// </summary>
        /// <param name="_st"></param>
        /// <param name="scannedModel"></param>
        /// <param name="evaluatePermissions"></param>
        /// <returns></returns>
        private ScanModels ProcessPageParsing(TextReader _st, ScanModels scannedModel, bool evaluatePermissions = false)
        {
            var linedx = 0;
            var line = string.Empty;
            while (_st.Peek() >= 0)
            {
                while ((line = _st.ReadLine()) != null)
                {
                    linedx++;
                    var lowerline = line.ToLower();
                    var _checkString = ViolationFlag(lowerline);
                    if (!string.IsNullOrEmpty(_checkString)
                        && scannedModel.SetViolationLines(linedx, _checkString))
                    {
                    }

                    _checkString = EvaluateFlag(lowerline);
                    if (!string.IsNullOrEmpty(_checkString)
                        && scannedModel.SetEvaluationLines(linedx, _checkString))
                    {
                    }

                    if (evaluatePermissions)
                    {
                        _checkString = EvaluatePermissionsFlag(lowerline);
                        if (!string.IsNullOrEmpty(_checkString)
                            && scannedModel.SetPermissionLines(linedx, _checkString))
                        {
                        }
                    }
                }
            }

            scannedModel.NumberOfLines = linedx;
            return scannedModel;
        }

        /// <summary>
        /// Look for file name to match known libraries
        /// </summary>
        /// <param name="_pagrPath"></param>
        /// <returns></returns>
        internal bool IsViolationFile(string _pagrPath)
        {
            _pagrPath = _pagrPath.ToLower();
            if (_pagrPath.IndexOf("jquery") > -1
                || _pagrPath.IndexOf("angular") > -1
                || _pagrPath.IndexOf("knockout") > -1
                || _pagrPath.IndexOf("discoveraccess") > -1
                || _pagrPath.IndexOf("react.js") > -1
                || _pagrPath.IndexOf("react-dom.js") > -1)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Look for file extensions we do not want to inspect
        /// </summary>
        /// <param name="_pagrPath"></param>
        /// <returns></returns>
        internal bool IsExcludedFile(string _pagrPath)
        {
            _pagrPath = _pagrPath.ToLower();
            var _isExcluded = false;

            if (_pagrPath.IndexOf("jquery") > -1
                || _pagrPath.IndexOf("angular") > -1
                || _pagrPath.IndexOf("knockout") > -1
                || _pagrPath.IndexOf("react") > -1
                || _pagrPath.IndexOf("discoveraccess") > -1
                || _pagrPath.IndexOf(".js.map") > -1
                || _pagrPath.IndexOf(".ts") > -1)
            {
                _isExcluded = true;
            }

            if ((_pagrPath.IndexOf(".html") < 0) && (_pagrPath.IndexOf(".aspx") < 0) && (_pagrPath.IndexOf(".js") < 0))
            {
                _isExcluded = true;
            }

            return _isExcluded;
        }

        internal string EvaluatePermissionsFlag(string _line)
        {
            var _finalFlag = "";

            if (_line.ToLower().IndexOf("external connections") > -1)
            {
                _finalFlag += "External Connections|";
            }

            if (_line.ToLower().IndexOf("let it edit or delete documents and list items in all site collections") > -1)
            {
                _finalFlag += "Site Collection|";
            }

            if (_line.ToLower().IndexOf("let it have full control of this site collection") > -1)
            {
                _finalFlag += "Site Collection|";
            }

            //if (_line.ToLower().IndexOf("users of this site") > -1)
            //{
            //    _finalFlag += "Users of this Site|";
            //}

            if (_line.ToLower().IndexOf("let it share its permissions with other users") > -1)
            {
                _finalFlag += "Other Users|";
            }

            if (_line.ToLower().IndexOf("microblogging") > -1)
            {
                _finalFlag += "Microblogging|";
            }

            if (_line.ToLower().IndexOf("managed metadata") > -1)
            {
                _finalFlag += "Managed Metadata|";
            }

            if (_line.ToLower().IndexOf("startworkflow") > -1)
            {
                _finalFlag += "Workfow|";
            }

            return _finalFlag;
        }

        /// <summary>
        /// Look for Web Services executions that might consumed invalid sources but may be acceptable
        /// </summary>
        /// <param name="_pagrPath"></param>
        /// <param name="_line"></param>
        /// <returns></returns>
        internal string EvaluateFlag(string _line)
        {
            string _finalFlag = "";
            if (_line.ToLower().IndexOf(".ajax(") > -1 // jQuery
                || _line.ToLower().IndexOf("$http->") > -1 // ReactJS
                || _line.ToLower().IndexOf("$http(") > -1 // Angular 1.x
                || _line.ToLower().IndexOf(".http.") > -1 // Angular 2.x
                || _line.ToLower().IndexOf("HTTP_PROVIDERS") > -1 // Angular TS
                )
            {
                _finalFlag = "http|";
            }

            return _finalFlag;
        }

        /// <summary>
        /// Look for Web Services that might be consumed
        /// </summary>
        /// <param name="_line"></param>
        /// <returns></returns>
        internal string ViolationFlag(string _line)
        {
            _line = _line.ToLower();
            string _finalFlag = "";

            if ((_line.IndexOf(".setsinglevalueprofileproperty") > -1) || (_line.IndexOf(".setmultivaluedprofileproperty") > -1))
            {
                _finalFlag += "User Profile|";
            }

            if (_line.IndexOf(".setmyprofilepicture") > -1)
            {
                _finalFlag += "User Profile|";
            }

            if (_line.IndexOf("feedmanager") > -1)
            {
                _finalFlag += "Social Feed|";
            }

            if (_line.IndexOf("sp.social") > -1)
            {
                _finalFlag += "Social|";
            }

            if (_line.IndexOf("spservice") > -1)
            {
                _finalFlag += "SPService|";
            }

            if (Opts.EvaluateLibraries && _line.IndexOf("jquery") > -1)
            {
                _finalFlag += "JQuery|";
            }

            if (Opts.EvaluateLibraries && _line.IndexOf("discoveraccessconnector") > -1)
            {
                _finalFlag += "DiscoverAccessConnector|";
            }

            if (_line.IndexOf("sp.workflow") > -1)
            {
                _finalFlag += "Workflow|";
            }

            //if (_line.IndexOf("sp.clientcontext(") > -1)
            //{
            //    _finalFlag += "SP.ClientContext|";
            //    LogVerbose(_line);
            //}

            return _finalFlag;
        }

        /// <summary>
        /// Evaluate if scanning request exists for the Site Url
        /// </summary>
        /// <param name="applicationSiteContext"></param>
        /// <param name="applicationScanList"></param>
        /// <param name="_siteUrl"></param>
        /// <returns></returns>
        internal Nullable<int> CheckIfSiteExists(ClientContext applicationSiteContext, List applicationScanList, string _siteUrl)
        {
            var _siteId = default(Nullable<int>);

            try
            {
                CamlQuery camlQuery = new CamlQuery
                {
                    ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, CAML.Where(CAML.Eq(CAML.FieldValue("Title", "Text", _siteUrl))), "", "", 50)
                };
                ListItemCollectionPosition itemPosition = null;

                while (true)
                {
                    camlQuery.ListItemCollectionPosition = itemPosition;
                    ListItemCollection listItems = applicationScanList.GetItems(camlQuery);
                    applicationSiteContext.Load(listItems);
                    applicationSiteContext.ExecuteQuery();

                    itemPosition = listItems.ListItemCollectionPosition;
                    foreach (ListItem listItem in listItems)
                    {
                        _siteId = listItem.Id;
                        break;
                    }

                    if (itemPosition == null)
                    {
                        break;
                    }
                }
            }
            catch (Exception lex)
            {
                LogWarning("Failed to query {0} with message {1}", _siteUrl, lex.Message);
            }

            return _siteId;
        }

        /// <summary>
        /// parse scan results into attachments of the scan requests
        /// </summary>
        /// <param name="applicationSiteContext"></param>
        /// <param name="applicationScanList"></param>
        /// <param name="scanLog"></param>
        /// <param name="scanSiteUrl">The site URL for the <paramref name="scanLog"/> entity</param>
        /// <param name="scanItemId"></param>
        internal void AddFlaggedSitetoList(ClientContext applicationSiteContext, List applicationScanList, ScanResults scanLog, string scanSiteUrl, Nullable<int> scanItemId = null)
        {
            if (scanLog == null)
            {
                // exception was caught and there is an issue with permissions or authorization
                LogWarning("An exception occurred while scanning the site {0}", scanSiteUrl);
                return;
            }

            var _siteUrl = scanLog.Url;

            // write the logs to disc
            var logfiles = WriteScanLogsToDisk(scanLog);

            // Default to writing log as attachment
            if (Opts.SkipLog)
            {
                LogWarning("We will not write the log or violations to the Application Scan list for {0}", _siteUrl);
                return;
            }
            else
            {

                if (!scanItemId.HasValue)
                {
                    scanItemId = CheckIfSiteExists(applicationSiteContext, applicationScanList, _siteUrl);
                }

                LogVerbose(">> Scan Logs uploading for >> {0} >> scan log exists [{1}]", _siteUrl, scanItemId.HasValue);

                try
                {
                    // this should be the start Site URL or the Add-In full appweburl
                    var _regionSiteType = GetRegionSiteType(_siteUrl);
                    var _region = _regionSiteType.Region;

                    ListItem applicationScanItem = null;
                    if (!scanItemId.HasValue)
                    {
                        ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                        applicationScanItem = applicationScanList.AddItem(itemCreateInfo);
                        applicationScanItem["Title"] = _siteUrl;
                    }
                    else
                    {
                        applicationScanItem = applicationScanList.GetItemById(scanItemId.Value);
                        applicationSiteContext.Load(applicationScanItem);
                        applicationSiteContext.ExecuteQueryRetry();
                    }

                    // Process List Item
                    applicationScanItem["Violation"] = scanLog.Violations.Count();
                    applicationScanItem["Region"] = _region.ToUpper().Trim();
                    applicationScanItem["Flagged"] = scanLog.Violations.Any() || scanLog.Evaluations.Any() || scanLog.Permissions.Any() || scanLog.AppType == AddInEnum.ProviderHosted;
                    applicationScanItem["Components"] = scanLog.AppComponents;
                    applicationScanItem["Tenant"] = scanLog.SiteTenant.ToString("f");
                    applicationScanItem["RowProcessed"] = true;
                    applicationScanItem.Update();
                    applicationSiteContext.ExecuteQueryRetry();


                    applicationScanItem.EnsureProperties(asi => asi.AttachmentFiles);

                    foreach (var logfile in logfiles)
                    {
                        AttachLogToScannedLog(applicationSiteContext, applicationScanItem, logfile);
                    }

                }
                catch (Exception ex)
                {
                    LogWarning("Failed to process scan log {0}", ex.Message);
                }
            }
        }

        internal bool AttachLogToScannedLog(ClientContext applicationSiteContext, ListItem applicationScanItem, string fileForSiteLog)
        {
            var filenameForSiteLog = Path.GetFileName(fileForSiteLog);

            try
            {
                if (!applicationScanItem.AttachmentFiles.Any(af => af.FileName == filenameForSiteLog))
                {
                    LogVerbose("[Item:{0} ... Attaching file {1}", applicationScanItem.Id, filenameForSiteLog);
                    using (var stream = new System.IO.FileStream(fileForSiteLog, System.IO.FileMode.Open))
                    {
                        AttachmentCreationInformation attachFileInfo = new AttachmentCreationInformation
                        {
                            ContentStream = stream,
                            FileName = filenameForSiteLog
                        };
                        var newattachment = applicationScanItem.AttachmentFiles.Add(attachFileInfo);
                        applicationSiteContext.Load(newattachment, ntt => ntt.FileName, ntt => ntt.ServerRelativeUrl);
                        applicationSiteContext.ExecuteQueryRetry();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogWarning("Failed to attach file {0} with message {1}", filenameForSiteLog, ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Write results to log file
        /// </summary>
        /// <param name="scannedSite"></param>
        /// <returns></returns>
        internal List<string> WriteScanLogsToDisk(ScanResults scannedSite)
        {
            var scanurl = new Uri(scannedSite.Url);
            var scanlogs = new List<string>();
            var runtimedate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");

            try
            {
                // Write all lines to text file
                var lines = new List<string>
                {
                    "-------------------------------------------------",
                    "--------------------  Messages ------------------",
                    "-------------------------------------------------"
                };
                scannedSite.Messages.ToList().ForEach(comp => lines.Add(comp.ToString()));

                lines.Add("-------------------------------------------------");
                lines.Add("--------------------  Evaluations ---------------");
                lines.Add("-------------------------------------------------");
                scannedSite.Evaluations.ToList().ForEach(err => lines.Add(err.ToString()));
                if (scannedSite.SubSites.Any())
                {
                    scannedSite.SubSites.ForEach(ssite =>
                    {
                        ssite.Evaluations.ToList().ForEach(err => lines.Add(err.ToString()));
                    });
                }

                lines.Add("-------------------------------------------------");
                lines.Add("--------------------  Failures ------------------");
                lines.Add("-------------------------------------------------");
                scannedSite.Violations.ToList().ForEach(err => lines.Add(err.ToString()));
                if (scannedSite.SubSites.Any())
                {
                    scannedSite.SubSites.ForEach(ssite =>
                    {
                        ssite.Violations.ToList().ForEach(err => lines.Add(err.ToString()));
                    });
                }

                lines.Add("-------------------------------------------------");
                lines.Add("--------------------  Clean ---------------------");
                lines.Add("-------------------------------------------------");
                scannedSite.Clean.ToList().ForEach(msg => lines.Add(msg.ToString()));


                var txtlogfile = $"{_resultLogDirectory}\\{scannedSite.WebId}_log_{runtimedate}.txt";
                System.IO.File.WriteAllLines(txtlogfile, lines);
                scanlogs.Add(txtlogfile);
            }
            catch (Exception ex)
            {
                LogWarning("Failed to write log file {0}", ex.Message);
            }

            try
            {
                // serialize JSON to a string and then write string to a file
                var jsonlogfile = $"{_resultLogDirectory}\\{scannedSite.WebId}_log_{runtimedate}_json.txt";
                System.IO.File.WriteAllText(jsonlogfile, Newtonsoft.Json.JsonConvert.SerializeObject(scannedSite));
                scanlogs.Add(jsonlogfile);
            }
            catch (Exception jsonex)
            {
                LogWarning("Failed to write JSON file {0}", jsonex.Message);
            }

            try
            {
                // write HTML version for quick links

                var lines = new List<string>
                {
                    string.Format("<html><head><title>Scanned Results {0}</title></head><body>", scanurl),
                    "<br>-------------------------------------------------",
                    "<br>----------------  Scanned Site ------------------",
                    "<br>-------------------------------------------------",
                    string.Format("<br><a href=\"{0}\" title=\"link to site\">{0}</a>", scanurl)
                };
                scannedSite.Messages.ToList().ForEach(comp => lines.Add($"<br />{comp}"));

                if (scannedSite.AddIns.Any())
                {
                    lines.Add("<br>-----------------------------------------");
                    lines.Add("<br>----------------  Add-Ins ---------------");
                    lines.Add("<br>-----------------------------------------");
                    foreach (var scannedAddIn in scannedSite.AddIns)
                    {
                        lines.Add(string.Format("<br>----  Title: {0}", scannedAddIn.Title));
                        lines.Add(string.Format("<br>----  Hosted Type: {0}", scannedAddIn.HostedType.ToString("f")));
                        lines.Add(string.Format("<br>----  Redirect Url: <a href=\"{0}\" title=\"link to site\">{0}</a>", scannedAddIn.AppRedirectUrl));
                        if (scannedAddIn.HostedType == AddInEnum.SharePointHosted)
                        {
                            lines.Add(string.Format("<br>----  App Web URL: <a href=\"{0}\" title=\"link to site\">{0}</a>", scannedAddIn.AppWebFullUrl));
                        }
                        if (scannedAddIn.HostedType == AddInEnum.ProviderHosted)
                        {
                            lines.Add(string.Format("<br>----  Remote Url: {0}", scannedAddIn.RemoteAppUrl));
                            lines.Add(string.Format("<br>----  Start Url: {0}", scannedAddIn.StartPage));
                        }
                        if (!string.IsNullOrEmpty(scannedAddIn.SettingsPageUrl))
                        {
                            lines.Add(string.Format("<br>----  Settings Page Url: <a href=\"{0}\" title=\"link to site\">{0}</a>", scannedAddIn.SettingsPageUrl));
                        }
                        lines.Add(string.Format("<br>----  EULA: <a href=\"{0}\" title=\"link to site\">{0}</a>", scannedAddIn.EulaUrl));
                        lines.Add(string.Format("<br>----  Privacy URL: <a href=\"{0}\" title=\"link to site\">{0}</a>", scannedAddIn.PrivacyUrl));
                        lines.Add(string.Format("<br>----  Publisher: {0}", scannedAddIn.Publisher));
                        lines.Add(string.Format("<br>----  Support URL: <a href=\"{0}\" title=\"link to site\">{0}</a>", scannedAddIn.SupportUrl));
                        lines.Add(string.Format("<br>----  Short Description: {0}", scannedAddIn.ShortDescription));

                        if (scannedAddIn.HasPermissions)
                        {
                            lines.Add("<br>-------  Permissions ----------");
                            foreach (var permission in scannedAddIn.AppPermissions)
                            {
                                lines.Add(string.Format("<br>----------------  {0}", permission));
                            }
                        }
                    }
                }

                if (scannedSite.Evaluations.Any(ev => ev.ObjectType == AddInObjectTypeEnum.File))
                {
                    lines.Add("<br>-------------------------------------------------");
                    lines.Add("<br>--------------------  Evaluations ---------------");
                    lines.Add("<br>-------------------------------------------------");
                    foreach (var evaluation in scannedSite.Evaluations.Where(ev => ev.ObjectType == AddInObjectTypeEnum.File))
                    {
                        var evaluri = new Uri(scanurl, evaluation.ObjectUrl);
                        lines.Add(string.Format("<br>----  {2}: <a href=\"{0}\">{1}</a>", evaluri, evaluation.ObjectUrl, evaluation.ObjectType.ToString("f")));
                    }

                    if (scannedSite.SubSites.Any(s => s.Evaluations.Any(ev => ev.ObjectType == AddInObjectTypeEnum.File)))
                    {
                        scannedSite.SubSites.ForEach(ssite =>
                        {
                            foreach (var evaluation in ssite.Evaluations.Where(ev => ev.ObjectType == AddInObjectTypeEnum.File))
                            {
                                var evaluri = new Uri(scanurl, evaluation.ObjectUrl);
                                lines.Add(string.Format("<br>------------  {2}: <a href=\"{0}\">{1}</a>", evaluri, evaluation.ObjectUrl, evaluation.ObjectType.ToString("f")));
                            }
                        });
                    }
                }

                if (scannedSite.Violations.Any(ev => ev.ObjectType == AddInObjectTypeEnum.File))
                {
                    lines.Add("-------------------------------------------------");
                    lines.Add("--------------------  Failures ------------------");
                    lines.Add("-------------------------------------------------");
                    foreach (var violation in scannedSite.Violations.Where(ev => ev.ObjectType == AddInObjectTypeEnum.File))
                    {
                        var viouri = new Uri(scanurl, violation.ObjectUrl);
                        lines.Add(string.Format("<br>----  {2}: <a href=\"{0}\">{1}</a>", viouri, violation.ObjectUrl, violation.ObjectType.ToString("f")));
                    }

                    if (scannedSite.SubSites.Any(s => s.Violations.Any(ev => ev.ObjectType == AddInObjectTypeEnum.File)))
                    {
                        scannedSite.SubSites.ForEach(ssite =>
                        {
                            foreach (var violation in ssite.Violations.Where(ev => ev.ObjectType == AddInObjectTypeEnum.File))
                            {
                                var viouri = new Uri(scanurl, violation.ObjectUrl);
                                lines.Add(string.Format("<br>------------  {2}: <a href=\"{0}\">{1}</a>", viouri, violation.ObjectUrl, violation.ObjectType.ToString("f")));
                            }
                        });
                    }
                }

                // terminate the html body
                lines.Add("</body></html>");

                var htmllogfile = $"{_resultLogDirectory}\\{scannedSite.WebId}_log_{runtimedate}.html";
                System.IO.File.WriteAllLines(htmllogfile, lines);
                scanlogs.Add(htmllogfile);
            }
            catch (Exception htmlex)
            {
                LogWarning("Failed to write HTML file {0}", htmlex.Message);
            }

            return scanlogs;
        }


    }
}
