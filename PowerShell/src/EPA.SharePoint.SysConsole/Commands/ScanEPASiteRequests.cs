using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using EPA.SharePoint.SysConsole.Models.Apps;
using EPA.SharePoint.SysConsole.Models.Governance;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    /// <summary>
    /// The function cmdlet will scan all of the site requests and check if they exist
    /// </summary>
    /// <remarks>
    ///     Will archive completed site requests
    /// </remarks>
    [Verb("ScanEPASiteRequests", HelpText = "scan site requests and move to archive folder.")]
    public class ScanEPASiteRequestsOptions : TenantCommandOptions
    {
        /// <summary>
        /// Should the query run against the root or all items
        /// </summary>
        [Option("norootfilter", Required = false)]
        public bool NoRootFilter { get; set; }

        /// <summary>
        /// validate Sites with Site Requests
        /// </summary>
        [Option("validate-sites", Required = false)]
        public bool ValidateSites { get; set; }
    }


    public static class ScanEPASiteRequestsOptionsExtension
    {
        /// <summary>
        /// Will execute the site request scan
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this ScanEPASiteRequestsOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPASiteRequests(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CSOM unhandled exceptions")]
    public class ScanEPASiteRequests : BaseSpoTenantCommand<ScanEPASiteRequestsOptions>
    {
        public ScanEPASiteRequests(ScanEPASiteRequestsOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        internal string TenantAdminUrl { get; set; }
        internal string ClientId { get; set; }
        internal string ClientSecret { get; set; }
        /// <summary>
        /// Threshold for processing list items
        /// </summary>
        private const int threshold = 25;

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
            var siteRequestUrl = Settings.Commands.SPOSiteRequestUrl;

            using (var siterequestcontext = this.ClientContext.Clone(siteRequestUrl))
            {
                var siterequestlist = siterequestcontext.Web.GetListByTitle(Constants_SiteRequest.SiteRequestListName,
                    lctx => lctx.RootFolder,
                    lctx => lctx.RootFolder.ServerRelativeUrl,
                    lctx => lctx.ItemCount);

                var camlFields = new string[] {
                    ConstantsFields.Field_ID,
                    ConstantsFields.Field_Title,
                    ConstantsFields.Field_Created,
                    ConstantsFields.Field_Modified,
                    ConstantsFields.Field_FileRef,
                    ConstantsFields.Field_FileDirRef,
                    Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteExists,
                    Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteMovedOrDeleted,
                    Constants_SiteRequest.SiteRequestFields.ChoiceField_TypeOfSite,
                    Constants_SiteRequest.SiteRequestFields.FieldText_TypeOfSiteID,
                    Constants_SiteRequest.SiteRequestFields.FieldLookup_SiteCollectionName,
                    Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag,
                    Constants_SiteRequest.SiteRequestFields.FieldText_RequestRejectedFlag,
                    Constants_SiteRequest.SiteRequestFields.FieldText_SiteSponsorApprovedFlag,
                    Constants_SiteRequest.SiteRequestFields.FieldText_SiteURL,
                    Constants_SiteRequest.SiteRequestFields.FieldText_TemplateName
                };


                var startItemId = 0;
                var lastItemId = siterequestlist.QueryLastItemId();


                var rootFolderUrl = siterequestlist.RootFolder.ServerRelativeUrl;


                var archivedFolder = siterequestlist.GetOrCreateFolder(siterequestlist.RootFolder, "Archived", startItemId, lastItemId);
                if (!archivedFolder.IsPropertyAvailable(fctx => fctx.ServerRelativeUrl))
                {
                    archivedFolder.Context.Load(archivedFolder, fctx => fctx.ServerRelativeUrl);
                    archivedFolder.Context.ExecuteQueryRetry();
                }
                var archiveFolderUrl = archivedFolder.ServerRelativeUrl;


                // Set up default CAML query
                var camlViewFields = CAML.ViewFields(camlFields.Select(s => CAML.FieldRef(s)).ToArray());

                if (Opts.ValidateSites)
                {
                    ValidateSiteRequestsWithTenant(siterequestlist, camlFields, camlViewFields, rootFolderUrl);
                }

                // Move Items into an Archived Folder so we know they are completed
                MoveSiteRequestArchived(siterequestcontext, siterequestlist, camlViewFields, rootFolderUrl, archiveFolderUrl);

            }

            return 1;
        }

        private void ValidateSiteRequestsWithTenant(List siterequestlist, string[] camlFields, string camlViewFields, string rootFolderUrl)
        {
            var objects = new Collection<Model_SiteRequestItem>();

            var filedireq = string.Empty;
            if (Opts.NoRootFilter == false)
            {
                filedireq = CAML.And(
                    CAML.Eq(CAML.FieldValue("FileDirRef", FieldType.Text.ToString("f"), rootFolderUrl)),
                    CAML.Eq(CAML.FieldValue("FSObjType", FieldType.Integer.ToString("f"), 0.ToString()))
                );
            }

            // reclaim the ids
            var startItemId = 0;
            var lastItemId = siterequestlist.QueryLastItemId();

            // Pull all Site Requests
            var camlQueries = siterequestlist.SafeCamlClauseFromThreshold(2000, filedireq, startItemId, lastItemId);
            foreach (var camlQuery in camlQueries)
            {
                var camlWhere = (string.IsNullOrEmpty(camlQuery) ? string.Empty : CAML.Where(camlQuery));
                var camlK = new CamlQuery()
                {
                    DatesInUtc = true,
                    ViewXml = CAML.ViewQuery(
                        ViewScope.RecursiveAll,
                        camlWhere,
                        CAML.OrderBy(new OrderByField(ConstantsFields.Field_ID)),
                        camlViewFields,
                        200)
                };
                LogWarning("Caml {0}", camlWhere);
                ListItemCollectionPosition itemPosition = null;
                while (true)
                {
                    LogVerbose("Caml Position {0}", (itemPosition == null) ? "Initial" : itemPosition.PagingInfo);
                    camlK.ListItemCollectionPosition = itemPosition;
                    var items = siterequestlist.GetItems(camlK);
                    siterequestlist.Context.Load(items, ictx => ictx.ListItemCollectionPosition);
                    siterequestlist.Context.ExecuteQueryRetry();
                    itemPosition = items.ListItemCollectionPosition;

                    foreach (var requestitem in items)
                    {
                        var model = new Model_SiteRequestItem()
                        {
                            Id = requestitem.Id,
                            Title = requestitem.RetrieveListItemValue(ConstantsFields.Field_Title)
                        };

                        model.FileRef = requestitem.RetrieveListItemValue(ConstantsFields.Field_FileRef);
                        model.FileDirRef = requestitem.RetrieveListItemValue(ConstantsFields.Field_FileDirRef);
                        model.Created = requestitem.RetrieveListItemValue(ConstantsFields.Field_Created).ToNullableDatetime();
                        model.Modified = requestitem.RetrieveListItemValue(ConstantsFields.Field_Modified).ToNullableDatetime();
                        model.RequestCompletedFlag = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag);
                        model.RequestRejectedFlag = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestRejectedFlag);
                        model.SiteExists = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteExists).ToBoolean(false);
                        model.SiteMovedOrDeleted = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteMovedOrDeleted).ToBoolean(false);
                        model.SiteUrl = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteURL).Replace("&nbsp;", " ");
                        var lookupsite = requestitem.RetrieveListItemValueAsLookup(Constants_SiteRequest.SiteRequestFields.FieldLookup_SiteCollectionName);


                        foreach (var column in camlFields)
                        {
                            model.ColumnValues.Add(new SPListItemFieldDefinition()
                            {
                                FieldName = column,
                                FieldValue = requestitem[column]
                            });
                        }

                        objects.Add(model);
                    }

                    if (itemPosition == null)
                    {
                        break;
                    }
                }
            }


            // If we aren't sure if it exists, lets check and validate
            var sitesToValidate = objects.Where(w => !w.SiteExists
                 || (w.SiteExists && w.RequestCompletedFlag.Equals("no", StringComparison.CurrentCultureIgnoreCase)));
            foreach (var model in sitesToValidate)
            {
                var _siteUrl = model.SiteUrl;
                var completedStatus = model.RequestCompletedFlag;
                try
                {
                    SetSiteAdmin(_siteUrl, CurrentUserName, true);

                    using var sitecontext = this.ClientContext.Clone(_siteUrl);
                    sitecontext.Load(sitecontext.Web, sctx => sctx.Id);
                    sitecontext.ExecuteQueryRetry();

                    model.PrcUpdated = true;
                    model.SiteExists = true;
                }
                catch (Exception ex)
                {
                    // only flag it if this was a completed request
                    if (completedStatus.IndexOf("yes", StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        model.PrcUpdated = true;
                        model.SiteMovedOrDeleted = true;
                    }
                    LogWarning("Failed to find web => {0}", ex.Message);
                }
            }


            // Any sites that were updated lets mark as completed
            var idx = 0;
            var siteExistsItems = objects.Where(w => w.SiteExists && w.PrcUpdated);
            var totalExistsItems = siteExistsItems.Count();
            foreach (var site in siteExistsItems)
            {
                ++idx;
                totalExistsItems--;
                var siteitem = siterequestlist.GetItemById("" + site.Id);
                siterequestlist.Context.Load(siteitem);
                if (site.RequestCompletedFlag.Equals("no", StringComparison.CurrentCultureIgnoreCase))
                {
                    siteitem[Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag] = "Yes";
                }
                siteitem[Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteExists] = 1;
                siteitem[Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteMovedOrDeleted] = 0;
                siteitem.SystemUpdate();

                if (idx > threshold || totalExistsItems <= 0)
                {
                    siterequestlist.Context.ExecuteQueryRetry();
                    idx = 0;
                }
            }

            idx = 0;
            var siteDeletedItems = objects.Where(w => w.SiteMovedOrDeleted && w.PrcUpdated);
            var totalDeletedItems = siteDeletedItems.Count();
            foreach (var site in siteDeletedItems)
            {
                ++idx;
                totalDeletedItems--;
                var siteitem = siterequestlist.GetItemById("" + site.Id);
                siterequestlist.Context.Load(siteitem);
                siteitem[Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteExists] = 0;
                siteitem[Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteMovedOrDeleted] = 1;
                siteitem.SystemUpdate();

                if (idx > threshold || totalDeletedItems <= 0)
                {
                    siterequestlist.Context.ExecuteQueryRetry();
                    idx = 0;
                }
            }
        }

        private void MoveSiteRequestArchived(ClientContext siterequestcontext, List siterequestlist, string camlViewFields, string rootFolderUrl, string archiveFolderUrl)
        {

            var objects = new Collection<Model_SiteRequestItem>();


            var startItemId = 0;
            var lastItemId = siterequestlist.QueryLastItemId();

            var filedireq = CAML.And(
                CAML.Eq(CAML.FieldValue("FileDirRef", FieldType.Text.ToString("f"), rootFolderUrl)),
                CAML.And(
                    CAML.Eq(CAML.FieldValue("FSObjType", FieldType.Integer.ToString("f"), 0.ToString())),
                    CAML.Eq(CAML.FieldValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag, FieldType.Text.ToString("f"), "Yes"))
                ));

            var camlQueries = siterequestlist.SafeCamlClauseFromThreshold(2000, filedireq, startItemId, lastItemId);
            foreach (var camlQuery in camlQueries)
            {
                var camlWhere = CAML.Where(camlQuery);
                var camlK = new CamlQuery()
                {
                    DatesInUtc = true,
                    ViewXml = CAML.ViewQuery(
                        ViewScope.RecursiveAll,
                        camlWhere,
                        CAML.OrderBy(new OrderByField(ConstantsFields.Field_ID)),
                        camlViewFields,
                        100)
                };

                LogWarning("Caml {0}", camlWhere);
                ListItemCollectionPosition itemPosition = null;
                while (true)
                {
                    LogVerbose("Caml Position {0}", (itemPosition == null) ? "Initial" : itemPosition.PagingInfo);
                    camlK.ListItemCollectionPosition = itemPosition;
                    var items = siterequestlist.GetItems(camlK);
                    siterequestlist.Context.Load(items, ictx => ictx.ListItemCollectionPosition);
                    siterequestlist.Context.ExecuteQueryRetry();
                    itemPosition = items.ListItemCollectionPosition;

                    foreach (var requestitem in items)
                    {
                        var model = new Model_SiteRequestItem()
                        {
                            Id = requestitem.Id,
                            Title = requestitem.RetrieveListItemValue(ConstantsFields.Field_Title)
                        };

                        model.FileRef = requestitem.RetrieveListItemValue(ConstantsFields.Field_FileRef);
                        model.FileDirRef = requestitem.RetrieveListItemValue(ConstantsFields.Field_FileDirRef);
                        model.Created = requestitem.RetrieveListItemValue(ConstantsFields.Field_Created).ToNullableDatetime();
                        model.Modified = requestitem.RetrieveListItemValue(ConstantsFields.Field_Modified).ToNullableDatetime();
                        model.RequestCompletedFlag = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag);
                        model.RequestRejectedFlag = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestRejectedFlag);
                        model.SiteUrl = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteURL);
                        model.SiteExists = requestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteExists).ToBoolean(false);

                        objects.Add(model);
                    }

                    if (itemPosition == null)
                    {
                        break;
                    }
                }
            }

            rootFolderUrl = TokenHelper.EnsureTrailingSlash(rootFolderUrl);
            archiveFolderUrl = TokenHelper.EnsureTrailingSlash(archiveFolderUrl);

            var itemsToArchive = objects.Where(w => w.RequestCompletedFlag == "Yes" && w.SiteExists);
            foreach (var itemToArchive in itemsToArchive)
            {
                if (itemToArchive.FileRef.IndexOf(@"/archived/", StringComparison.CurrentCultureIgnoreCase) == -1)
                {
                    var fileRef = itemToArchive.FileRef;
                    var targetUrl = fileRef.Replace(rootFolderUrl, archiveFolderUrl);
                    LogWarning("Moving {0} to {1}", fileRef, targetUrl);
                    var moved = siterequestcontext.MoveFileToFolder(fileRef, targetUrl);
                    if (!moved)
                    {
                        LogWarning($"Failed to move {fileRef}");
                    }
                }
            }

            LogVerbose("Items queried {0}", objects.Count());
        }
    }
}
