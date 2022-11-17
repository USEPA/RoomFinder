using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("getEPAQueryAppMonitor", HelpText = "Checks sharp scan site to evaluate app requests.")]
    public class GetEPAQueryAppMonitorOptions : TenantCommandOptions
    {
    }

    public static class GetEPAQueryAppMonitorExtension
    {
        /// <summary>
        /// Will execute the query epa app request monitor
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this GetEPAQueryAppMonitorOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAQueryAppMonitor(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Opens a web request and queries the specified list
    /// </summary>
    /// <remarks>
    /// If AssetID already in Sharp Request list -> do nothing
    /// If not, create new Sharp request item 
    /// </remarks>
    public class GetEPAQueryAppMonitor : BaseSpoTenantCommand<GetEPAQueryAppMonitorOptions>
    {
        public GetEPAQueryAppMonitor(GetEPAQueryAppMonitorOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        internal string TenantAdminUrl { get; set; }
        internal string ClientId { get; set; }
        internal string ClientSecret { get; set; }

        #endregion

        public override void OnInit()
        {
            TenantAdminUrl = Settings.Commands.TenantAdminUrl;
            ClientId = Settings.SpoAddInMakeEPASite.ClientId;
            ClientSecret = Settings.SpoAddInMakeEPASite.ClientSecret;

            LogVerbose($"AppCredential connecting and setting Add-In keys");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantAdminUrl), null, ClientId, ClientSecret, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            try
            {
                // Connect to report site and get & groups list
                var reportSiteURL = "https://usepa.sharepoint.com/sites/oei_Work/SharePointPM/ApplicationReview";
                var reportListName = "Application Requests";
                var reportSiteContext = this.ClientContext.Clone(reportSiteURL);
                var reportWeb = reportSiteContext.Web;
                var reportList = reportWeb.Lists.GetByTitle(reportListName);
                reportSiteContext.Load(reportWeb);
                reportSiteContext.Load(reportList);
                reportSiteContext.ExecuteQueryRetry();

                // Set connection to app catalog
                var appCatalogSiteURL = "https://usepa.sharepoint.com/sites/USEPA_APPS";
                var appCatalogListName = "App Requests";
                var appCatalogSiteContext = this.ClientContext.Clone(appCatalogSiteURL);
                var appCatalogWeb = appCatalogSiteContext.Web;
                var appCatalogList = appCatalogWeb.Lists.GetByTitle(appCatalogListName);
                appCatalogSiteContext.Load(appCatalogWeb);
                appCatalogSiteContext.Load(appCatalogList);
                appCatalogSiteContext.ExecuteQueryRetry();


                LogVerbose("App Catalog list: {0}", appCatalogList.Title);


                ListItemCollectionPosition itemPosition = null;

                var enumerateFlag = true;
                var viewFields = new string[]
                {
                    "Title",
                    "AppPublisher",
                    "AppRequester",
                    "AppRequestJustification",
                    "AppRequestIsSiteLicense",
                    "AppRequestPermissionXML",
                    "AppRequestStatus",
                    "AssetID"
                };
                var camlQuery = CamlQuery.CreateAllItemsQuery(100);
                camlQuery.ViewXml = CAML.ViewQuery(
                    ViewScope.RecursiveAll,
                    CAML.Where(CAML.Eq(CAML.FieldValue("AppRequestStatus", FieldType.Choice.ToString("f"), "New"))),
                    CAML.OrderBy(new OrderByField("Modified")),
                    CAML.ViewFields(viewFields.Select(s => CAML.FieldRef(s)).ToArray()),
                    100);

                // Query [Pending Apps] from app catalog
                while (enumerateFlag)
                {
                    camlQuery.ListItemCollectionPosition = itemPosition;
                    ListItemCollection appListItems = appCatalogList.GetItems(camlQuery);

                    appCatalogSiteContext.Load(appListItems);
                    appCatalogSiteContext.ExecuteQueryRetry();
                    itemPosition = appListItems.ListItemCollectionPosition;

                    foreach (var appListItem in appListItems)
                    {
                        var assetId = appListItem.RetrieveListItemValue("AssetID");
                        var appListItemTitle = appListItem.RetrieveListItemValue("Title");
                        var appRequestUser = appListItem.RetrieveListItemUserValue("AppRequester");
                        var requestStatus = appListItem.RetrieveListItemValue("AppRequestStatus");
                        var requestLicense = appListItem.RetrieveListItemValue("AppRequestIsSiteLicense");
                        var appPermissionXml = appListItem.RetrieveListItemValue("AppRequestPermissionXML");
                        var appRequestJustification = appListItem.RetrieveListItemValue("AppRequestJustification");

                        var requestEmail = (appRequestUser != null) ? appRequestUser.Email : string.Empty;
                        if (string.IsNullOrEmpty(requestEmail))
                        {
                            LogWarning("The app {0} requested by {1} has an invalid email address", assetId, appRequestUser.LookupValue);
                        }
                        else
                        {
                            // If status is New -> check Asset ID
                            if (requestStatus == "New" && !string.IsNullOrEmpty(assetId))
                            {
                                /// Check if the site has already been logged
                                var AppFound = default(Nullable<int>);
                                var camlRequestTrackerQuery = CamlQuery.CreateAllItemsQuery();
                                camlRequestTrackerQuery.ViewXml = CAML.ViewQuery(
                                    ViewScope.RecursiveAll,
                                    CAML.Where(CAML.Eq(CAML.FieldValue("AssetID", FieldType.Text.ToString("f"), assetId))),
                                    string.Empty,
                                    CAML.ViewFields(CAML.FieldRef("App_x0020_Status"), CAML.FieldRef("Status"), CAML.FieldRef("Requested_x0020_By"), CAML.FieldRef("Id")),
                                    10);

                                var userEmails = new List<string>();
                                var spTrackerItems = reportList.GetItems(camlRequestTrackerQuery);
                                reportSiteContext.Load(spTrackerItems);
                                reportSiteContext.ExecuteQueryRetry();
                                var spTrackerCount = spTrackerItems.Count();
                                if (spTrackerCount > 0)
                                {
                                    foreach (var spTrackerItem in spTrackerItems)
                                    {
                                        AppFound = spTrackerItem.Id;
                                        var requestedBy = spTrackerItem.RetrieveListItemUserValues("Requested_x0020_By");
                                        if (requestedBy != null)
                                        {
                                            // check if requesting user 
                                            if (!requestedBy.Any(a => a.Email == requestEmail))
                                            {
                                                userEmails.Add(requestEmail);
                                            }
                                        }
                                        break;
                                    }
                                }

                                var assetMessage = string.Format("{0}--{1}--{2}--{3}--{4} Found:{5} ##:{6}", assetId, appListItemTitle, requestStatus, requestEmail, requestLicense, AppFound, spTrackerCount);
                                if (this.ShouldProcess(string.Format("Should we persist the asset {0}?", assetMessage)))
                                {
                                    if (!AppFound.HasValue)
                                    {
                                        // Add app request to Sharp review list --> let list workflow determin who reviews..
                                        try
                                        {
                                            if (!string.IsNullOrEmpty(appPermissionXml))
                                            {
                                                var resultingPermissionXml = parseAppPermissions(appPermissionXml);
                                                appRequestJustification = appRequestJustification + "\n" + resultingPermissionXml;
                                            }

                                            var tmpusername = reportSiteContext.Web.EnsureUser(string.Format("{0}|{1}", ClaimIdentifier, appRequestUser.Email));

                                            var itemCreateInfo = new Microsoft.SharePoint.Client.ListItemCreationInformation();
                                            var newItem = reportList.AddItem(itemCreateInfo);
                                            newItem["Title"] = appListItemTitle;
                                            newItem["App_x0020_Status"] = "Pending";
                                            newItem["AssetID"] = assetId;
                                            newItem["Developed_x0020_By"] = "Third party";
                                            newItem["Developer_x0020__x0028_External_"] = appListItem.RetrieveListItemValue("AppPublisher");
                                            newItem["Application_x0020_Description"] = appRequestJustification;
                                            newItem["Hosted"] = "I don't know";
                                            newItem["Request_x0020_Date"] = appListItem.RetrieveListItemValue("Created").ToDateTime();
                                            newItem["Requested_x0020_By"] = tmpusername;
                                            newItem["Author"] = tmpusername;
                                            newItem.Update();


                                            LogWarning("Adding [{0}] Requested by {1}", assetId, appRequestUser.Email);
                                            reportSiteContext.Load(newItem);
                                            reportSiteContext.ExecuteQueryRetry();
                                        }
                                        catch (Exception ex)
                                        {
                                            LogError(ex, "Failed to add new item {0}", assetId);
                                        }
                                    }
                                    else if (AppFound.HasValue && userEmails.Any())
                                    {
                                        // App already requested and/or approved --> don't add app for Sharp review.. maybe update something in the request item?
                                        // ? Send notification to ? that app has been been requested again?
                                        try
                                        {
                                            var existingRequestItem = reportList.GetItemById(AppFound.Value);
                                            reportSiteContext.Load(existingRequestItem);
                                            reportSiteContext.ExecuteQueryRetry();

                                            var currentRequesters = new List<FieldUserValue>();
                                            // will ensure 
                                            var existingRequesters = existingRequestItem.RetrieveListItemUserValues("Requested_x0020_By");
                                            if (existingRequesters != null && existingRequesters.Any())
                                            {
                                                currentRequesters.AddRange(existingRequesters);
                                            }

                                            var additionalRequesters = string.Join(";", userEmails);
                                            foreach (var userEmail in userEmails)
                                            {
                                                var tmpusername = reportSiteContext.Web.EnsureUser(string.Format("{0}|{1}", ClaimIdentifier, requestEmail));
                                                reportSiteContext.Load(tmpusername);
                                                reportSiteContext.ExecuteQueryRetry();
                                                currentRequesters.Add(new FieldUserValue() { LookupId = tmpusername.Id });
                                            }


                                            LogWarning("Updating [{0}] with additional requesters {1}", assetId, additionalRequesters);
                                            existingRequestItem["Requested_x0020_By"] = currentRequesters;
                                            existingRequestItem["V3Comments"] = string.Format("Adding additional requester(s) {0}", additionalRequesters);
                                            existingRequestItem.Update();
                                            reportSiteContext.ExecuteQueryRetry();
                                        }
                                        catch (Exception ex)
                                        {
                                            LogError(ex, "Failed to updated item {0}", assetId);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (itemPosition == null)
                    {
                        break;
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed in QueryAppMonitor for WhatIf=>{0}", ShouldProcess("EPQQueryAppMonitor"));
            }
            return -1;
        }

        private string parseAppPermissions(string appRequestPermissionXml)
        {
            /*
                <AppPrincipal xmlns="http://schemas.microsoft.com/sharepoint/2012/app/manifest">
                    <RemoteWebApplication ClientId="17b6a002-cfbe-403d-ba89-b6c9f7a18773" />
                </AppPrincipal>
                <AppPermissionRequests xmlns="http://schemas.microsoft.com/sharepoint/2012/app/manifest">
                    <AppPermissionRequest Scope="http://sharepoint/content/sitecollection/web" Right="Manage" />
                    <AppPermissionRequest Scope="http://sharepoint/social/tenant" Right="Read" />
                    <AppPermissionRequest Scope="http://sharepoint/content/tenant" Right="Read" />
                </AppPermissionRequests>
            */
            var xmlPermissions = string.Empty;
            if (!string.IsNullOrEmpty(appRequestPermissionXml))
            {
                var appXml = XDocument.Parse(string.Format("<appxml xmlns=\"{1}\">{0}</appxml>", appRequestPermissionXml, "http://schemas.microsoft.com/sharepoint/2012/app/manifest"), LoadOptions.None);
                var appType = appXml.Root.GetType();
                var appRequestsName = XName.Get("AppPermissionRequests", "http://schemas.microsoft.com/sharepoint/2012/app/manifest");
                var appRequests = appXml.Root.Element(appRequestsName);
                if (appRequests != null)
                {
                    var appRequestName = XName.Get("AppPermissionRequest", "http://schemas.microsoft.com/sharepoint/2012/app/manifest");
                    var appRequestItems = appRequests.Elements(appRequestName);
                    foreach (var appItem in appRequestItems)
                    {
                        var appRight = appItem.Attribute("Right").Value;
                        var appScope = appItem.Attribute("Scope").Value;
                        xmlPermissions += string.Format("AppPermission Right:{0} || Scope:{1}", appRight, appScope);
                    }
                }
            }
            return xmlPermissions;
        }

        private void GetAppInfoByName(string appName)
        {

            ClientObjectList<AppInfo> _appInfo = TenantContext.GetAppInfoByName(appName);
            TenantContext.Context.Load(_appInfo);
            TenantContext.Context.ExecuteQueryRetry();

            foreach (AppInfo _if in _appInfo)
            {
                LogVerbose(_if.ToString());
            }
        }

        private void GetAppInfoByProductId(Guid productId)
        {

            ClientObjectList<AppInfo> _appInfo = TenantContext.GetAppInfoByProductId(productId);
            TenantContext.Context.Load(_appInfo);
            TenantContext.Context.ExecuteQueryRetry();

            foreach (AppInfo _if in _appInfo)
            {
                LogVerbose(_if.ToString());
            }
        }
    }
}
