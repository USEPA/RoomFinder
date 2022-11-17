using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using Serilog;
using System;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("moveEPAFolder", HelpText = "Processes a folder into a new location.")]
    public class MoveEPAFolderOptions : CommonOptions
    {
        /// <summary>
        /// The site to search and query Term Sets
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// The source list containing attachments
        /// </summary>
        [Option("source-list", Required = true)]
        public string SourceListName { get; set; }

        /// <summary>
        /// The source folder relative path to be moved
        /// </summary>
        [Option("starting-folder", Required = false)]
        public string StartingFolder { get; set; }

        /// <summary>
        /// The source document library where files will be copied
        /// </summary>
        [Option("destination-list", Required = true)]
        public string DestinationLibraryName { get; set; }
    }

    public static class MoveEPAFolderOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this MoveEPAFolderOptions opts, IAppSettings appSettings)
        {
            var cmd = new MoveEPAFolder(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will move a folder from a document library to a new document library
    /// </summary>
    public class MoveEPAFolder : BaseSpoCommand<MoveEPAFolderOptions>
    {
        public MoveEPAFolder(MoveEPAFolderOptions opts, IAppSettings settings)
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

            var ctx = this.ClientContext;
            if (!ctx.Web.IsPropertyAvailable("ServerRelativeUrl"))
            {
                ctx.Load(ctx.Web, w => w.ServerRelativeUrl);
            }
            ctx.ExecuteQueryRetry();

            List _srclist = ctx.Web.Lists.GetByTitle(Opts.SourceListName);
            ctx.Load(_srclist, f => f.Title, lf => lf.Id, lf => lf.RootFolder);
            ctx.Load(_srclist.RootFolder, rf => rf.Files, rf => rf.ServerRelativeUrl, rf => rf.Folders);
            ctx.ExecuteQueryRetry();
            var folderICareAbout = _srclist.RootFolder.Folders.FirstOrDefault(f => f.Name == Opts.StartingFolder);

            var sourceFolder = string.Format("{0}/{1}", _srclist.RootFolder.ServerRelativeUrl, Opts.StartingFolder);

            ListItemCollectionPosition listItemCollectionPosition = null;
            CamlQuery camlQuery = new CamlQuery
            {
                ViewXml = string.Format(@"<View><Query>
<OrderBy Override='TRUE'><FieldRef Name='ID' /></OrderBy>
<Where><Eq><FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value></Eq></Where>
<ViewFields>
<FieldRef Name='Title'/><FieldRef Name='FileDirRef' /><FieldRef Name='FSObjType' /><FieldRef Name='ContentType' /><FieldRef Name='Modified' /><FieldRef Name='Editor' />
</ViewFields>
<RowLimit>100</RowLimit>
</Query></View>"),
                FolderServerRelativeUrl = folderICareAbout.ServerRelativeUrl,
                ListItemCollectionPosition = listItemCollectionPosition
            };
            ListItemCollection listItems = _srclist.GetItems(camlQuery);
            ctx.Load(listItems, intt => intt.Include(ipp => ipp.Id, ipp => ipp.DisplayName, ipp => ipp.Folder, ipp => ipp.ContentType));
            ctx.ExecuteQueryRetry();
            LogVerbose("Total items {0}", listItems.Count());

            List _dlist = ctx.Web.Lists.GetByTitle(Opts.DestinationLibraryName);
            ctx.Load(_dlist, f => f.Title, lf => lf.Id, lf => lf.RootFolder);
            ctx.Load(_dlist.RootFolder, rf => rf.Files, rf => rf.ServerRelativeUrl, rf => rf.Folders);
            ctx.ExecuteQueryRetry();

            foreach (var item in listItems)
            {
                var fitem = item.Folder;
                var serverPath = fitem.ServerRelativeUrl.Replace(folderICareAbout.ServerRelativeUrl, string.Empty);
                MoveFilesTo(fitem, _dlist.RootFolder);
            }

            return 1;
        }

        public void MoveFilesTo(Folder folder, Folder destinationFolder)
        {
            var ctx = (ClientContext)folder.Context;
            if (!ctx.Web.IsPropertyAvailable("ServerRelativeUrl"))
            {
                ctx.Load(ctx.Web, w => w.ServerRelativeUrl);
            }
            ctx.Load(folder, f => f.Files, f => f.ServerRelativeUrl, f => f.Folders);
            ctx.ExecuteQueryRetry();


            //Ensure target folder exists
            var folderName = folder.Name;
            destinationFolder = destinationFolder.ListEnsureFolder(folderName);
            LogVerbose("enumerating files under {0}", folder.ServerRelativeUrl);
            foreach (var file in folder.Files)
            {
                var targetFileUrl = file.ServerRelativeUrl.Replace(folder.ServerRelativeUrl, destinationFolder.ServerRelativeUrl);
                LogVerbose("   Folder:{0} To Target:{1}", folderName, targetFileUrl);
                //file.CopyTo(targetFileUrl, true);
                file.MoveTo(targetFileUrl, MoveOperations.Overwrite);
            }
            ctx.ExecuteQueryRetry();

            LogVerbose("enumerating folders under {0}", folder.ServerRelativeUrl);
            foreach (var subFolder in folder.Folders)
            {
                MoveFilesTo(subFolder, destinationFolder);
            }
        }
    }
}
