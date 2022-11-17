using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using Microsoft.SharePoint.Client.UserProfiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEPADocumentMetadata", HelpText = "scan documents and replace metadata with author/modify by profile information.")]
    public class ScanEPADocumentMetadataOptions : TenantCommandOptions
    {
        /// <summary>
        /// The CSV file containing OneDrive sites to scan 
        ///     Example: C:\Data\Scripts\EPA-Documents Metadata\ODFB-Sites.csv
        /// </summary>
        [Option("onedrive-csv", Required = true)]
        public string OneDriveFilePath { get; set; }
    }

    public static class ScanEPADocumentMetadataOptionsExtension
    {
        /// <summary>
        /// Will execute the Scan EPA Site Metadata command, processing documents and adding metadata
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this ScanEPADocumentMetadataOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPADocumentMetadata(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class ScanEPADocumentMetadata : BaseSpoTenantCommand<ScanEPADocumentMetadataOptions>
    {
        public ScanEPADocumentMetadata(ScanEPADocumentMetadataOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private Variables

        private string[] _oobDocLibs { get; set; }

        private string _renderingScript = "";

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

            if (!System.IO.File.Exists(Opts.OneDriveFilePath))
            {
                throw new System.IO.FileNotFoundException("File not found", Opts.OneDriveFilePath);
            }

            _oobDocLibs = new string[] {
                "Maintenance Log Library",
                "Master Page Gallery",
                "Site Assets",
                "Style Library",
                "Reporting Templates",
                "Drop Off Library",
                "Site Pages",
                "Form Templates",
                "wfpub",
                "Workflows",
                "Images",
                "Pages",
                "Page",
                "App Packages",
                "Request Forms",
                "Forms",
                "Access Requests",
                "Badges",
                "Cache Profiles",
                "Device Channels",
                "Categories",
                "Reports List"
             };
        }

        public override int OnRun()
        {
            try
            {
                TenantContext.EnsureProperties(tctx => tctx.RootSiteUrl);
                var _tenantUrl = TenantContext.RootSiteUrl;

                // Queries all Sites looking for a Metadata library
                // var searchSites = GetSites("Org");

                _renderingScript = Settings.Commands.RenderingScript;

                GetODFBSites();
            }
            catch (Exception exception1)
            {
                LogError(exception1, "Failed to process OneDrive Sites {0}", exception1.Message);
                return -1;
            }

            return 1;
        }


        private void GetODFBSites()
        {
            var onedrivesites = new List<string>();

            try
            {
                using StreamReader reader = new StreamReader(Opts.OneDriveFilePath);
                while (reader.Peek() >= 0)
                {
                    string text1 = reader.ReadLine();
                    if (!string.IsNullOrEmpty(text1))
                    {
                        onedrivesites.Add(text1.Trim());
                    }
                }
            }
            catch (Exception exception)
            {
                LogError(exception, "The process failed: {0}", exception.ToString());
            }

            try
            {
                foreach (var onedrivesite in onedrivesites)
                {
                    ProcessSiteCollection(onedrivesite);
                }
            }
            catch (Exception exception)
            {
                LogError(exception, "The process failed: {0}", exception.ToString());
            }
        }

        private string GetOwner(ClientContext context)
        {
            string email = "";

            try
            {
                Site clientObject = context.Site;
                User owner = clientObject.Owner;
                context.Load<Site>(clientObject, sctx => sctx.Id);
                context.Load<User>(owner, octx => octx.Email);
                context.ExecuteQueryRetry();

                email = owner.Email;
            }
            catch (Exception exception1)
            {
                LogError(exception1, exception1.ToString());
            }

            return email;
        }


        private void GetSites()
        {
            var list = GetSiteCollections();

            foreach (var site in list)
            {
                var str = site.Url;

                try
                {
                    LogVerbose(str);
                    bool flag = false;
                    if (str.ToLower().IndexOf("_application") > -1)
                    {
                        flag = true;
                    }
                    if (str.ToLower().IndexOf("_custom") > -1)
                    {
                        flag = true;
                    }
                    if (str.ToLower().IndexOf("_development") > -1)
                    {
                        flag = true;
                    }
                    if (str.ToLower().IndexOf("test") > -1)
                    {
                        flag = true;
                    }

                    if (!flag)
                    {
                        ProcessSiteCollection(str);
                    }
                    else
                    {
                        LogWarning("Exception on site: {0}", str);
                    }
                }
                catch (Exception exception1)
                {
                    LogError(exception1, "Failed in getSites {0}", exception1.Message);
                }
            }
        }

        private List<QueryResults> GetSites(string Cat)
        {
            var context = TenantContext.Context;
            var keywordQueryValue = string.Empty;


            if (Cat == "")
            {
                keywordQueryValue = "isDocument:\"true\"";
            }
            else if (Cat == "Org")
            {
                keywordQueryValue = "MetaData AND About.aspx AND (contentclass:STS_ListItem OR IsDocument:True)";
            }

            var results = new List<QueryResults>();
            int totalRows = 0;
            int startRow = 0;
            int startIncrementor = 500;


            SearchExecutor searchExec = new SearchExecutor(TenantContext.Context);
           

            while (true)
            {
                KeywordQuery keywordQuery = new KeywordQuery(context)
                {
                    QueryText = keywordQueryValue,
                    StartRow = startRow,
                    RowLimit = startIncrementor
                };
                keywordQuery.SelectProperties.Add("Title");
                keywordQuery.SelectProperties.Add("SPSiteUrl");
                keywordQuery.SelectProperties.Add("Path");
                keywordQuery.SelectProperties.Add("Created");
                keywordQuery.SortList.Add("Created", SortDirection.Descending);

                ClientResult<ResultTableCollection> result = searchExec.ExecuteQuery(keywordQuery);
                TenantContext.Context.ExecuteQueryRetry();

                if ((result != null) && (result.Value[0].RowCount > 0))
                {
                    totalRows = result.Value[0].TotalRows;
                    LogVerbose("Found total {0} in query at start rows {1}", totalRows, startRow);
                    foreach (IDictionary<string, object> dictionary in result.Value[0].ResultRows)
                    {
                        var qresult = new QueryResults()
                        {
                            title = dictionary["Title"].ToString(),
                            siteUrl = dictionary["SPSiteUrl"].ToString(),
                            created = dictionary["Created"].ToString(),
                            path = dictionary["Path"].ToString()
                        };
                        results.Add(qresult);
                        LogVerbose(qresult.ToString());
                    }

                    startRow += startIncrementor;
                }
                else
                {
                    break;
                }
            }

            return results;
        }



        private void ProcessDocument(string _siteUrl, string _documentPath, string _fullPath)
        {
            bool _changed = false;
            using ClientContext context = new ClientContext(_siteUrl);
            Web web = context.Web;
            Microsoft.SharePoint.Client.File fileByServerRelativeUrl = context.Web.GetFileByServerRelativeUrl(_documentPath);
            context.Load(fileByServerRelativeUrl);
            context.Load(fileByServerRelativeUrl.ListItemAllFields);
            context.ExecuteQuery();

            ListItem listItemAllFields = fileByServerRelativeUrl.ListItemAllFields;
            context.Load(listItemAllFields);
            context.ExecuteQuery();

            try
            {
                context.Load(fileByServerRelativeUrl);
                context.Load(listItemAllFields);
                context.ExecuteQuery();
            }
            catch (Exception)
            {
                char[] separator = new char[] { '/' };
                string[] strArray = _fullPath.Replace(_siteUrl, "").Split(separator);
                if ((strArray.Length > 3) && (strArray[1] != ""))
                {
                    ProcessDocument(_siteUrl + "/" + strArray[1], _documentPath, _fullPath);
                }
            }

            Console.WriteLine("Item:" + listItemAllFields["Title"]);
            FieldUserValue value2 = null;
            User clientObject = null;
            try
            {
                value2 = (FieldUserValue)listItemAllFields["Editor"];
                clientObject = web.GetUserById(value2.LookupId);
                context.Load<User>(clientObject);
                context.ExecuteQuery();
            }
            catch (Exception exception2)
            {
                Console.WriteLine(exception2);
            }

            DateTime now = DateTime.Now;

            try
            {
                now = (DateTime)listItemAllFields["Modified"];
            }
            catch (Exception)
            {
            }

            try
            {
                if (string.IsNullOrEmpty((string)listItemAllFields["Title"]))
                {
                    listItemAllFields["Title"] = Path.GetFileNameWithoutExtension((string)listItemAllFields["FileLeafRef"]);
                    listItemAllFields.Update();
                    _changed = true;
                }
            }
            catch (Exception exception4)
            {
                Console.WriteLine(exception4);
            }

            try
            {
                if (listItemAllFields["Creator"] == null)
                {
                    User user2 = context.Web.EnsureUser(listItemAllFields["Created_x0020_By"].ToString());
                    context.Load<User>(user2, new Expression<Func<User, object>>[0]);
                    context.ExecuteQuery();
                    listItemAllFields["Creator"] = user2;
                    listItemAllFields.Update();
                    _changed = true;
                }
            }
            catch (Exception exception5)
            {
                Console.WriteLine(exception5);
            }

            try
            {
                if (listItemAllFields["Document_x0020_Creation_x0020_Date"] == null)
                {
                    listItemAllFields["Document_x0020_Creation_x0020_Date"] = listItemAllFields["Created"];
                    listItemAllFields.Update();
                    _changed = true;
                }
            }
            catch (Exception exception6)
            {
                Console.WriteLine(exception6);
            }

            try
            {
                if (string.IsNullOrEmpty((string)listItemAllFields["EPA_x0020_Office"]))
                {
                    PeopleManager peopleManager = new PeopleManager(context);
                    PersonProperties propertiesFor = peopleManager.GetPropertiesFor((string)listItemAllFields["Created_x0020_By"]);

                    context.Load<PersonProperties>(propertiesFor, p => p.AccountName, p => p.UserProfileProperties);
                    context.ExecuteQuery();

                    foreach (var pair in propertiesFor.UserProfileProperties)
                    {
                        if (pair.Key.ToString() == "Department")
                        {
                            listItemAllFields["EPA_x0020_Office"] = pair.Value.ToString();
                            listItemAllFields.Update();
                            break;
                        }
                    }
                }
            }
            catch (Exception exception7)
            {
                Console.WriteLine(exception7);
            }


            try
            {
                if ((listItemAllFields["Record"] == null) || (listItemAllFields["Record"].ToString() == "None"))
                {
                    listItemAllFields["Record"] = "Shared";
                    listItemAllFields.Update();
                    _changed = true;
                }
            }
            catch (Exception exception8)
            {
                Console.WriteLine(exception8);
            }

            if (_changed)
            {
                Console.WriteLine("............ Updating: " + _documentPath);
                if (clientObject != null)
                {
                    listItemAllFields["Editor"] = clientObject;
                    listItemAllFields.Update();
                }
                try
                {
                    listItemAllFields["Modified"] = now;
                    listItemAllFields.Update();
                    context.ExecuteQuery();
                }
                catch (Exception exception9)
                {
                    Console.WriteLine(exception9);
                }
            }
        }

        private void ProcessDocumentLibrary(ClientContext ctx, Web _web, List _docLib, string _region, string _siteOwner, out List<DocumentItem> results)
        {
            results = new List<DocumentItem>();
            try
            {
                ListItemCollectionPosition listItemCollectionPosition = null;
                do
                {
                    CamlQuery query = CamlQuery.CreateAllItemsQuery(500, new string[0]);
                    query.ListItemCollectionPosition = listItemCollectionPosition;
                    query.ViewXml = @"<Query>
<OrderBy><FieldRef Name='FileDirRef' /></OrderBy>
</Query>
<ViewFields>
<FieldRef Name='ID' />
<FieldRef Name='LinkFilename' />
<FieldRef Name='FileDirRef' />
<FieldRef Name='FileLeafRef' />
<FieldRef Name='Title' />
<FieldRef Name='Record' />
<FieldRef Name='EPA_x0020_Office' />
<FieldRef Name='Document_x0020_Creation_x0020_Date' />
<FieldRef Name='Author' />
</ViewFields>
<QueryOptions>
<ViewAttributes Scope='Recursive' />
<OptimizeFor>FolderUrls</OptimizeFor>
</QueryOptions>";

                    ListItemCollection clientObject = _docLib.GetItems(query);
                    ctx.Load<ListItemCollection>(clientObject, new Expression<Func<ListItemCollection, object>>[0]);
                    ctx.ExecuteQuery();
                    listItemCollectionPosition = clientObject.ListItemCollectionPosition;

                    foreach (ListItem item in clientObject)
                    {
                        var dresult = new DocumentItem()
                        {
                            url = _web.Url,
                            region = _region,
                            owner = _siteOwner,
                            fileleafref = item.RetrieveListItemValue("FileLeafRef"),
                            title = item.RetrieveListItemValue("Title"),
                            epa_office = item.RetrieveListItemValue("EPA_x0020_Office"),
                            record = item.RetrieveListItemValue("Record"),
                            document_creation_date = item.RetrieveListItemValue("Document_x0020_Creation_x0020_Date"),
                            modified = item.RetrieveListItemValue("Modified"),
                            author = item.RetrieveListItemUserValue("Author").ToUserEmailValue()
                        };
                        results.Add(dresult);
                        LogVerbose(dresult.ToString());
                    }
                }
                while (listItemCollectionPosition != null);
            }
            catch (Exception exception1)
            {
                LogError(exception1, "Failed to query document library {0}", exception1.Message);
            }
        }


        private void ProcessSiteCollection(string _siteUrl)
        {
            try
            {
                SetSiteAdmin(_siteUrl, CurrentUserName, true);

                using var context = this.ClientContext.Clone(_siteUrl);
                Web clientObject = context.Web;
                context.Load(clientObject);
                context.Load(clientObject.Webs);
                context.ExecuteQueryRetry();

                ProcessSite(clientObject.Url);

                foreach (var _inWebs in clientObject.Webs)
                {
                    ProcessSubSites(_inWebs, context);
                }
            }
            catch (Exception exception1)
            {
                LogError(exception1, "Failed to processSiteCollection {0}", exception1.Message);
            }
        }

        private void ProcessSite(string _siteUrl)
        {
            try
            {
                ProcessSiteDocuments(_siteUrl);
            }
            catch (Exception exception1)
            {
                LogError(exception1, "Failed to processSite {0}", exception1.Message);
            }
        }

        private void ProcessSiteDocuments(string _siteUrl)
        {
            var documents = new List<DocumentItem>();
            try
            {
                using var context = this.ClientContext.Clone(_siteUrl);
                Web clientObject = context.Web;
                context.Load<Web>(clientObject);
                context.ExecuteQueryRetry();

                var _region = "";
                var _siteOwner = GetOwner(context);

                if (clientObject.Url.ToLower().Contains("https://usepa-my."))
                {
                    _region = "ODFB";
                }
                else
                {
                    var siteType = GetRegionSiteType(_siteUrl);
                    _region = siteType.Region;
                }

                var lists = context.LoadQuery(clientObject.Lists.Where(cltx =>
                    (cltx.BaseTemplate == 0x65 && cltx.BaseType == BaseType.DocumentLibrary)
                    || (cltx.BaseTemplate == 0x2bc && cltx.BaseType == BaseType.DocumentLibrary)));
                context.ExecuteQueryRetry();

                foreach (List list in lists)
                {
                    if (!_oobDocLibs.Contains<string>(list.Title))
                    {
                        LogVerbose("Discovering document metadata for {0}", list.Title);

                        ContentTypeCollection contentTypes = list.ContentTypes;
                        context.Load(contentTypes);
                        context.ExecuteQueryRetry();

                        ProcessDocumentLibrary(context, clientObject, list, _region, _siteOwner, out documents);
                    }
                }
            }
            catch (Exception exception1)
            {
                LogError(exception1, "Failed to processSiteDocuments {0}", exception1.Message);
            }
        }

        private void ProcessSubSites(Web _web, ClientContext ctx)
        {
            ctx.Load<WebCollection>(_web.Webs);
            ctx.ExecuteQueryRetry();

            ProcessSite(_web.Url);

            foreach (var _inWebs in _web.Webs)
            {
                ProcessSubSites(_inWebs, ctx);
            }
        }

        public class DocumentItem
        {
            public string url { get; set; }
            public string region { get; set; }
            public string owner { get; set; }
            public string fileleafref { get; set; }
            public string title { get; set; }
            public string epa_office { get; set; }
            public string record { get; set; }
            public string document_creation_date { get; set; }
            public string modified { get; set; }
            public string author { get; set; }

            public override string ToString()
            {
                return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", url, region, owner, fileleafref, title, epa_office, record, document_creation_date, modified, author);
            }
        }

        public class QueryResults
        {
            public string title { get; set; }
            public string siteUrl { get; set; }
            public string created { get; set; }
            public string path { get; set; }
            public override string ToString()
            {
                return string.Format(" >> {0} -- {1} -- {2} -- {3}", title, siteUrl, created, path);
            }
        }

        public class EpaSite
        {
            public string Description { get; set; }

            public string Title { get; set; }

            public string Url { get; set; }

            public string WebTemplate { get; set; }
        }
    }
}

