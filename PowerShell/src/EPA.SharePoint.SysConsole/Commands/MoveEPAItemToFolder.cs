using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.PipeBinds;
using Microsoft.SharePoint.Client;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    /// <summary>
    /// The function cmdlet will move a list item to the specified folder
    /// </summary>
    /// <remarks>Filter requests by threshold date</remarks>
    [Verb("moveEPAItemToFolder", HelpText = "The function cmdlet will move a list item to the specified folder.")]
    public class MoveEPAItemToFolderOptions : CommonOptions
    {
        /// <summary>
        /// The site to search and query Term Sets
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// The list instance from which we will execute the move operation
        /// </summary>
        [Option("list", Required = true)]
        public ListPipeBind List { get; set; }

        /// <summary>
        /// ID specified
        /// </summary>
        [Option("list-itemid", Required = true)]
        public int ListItemId { get; set; } = 0;

        /// <summary>
        /// The replacement path portion
        /// </summary>
        [Option("replace-foldername", Required = true)]
        public string ReplacementFolderName { get; set; }

        /// <summary>
        /// The target path portion
        /// </summary>
        [Option("target-foldername", Required = false)]
        public string TargetFolderName { get; set; }
    }

    public static class MoveEPAItemToFolderOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this MoveEPAItemToFolderOptions opts, IAppSettings appSettings)
        {
            var cmd = new MoveEPAItemToFolder(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class MoveEPAItemToFolder : BaseSpoCommand<MoveEPAItemToFolderOptions>
    {
        public MoveEPAItemToFolder(MoveEPAItemToFolderOptions opts, IAppSettings settings)
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
            var siteContext = this.ClientContext;
            var listInSite = siteContext.Web.Lists.GetByTitle(Opts.List.Title);
            siteContext.Load(listInSite, inc => inc.ItemCount, inn => inn.RootFolder, inn => inn.RootFolder.ServerRelativeUrl, inn => inn.ParentWeb.ServerRelativeUrl, inn => inn.LastItemModifiedDate);
            siteContext.ExecuteQueryRetry();

            var currentRelativeUrl = listInSite.RootFolder.ServerRelativeUrl;
            var currentFolder = listInSite.GetOrCreateFolder(listInSite.RootFolder, Opts.ReplacementFolderName);
            siteContext.Load(currentFolder, inn => inn.ServerRelativeUrl);
            siteContext.ExecuteQueryRetry();

            if (currentFolder == null)
            {
                LogWarning("List Item specified folder not found; leaving the cmdlet");
                return -1;
            }

            currentFolder.EnsureProperties(afold => afold.ServerRelativeUrl);
            currentRelativeUrl = currentFolder.ServerRelativeUrl;


            var targetRelativeUrl = listInSite.RootFolder.ServerRelativeUrl;
            if (!string.IsNullOrEmpty(Opts.TargetFolderName))
            {

                var targetFolder = listInSite.GetOrCreateFolder(listInSite.RootFolder, Opts.TargetFolderName);
                siteContext.Load(targetFolder, inn => inn.ServerRelativeUrl);
                siteContext.ExecuteQueryRetry();

                if (targetFolder == null)
                {
                    LogWarning("Approved folder not found; leaving the cmdlet");
                    return -1;
                }

                targetFolder.EnsureProperties(afold => afold.ServerRelativeUrl);
                targetRelativeUrl = targetFolder.ServerRelativeUrl;
            }

            var requestItem = listInSite.GetItemById(Opts.ListItemId);
            siteContext.Load(requestItem);
            siteContext.ExecuteQueryRetry();

            var fileRef = requestItem.RetrieveListItemValue(ConstantsListFields.Field_FileRef);
            var fileDirRef = requestItem.RetrieveListItemValue(ConstantsListFields.Field_FileDirRef);
            var created = requestItem.RetrieveListItemValue(ConstantsListFields.Field_Created).ToDateTime();
            var modified = requestItem.RetrieveListItemValue(ConstantsListFields.Field_Modified).ToDateTime();
            var modifiedBy = requestItem.RetrieveListItemUserValue(ConstantsListFields.Field_Editor);
            LogVerbose("File {0} url {1} created {2} modified {3}", requestItem.Id, fileRef, created, modified);

            if (fileRef.Contains(currentRelativeUrl))
            {
                // lets move the Denied files first
                var targetUrl = fileRef.Replace(currentRelativeUrl, targetRelativeUrl);

                LogVerbose($"ID {requestItem.Id} pulling item {fileRef} and moving to {targetUrl}");
                var moved = siteContext.MoveFileToFolder(fileRef, targetUrl);
                if (!moved)
                {
                    LogWarning($"Failed to move {fileRef}");
                    return -1;
                }
            }

            return 1;
        }
    }
}
