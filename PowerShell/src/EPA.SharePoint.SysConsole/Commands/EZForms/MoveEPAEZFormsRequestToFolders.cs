using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("MoveEPAEZFormsRequests", HelpText = "will move a ezform stale requests into the archive state.")]
    public class MoveEPAEZFormsRequestToFoldersOptions : CommonOptions
    {
        /// <summary>
        /// Determines the specific list items by date from which to migrate
        /// </summary>
        [Option("archive-date", Required = false, HelpText = "Specify a time frame from which to scan and move items.")]
        public DateTime? ArchiveDate { get; set; }

        /// <summary>
        /// migrate to deeper folder structure
        /// </summary>
        [Option("migrate-subfolder", Required = false, HelpText = "If we want to migrate to deeper folder structure.")]
        public bool MigrateSubfolder { get; set; }

        /// <summary>
        /// migrate requests which are no longer valid into Deleted folder
        /// </summary>
        [Option("migrate-deleted", Required = false, HelpText = "If we want to migrate non-valid requests into deleted folder.")]
        public bool MigrateDeleted { get; set; }
    }

    public static class MoveEPAEZFormsRequestToFoldersOptionsExtension
    {
        /// <summary>
        /// Will execute scan of EZ Forms and move requests into subfolders to ensure List View Threshold is not reached
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this MoveEPAEZFormsRequestToFoldersOptions opts, IAppSettings appSettings)
        {
            var cmd = new MoveEPAEZFormsRequestToFolders(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will move EZ Form requests from root folder to archive folders
    /// </summary>
    /// <remarks>Filter requests by threshold date</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Too much effort to handle exceptions.")]
    public class MoveEPAEZFormsRequestToFolders : BaseEZFormsRecertification<MoveEPAEZFormsRequestToFoldersOptions>
    {
        public MoveEPAEZFormsRequestToFolders(MoveEPAEZFormsRequestToFoldersOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override int OnRun()
        {
            // Migrate requests into Archived Folder
            var result = MigrateRequestsIntoArchivedFolder();

            // Migrate Items into Deleted Folder
            if (Opts.MigrateDeleted)
            {
                result = MigrateRequestsIntoDeletedFolder();
            }

            // Migrate Archived items into Archived/Year folders
            if (Opts.MigrateSubfolder)
            {
                // Push Archived Items into Request Date [Year] Folders
                result = MigrateRequestsIntoArchivedFolderWithDates();
            }

            return result;
        }

        #region Migrate Requests which are stagnate into Archived Folders

        public int MigrateRequestsIntoArchivedFolder()
        {
            // get everything created greater than 6 months ago
            var ArchiveDate = Opts.ArchiveDate ?? DateTime.UtcNow.AddMonths(-6);

            var camlFieldRefs = new string[] {
                    ConstantsFields.Field_ID,
                    ConstantsFields.Field_Created,
                    ConstantsFields.Field_Modified,
                    ConstantsFields.Field_Author,
                    ConstantsFields.Field_Editor,
                    ConstantsFields.Field_FileRef,
                    ConstantsFields.Field_FileDirRef,
                    ConstantsFields.Field_FileLeafRef,
                    EzForms_AccessRequest.Field_Request_x0020_Status,
                    EzForms_AccessRequest.Field_Routing_x0020_Phase,
                    EzForms_AccessRequest.Field_Request_x0020_Type
                };
            var camlViewClause = CAML.ViewFields(camlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());


            var listInSite = ClientContext.Web.Lists.GetByTitle(EzForms_AccessRequest.ListName);
            ClientContext.Load(listInSite, inc => inc.ItemCount, inn => inn.RootFolder, inn => inn.RootFolder.ServerRelativeUrl, inn => inn.ParentWeb.ServerRelativeUrl, inn => inn.LastItemModifiedDate);
            ClientContext.ExecuteQueryRetry();

            var startItemId = 0;
            var itemCountPossibleId = listInSite.ItemCount > 5000 ? listInSite.QueryLastItemId() : 5000;

            var archiveFolder = listInSite.GetOrCreateFolder(listInSite.RootFolder, "Archived", startItemId, itemCountPossibleId, loadFolders: false);
            if (archiveFolder == null)
            {
                LogWarning("Approved folder not found; leaving the cmdlet");
                return -1;
            }

            archiveFolder.EnsureProperties(afold => afold.ServerRelativeUrl);
            var archiveFolderRelativeUrl = archiveFolder.ServerRelativeUrl;
            var utcFormattedIsoDate = ArchiveDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var camlEqClause =
                CAML.And(
                    CAML.Or(
                        CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), "approved")),
                        CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), "denied"))),
                    CAML.Leq(string.Format("{0}<Value Type='{1}' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{2}</Value>",
                    CAML.FieldRef("Modified"),
                    FieldType.DateTime.ToString("f"),
                    utcFormattedIsoDate)));



            // CAML query array
            var camlQueries = listInSite.SafeCamlClauseFromThreshold(2000, camlEqClause, startItemId, itemCountPossibleId);
            camlQueries.ForEach(camlClause =>
            {
                var camlWhereClause = !string.IsNullOrEmpty(camlClause) ? CAML.Where(camlClause) : camlClause;
                LogWarning($"CAML query {camlWhereClause}");
                QueryListAndMoveArchivedItems(listInSite, camlViewClause, camlWhereClause, archiveFolderRelativeUrl);
            });

            return 1;
        }

        private void QueryListAndMoveArchivedItems(List listInSite, string camlViewClause, string camlWhereClause, string archiveFolderRelativeUrl)
        {
            var camlQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(ViewScope.DefaultValue, camlWhereClause, "", camlViewClause, 100),
                ListItemCollectionPosition = null
            };

            do
            {
                LogVerbose("Executing query with position {0}", camlQuery?.ListItemCollectionPosition?.PagingInfo ?? "default");

                var spListItems = listInSite.GetItems(camlQuery);
                ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                ClientContext.ExecuteQueryRetry();
                camlQuery.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    try
                    {
                        var fileRef = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_FileRef);
                        var fileDirRef = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_FileDirRef);
                        var created = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Created).ToDateTime();
                        var modified = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Modified).ToDateTime();
                        var modifiedBy = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Editor);
                        var requestType = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Type);
                        var requestStatus = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Status);
                        LogVerbose("File {0} url {1} created {2} modified {3}", requestItem.Id, fileRef, created, modified);

                        if ((requestStatus.Equals("denied", StringComparison.CurrentCultureIgnoreCase)
                            || requestStatus.Equals("approved", StringComparison.CurrentCultureIgnoreCase))
                            && fileRef.IndexOf(archiveFolderRelativeUrl, StringComparison.CurrentCultureIgnoreCase) < 0)
                        {
                            // lets move the archived files
                            var targetUrl = fileRef.Replace(fileDirRef, archiveFolderRelativeUrl);
                            LogVerbose($"ID {requestItem.Id} pulling item {fileRef} and moving to {targetUrl}");
                            var moved = ClientContext.MoveFileToFolder(fileRef, targetUrl, 15, 1000, userAgent: Settings.Commands.SharePointPnPUserAgent);
                            if (!moved)
                            {
                                LogWarning($"Failed to move item {fileRef} into {targetUrl}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Failed to query access list");
                    }
                }

            }
            while (camlQuery.ListItemCollectionPosition != null);

        }

        #endregion

        #region Migrate Requests in the Archived folder into Archived/Year folders

        /// <summary>
        /// Will migrate the "Archived" folder items into Archived\Request Date folders
        /// </summary>
        public int MigrateRequestsIntoArchivedFolderWithDates()
        {
            var files = new List<EzFormsListItemModel>();
            var camlFieldRefs = new string[] {
                EzForms_AccessRequest.Field_ID,
                EzForms_AccessRequest.Field_Title,
                EzForms_AccessRequest.Field_Created,
                EzForms_AccessRequest.Field_Modified,
                EzForms_AccessRequest.Field_Author,
                EzForms_AccessRequest.Field_Editor,
                EzForms_AccessRequest.Field_FileRef,
                EzForms_AccessRequest.Field_FileDirRef,
                EzForms_AccessRequest.Field_FileLeafRef,
                EzForms_AccessRequest.Field_Request_x0020_Date,
                EzForms_AccessRequest.Field_Request_x0020_Status,
                EzForms_AccessRequest.Field_Routing_x0020_Phase,
                EzForms_AccessRequest.Field_Request_x0020_Type
            };
            var camlViewClause = CAML.ViewFields(camlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());


            var listInSite = ClientContext.Web.Lists.GetByTitle(EzForms_AccessRequest.ListName);
            ClientContext.Load(listInSite, inc => inc.ItemCount, inn => inn.RootFolder, inn => inn.RootFolder.ServerRelativeUrl, inn => inn.ParentWeb.ServerRelativeUrl);
            ClientContext.ExecuteQueryRetry();

            var archiveFolder = listInSite.GetOrCreateFolder(listInSite.RootFolder, "Archived", loadFolders: false);
            ClientContext.Load(archiveFolder, inn => inn.ServerRelativeUrl);
            ClientContext.ExecuteQueryRetry();
            
            // CAML query array
            var camlQueries = listInSite.SafeCamlClauseFromThreshold();
            camlQueries.ForEach(camlClause =>
            {
                var camlEqClause = CAML.And(
                        CAML.Eq(CAML.FieldValue("FileDirRef", FieldType.Text.ToString("f"), archiveFolder.ServerRelativeUrl)),
                        CAML.Eq(CAML.FieldValue("FSObjType", FieldType.Integer.ToString("f"), 0.ToString())));

                if (!string.IsNullOrEmpty(camlClause))
                {
                    camlEqClause = CAML.And(camlClause, camlEqClause);
                }

                var camlWhereClause = CAML.Where(camlEqClause);
                var archives = QueryListAndMoveSingleArchived(listInSite, camlViewClause, camlFieldRefs, camlWhereClause);
                files.AddRange(archives);
            });

            var folderList = new Dictionary<int, string>();
            var distinctYears = files?.Select(s => s.RequestDate.Year).Distinct();
            foreach (var distinctYear in distinctYears)
            {
                var yearFolder = listInSite.GetOrCreateFolder(archiveFolder, distinctYear.ToString(), loadFolders: false);
                ClientContext.Load(yearFolder, inn => inn.ServerRelativeUrl);
                ClientContext.ExecuteQueryRetry();
                folderList.Add(distinctYear, yearFolder.ServerRelativeUrl);
            }

            foreach (var ezform in files?.OrderBy(ob => ob.RequestDate))
            {
                var requestYear = ezform.RequestDate.Year;
                var folder = folderList.FirstOrDefault(f => f.Key == requestYear);
                if (folder.Equals(default))
                {
                    throw new Exception($"Failed to discovery request folder {requestYear}");
                }

                var fileRef = ezform.FileRef;
                var requestDateFolder = folder.Value.EnsureTrailingSlash();

                if (!fileRef.Contains(requestDateFolder))
                {
                    // lets move the Denied files first
                    var targetUrl = fileRef.Replace(archiveFolder.ServerRelativeUrl, folder.Value);

                    LogVerbose("ID {0} pulling item {1} and moving to {2}", ezform.Id, fileRef, targetUrl);
                    var moved = ClientContext.MoveFileToFolder(fileRef, targetUrl, 15, 1000, userAgent: Settings.Commands.SharePointPnPUserAgent);
                    if (!moved)
                    {
                        LogWarning($"Failed to move item {fileRef} into {targetUrl}");
                    }
                }
            }

            return 1;
        }

        private IEnumerable<EzFormsListItemModel> QueryListAndMoveSingleArchived(List listInSite, string camlViewClause, string[] camlFields, string camlWhereClause)
        {
            var foundFiles = new List<EzFormsListItemModel>();

            var camlQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, camlWhereClause, "", camlViewClause, 100),
                ListItemCollectionPosition = null
            };

            do
            {
                LogVerbose($"Found additional rows, executing query with position {camlQuery?.ListItemCollectionPosition?.PagingInfo ?? "default"}");
                var spListItems = listInSite.GetItems(camlQuery);
                this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                this.ClientContext.ExecuteQueryRetry();
                camlQuery.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var ezitem in spListItems)
                {
                    try
                    {
                        var model = new EzFormsListItemModel()
                        {
                            Id = ezitem.Id,
                            Title = ezitem.RetrieveListItemValue(EzForms_AccessRequest.Field_Title),
                            RequestDate = ezitem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Date).ToDateTime(),
                            FileRef = ezitem.RetrieveListItemValue(EzForms_AccessRequest.Field_FileRef),
                            FileDirRef = ezitem.RetrieveListItemValue(EzForms_AccessRequest.Field_FileDirRef),
                            Created = ezitem.RetrieveListItemValue(EzForms_AccessRequest.Field_Created).ToDateTime(),
                            CreatedBy = GetPrincipal(ezitem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Author)),
                            Modified = ezitem.RetrieveListItemValue(EzForms_AccessRequest.Field_Modified).ToDateTime(),
                            ModifiedBy = GetPrincipal(ezitem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Editor))
                        };

                        foreach (var column in camlFields)
                        {
                            model.ColumnValues.Add(new SPListItemFieldDefinition()
                            {
                                FieldName = column,
                                FieldValue = ezitem[column]
                            });
                        }

                        foundFiles.Add(model);
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Failed to query access list");
                    }
                }
            }
            while (camlQuery.ListItemCollectionPosition != null);

            return foundFiles;
        }

        #endregion

        internal SPPrincipalUserDefinition GetPrincipal(FieldUserValue user)
        {
            if (user != null) return null;

            return new SPPrincipalUserDefinition()
            {
                Id = user.LookupId,
                LoginName = user.LookupValue,
                Email = user.ToUserEmailValue()
            };
        }


        public int MigrateRequestsIntoDeletedFolder()
        {
            // get everything created greater than 6 months ago
            var ArchiveDeleteDate = Opts.ArchiveDate ?? DateTime.UtcNow.AddMonths(-9);

            // Avoiding these as administrators of the system
            var skipAuthors = new string[]
            {
                "sharepointadmin@usepa.onmicrosoft.com",
                "leonard.shawn@epa.gov"
            };

            var camlFieldRefs = new string[] {
                    ConstantsFields.Field_ID,
                    ConstantsFields.Field_Created,
                    ConstantsFields.Field_Modified,
                    ConstantsFields.Field_Author,
                    ConstantsFields.Field_Editor,
                    ConstantsFields.Field_FileRef,
                    ConstantsFields.Field_FileDirRef,
                    ConstantsFields.Field_FileLeafRef,
                    EzForms_AccessRequest.Field_Request_x0020_Status,
                    EzForms_AccessRequest.Field_Routing_x0020_Phase,
                    EzForms_AccessRequest.Field_Request_x0020_Type
                };
            var camlViewClause = CAML.ViewFields(camlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());


            var listInSite = ClientContext.Web.Lists.GetByTitle(EzForms_AccessRequest.ListName);
            ClientContext.Load(listInSite, inc => inc.ItemCount, inn => inn.RootFolder, inn => inn.RootFolder.ServerRelativeUrl, inn => inn.ParentWeb.ServerRelativeUrl, inn => inn.LastItemModifiedDate);
            ClientContext.ExecuteQueryRetry();

            var startItemId = 0;
            var lastItemId = listInSite.ItemCount > 5000 ? listInSite.QueryLastItemId() : 5000;


            var deletedFolder = listInSite.GetOrCreateFolder(listInSite.RootFolder, "Deleted", startItemId, lastItemId, loadFolders: false);
            if (deletedFolder == null)
            {
                LogWarning("Deleted folder not found; leaving the cmdlet");
                return -1;
            }

            deletedFolder.EnsureProperties(afold => afold.ServerRelativeUrl);
            var folderRelativeUrl = deletedFolder.ServerRelativeUrl;


            var skipLastEditorCaml = string.Empty;
            foreach (var skipAuthor in skipAuthors)
            {
                var userId = ClientContext.Web.EnsureUser(string.Format("{0}|{1}", ClaimIdentifier, skipAuthor.Trim()));
                ClientContext.Load(userId);
                ClientContext.ExecuteQueryRetry();

                var negauthor = CAML.Eq(CAML.FieldValue(ConstantsFields.Field_Editor, FieldType.User.ToString("f"), userId.Id.ToString()));
                if (string.IsNullOrEmpty(skipLastEditorCaml))
                {
                    skipLastEditorCaml = negauthor;
                }
                else
                {
                    skipLastEditorCaml = CAML.Or(skipLastEditorCaml, negauthor);
                }
            }

            var utcFormattedIsoDate = ArchiveDeleteDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var camlEqClause = CAML.Or(
                // Last Modified over a Year ago
                CAML.And(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), "Pending")),
                    CAML.Leq(string.Format("{0}<Value Type='{1}' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{2}</Value>",
                        CAML.FieldRef(ConstantsFields.Field_Modified), FieldType.DateTime.ToString("f"), utcFormattedIsoDate))
                        ),
                // Created over a Year ago and last modified by [collection of users]
                CAML.And(
                    CAML.And(
                        CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), "Pending")),
                        CAML.Leq(string.Format("{0}<Value Type='{1}' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{2}</Value>",
                            CAML.FieldRef(ConstantsFields.Field_Created), FieldType.DateTime.ToString("f"), utcFormattedIsoDate))
                            ),
                    skipLastEditorCaml
                    )
                );


            // CAML query array
            var camlQueries = listInSite.SafeCamlClauseFromThreshold(2000, camlEqClause, startItemId, lastItemId);
            camlQueries.ForEach(camlClause =>
            {
                var camlWhereClause = !string.IsNullOrEmpty(camlClause) ? CAML.Where(camlClause) : camlClause;
                QueryListAndMoveArchivedItemsIntoDeletedFolder(listInSite, camlViewClause, camlWhereClause, folderRelativeUrl, skipAuthors);
            });

            return 1;
        }

        /// <summary>
        /// Will query based on the specified <paramref name="camlWhereClause"/> and move each list item to its target <paramref name="folderRelativeUrl"/>
        /// </summary>
        /// <param name="listInSite"></param>
        /// <param name="camlViewClause"></param>
        /// <param name="camlWhereClause"></param>
        /// <param name="folderRelativeUrl"></param>
        /// <param name="skipAuthors">(optional) if specified will skip the row if the Author/Editor are in this collection</param>
        private void QueryListAndMoveArchivedItemsIntoDeletedFolder(List listInSite, string camlViewClause, string camlWhereClause, string folderRelativeUrl, string[] skipAuthors = null)
        {
            LogWarning("CAML query {0}", camlWhereClause);
            ListItemCollectionPosition ListItemCollectionPosition = null;
            var camlQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(ViewScope.DefaultValue, camlWhereClause, "", camlViewClause, 100)
            };


            while (true)
            {
                camlQuery.ListItemCollectionPosition = ListItemCollectionPosition;
                var spListItems = listInSite.GetItems(camlQuery);
                this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                this.ClientContext.ExecuteQueryRetry();
                ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    var fileRef = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_FileRef);
                    var fileDirRef = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_FileDirRef);
                    var created = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Created).ToDateTime();
                    var modified = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Modified).ToDateTime();
                    var modifiedBy = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Editor);
                    var requestType = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Type);
                    var requestStatus = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Status);

                    if (skipAuthors.Any() && skipAuthors.Any(m => m.Equals(modifiedBy.Email, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        // we are ensuring the editor is not a skipAuthor
                        LogVerbose("ID {0} Skipping as the Last Editor was {1}", requestItem.Id, modifiedBy.LookupValue);
                        continue;
                    }

                    if (requestStatus.Equals("pending", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // lets move the archived files
                        var targetUrl = fileRef.Replace(fileDirRef, folderRelativeUrl);

                        if (this.ShouldProcess(string.Format("File {0} url {1} created {2} modified {3} by {4} and moving to {5}", requestItem.Id, fileRef, created, modified, modifiedBy.Email, targetUrl)))
                        {
                            requestItem[EzForms_AccessRequest.Field_Request_x0020_Status] = "Deleted";
                            requestItem.SystemUpdate();

                            var moved = ClientContext.MoveFileToFolder(fileRef, targetUrl);
                            if (!moved)
                            {
                                LogWarning($"Failed to move {fileRef}");
                            }
                        }
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
        }
    }
}
