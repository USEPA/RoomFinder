using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("getEPASiteColumn", HelpText = "Checks tenant scanning sites for a column.")]
    public class GetEPASiteColumnOptions : TenantCommandOptions
    {
        /// <summary>
        /// The field for which we are scanning
        /// </summary>
        [Option("column", Required = false)]
        public string FieldColumnName { get; set; }

        /// <summary>
        /// The Absolute URL for the site we will scan
        /// </summary>
        [Option("site-url", Required = false)]
        public string SiteUrl { get; set; }

        [Option('d', "log-directory", Required = true, HelpText = "Directory path for logging location")]
        public string LogDirectory { get; set; }
    }

    public static class GetEPASiteColumnOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPASiteColumnOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPASiteColumn(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Query the tenant to discover all locations where the site column is present
    /// </summary>
    /// <remarks>Identify SharePoint groups that have the everyone group added to and replaces it with the UG-EPA-Employees group</remarks>
    public class GetEPASiteColumn : BaseSpoTenantCommand<GetEPASiteColumnOptions>
    {
        public GetEPASiteColumn(GetEPASiteColumnOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private Variables

        private List<ExtendSPOSiteModel> SiteActionLog { get; set; }

        private string TenantUrl { get; set; }

        #endregion

        public override void OnInit()
        {
            TenantUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "csom unknown errors.")]
        public override int OnRun()
        {
            TenantContext.EnsureProperties(tssp => tssp.RootSiteUrl);
            TenantUrl = TenantContext.RootSiteUrl;

            SiteActionLog = new List<ExtendSPOSiteModel>();

            LogVerbose("Enumerating Site collections #|# UserID: {0} ......... ", CurrentUserName);

            var collectionOfSites = GetSiteCollections(true);
            foreach (var siteCollection in collectionOfSites.Where(w =>
                (!string.IsNullOrEmpty(Opts.SiteUrl) && w.Url.IndexOf(Opts.SiteUrl, StringComparison.CurrentCultureIgnoreCase) > -1) || (string.IsNullOrEmpty(Opts.SiteUrl) && 1 == 1)))
            {
                var _siteUrl = siteCollection.Url;
                var _totalWebs = siteCollection.WebsCount;
                LogVerbose("Processing {0} owner {1}", siteCollection.Title, siteCollection.Owner);

                try
                {
                    SetSiteAdmin(_siteUrl, CurrentUserName, true);

                    using var siteContext = this.ClientContext.Clone(_siteUrl);
                    Web _web = siteContext.Web;
                    var extendedModel = new ExtendSPOSiteModel(siteCollection);

                    extendedModel = ProcessSiteCollectionSubWeb(extendedModel, _web);
                    // Add the Site collection to the report
                    SiteActionLog.Add(extendedModel);
                }
                catch (Exception e)
                {
                    LogError(e, "Failed to processSiteCollection with url {0}", _siteUrl);
                }
            }

            if (ShouldProcess("Writing file to disc"))
            {
                // Create JSON Directory if it does not exists
                var jsonString = JsonConvert.SerializeObject(SiteActionLog, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 5,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                var outFile = System.IO.Path.Combine(Opts.LogDirectory, $"everycolumnresults-{DateTime.UtcNow:yyyy-MM-dd_HH_mm_ss}.json");
                System.IO.File.WriteAllText(outFile, jsonString, System.Text.Encoding.UTF8);
            }


            return 1;
        }

        /// <summary>
        /// Process the site subweb
        /// </summary>
        /// <param name="model"></param>
        /// <param name="_web"></param>
        private ExtendSPOSiteModel ProcessSiteCollectionSubWeb(ExtendSPOSiteModel model, Web _web)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (_web is null)
            {
                throw new ArgumentNullException(nameof(_web));
            }

            try
            {
                _web.EnsureProperties(spp => spp.Id, spp => spp.Url);
                var _siteUrl = _web.Url;
                var _rootSiteIndex = _siteUrl.ToLower().IndexOf(TenantUrl);

                LogVerbose("Processing web URL {0} SKIPPING:{1}", _siteUrl, (_rootSiteIndex == -1));
                if (_rootSiteIndex > -1)
                {

                    var sitemodel = ProcessSite(_web);
                    if (sitemodel.Id.HasValue)
                    {
                        model.Sites.Add(sitemodel);
                    }

                    //Process subsites
                    _web.Context.Load(_web.Webs);
                    _web.Context.ExecuteQueryRetry();

                    if (model.WebsCount > 1)
                    {
                        LogVerbose("Site {0} has webs {1}", _siteUrl, model.WebsCount);
                        foreach (Web _inWeb in _web.Webs)
                        {
                            model = ProcessSiteCollectionSubWeb(model, _inWeb);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in processSiteCollection");
            }

            return model;
        }


        private SPSiteModel ProcessSite(Web _web)
        {
            var hasListFound = false;
            var model = new SPSiteModel();

            _web.EnsureProperties(wssp => wssp.Id,
                wspp => wspp.ServerRelativeUrl,
                wspp => wspp.Title,
                wssp => wssp.HasUniqueRoleAssignments,
                wssp => wssp.SiteUsers,
                wssp => wssp.Url,
                wssp => wssp.Lists,
                wssp => wssp.ContentTypes.Include(
                    lcnt => lcnt.Id,
                    lcnt => lcnt.Name,
                    lcnt => lcnt.StringId,
                    lcnt => lcnt.Description,
                    lcnt => lcnt.DocumentTemplate,
                    lcnt => lcnt.Group,
                    lcnt => lcnt.Hidden,
                    lcnt => lcnt.JSLink,
                    lcnt => lcnt.SchemaXml,
                    lcnt => lcnt.Scope,
                    lcnt => lcnt.FieldLinks.Include(
                        lcntlnk => lcntlnk.Id,
                        lcntlnk => lcntlnk.Name,
                        lcntlnk => lcntlnk.Hidden,
                        lcntlnk => lcntlnk.Required
                        ),
                    lcnt => lcnt.Fields.Include(
                        lcntfld => lcntfld.FieldTypeKind,
                        lcntfld => lcntfld.InternalName,
                        lcntfld => lcntfld.Id,
                        lcntfld => lcntfld.Group,
                        lcntfld => lcntfld.Title,
                        lcntfld => lcntfld.Hidden,
                        lcntfld => lcntfld.Description,
                        lcntfld => lcntfld.JSLink,
                        lcntfld => lcntfld.Indexed,
                        lcntfld => lcntfld.Required,
                        lcntfld => lcntfld.SchemaXml)),
                wssp => wssp.Fields.Include(
                    lcntfld => lcntfld.FieldTypeKind,
                    lcntfld => lcntfld.InternalName,
                    lcntfld => lcntfld.Id,
                    lcntfld => lcntfld.Group,
                    lcntfld => lcntfld.Title,
                    lcntfld => lcntfld.Hidden,
                    lcntfld => lcntfld.Description,
                    lcntfld => lcntfld.JSLink,
                    lcntfld => lcntfld.Indexed,
                    lcntfld => lcntfld.Required,
                    lcntfld => lcntfld.SchemaXml));
            model.Url = _web.Url;
            model.title = _web.Title;
            LogVerbose("Processing: {0}", _web.Url);


            /* Process Fields */
            try
            {
                foreach (var _fields in _web.Fields)
                {
                    if (string.IsNullOrEmpty(Opts.FieldColumnName) ||
                        _fields.Title.Equals(Opts.FieldColumnName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        hasListFound = true;

                        model.FieldDefinitions.Add(new SPFieldDefinitionModel()
                        {
                            FieldTypeKind = _fields.FieldTypeKind,
                            InternalName = _fields.InternalName,
                            FieldGuid = _fields.Id,
                            GroupName = _fields.Group,
                            Title = _fields.Title,
                            HiddenField = _fields.Hidden,
                            Description = _fields.Description,
                            JSLink = _fields.JSLink,
                            FieldIndexed = _fields.Indexed,
                            Required = _fields.Required,
                            SchemaXml = _fields.SchemaXml
                        });
                    }
                };
            }
            catch (Exception e)
            {
                LogError(e, "Failed to retrieve site owners {0}", _web.Url);
            }

            /* Process Content Type */
            try
            {
                foreach (var _ctypes in _web.ContentTypes)
                {
                    var cmodel = new SPContentTypeDefinition()
                    {
                        ContentTypeId = _ctypes.StringId,
                        Name = _ctypes.Name,
                        Description = _ctypes.Description,
                        DocumentTemplate = _ctypes.DocumentTemplate,
                        ContentTypeGroup = _ctypes.Group,
                        Hidden = _ctypes.Hidden,
                        JSLink = _ctypes.JSLink,
                        Scope = _ctypes.Scope
                    };

                    foreach (var _ctypeFields in _ctypes.FieldLinks)
                    {
                        if (string.IsNullOrEmpty(Opts.FieldColumnName) ||
                            _ctypeFields.Name.Equals(Opts.FieldColumnName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            cmodel.FieldLinks.Add(new SPFieldLinkDefinitionModel()
                            {
                                Name = _ctypeFields.Name,
                                Id = _ctypeFields.Id,
                                Hidden = _ctypeFields.Hidden,
                                Required = _ctypeFields.Required
                            });
                        }
                    }

                    if (cmodel.FieldLinks.Any())
                    {
                        hasListFound = true;
                        model.ContentTypes.Add(cmodel);
                    }
                };
            }
            catch (Exception e)
            {
                LogError(e, "Failed to retrieve site owners {0}", _web.Url);
            }

            // ********** Process List
            try
            {
                var lists = ProcessList(_web);
                if (lists.Any())
                {
                    hasListFound = true;
                    model.Lists.AddRange(lists);
                }
            }
            catch (Exception e)
            {
                LogError(e, "Exception occurred in processSite");
            }

            if (hasListFound)
            {
                // setting ID to indicate to parent consumer that this entity has unique permissions in the TREE
                model.Id = _web.Id;
            }

            return model;
        }

        private IList<SPListDefinition> ProcessList(Web _web)
        {
            var model = new List<SPListDefinition>();

            // ********** Process Lists
            ListCollection _lists = _web.Lists;
            _web.Context.Load(_lists,
                spp => spp.Include(
                    sppi => sppi.Id,
                    sppi => sppi.Title,
                    sppi => sppi.RootFolder.ServerRelativeUrl,
                    sppi => sppi.HasUniqueRoleAssignments,
                    sppi => sppi.Hidden,
                    sppi => sppi.IsSystemList,
                    sppi => sppi.IsPrivate,
                    sppi => sppi.IsApplicationList,
                    sppi => sppi.IsCatalog,
                    sppi => sppi.IsSiteAssetsLibrary));

            _web.Context.ExecuteQueryRetry();

            // Restrict to natural lists or custom lists
            foreach (List _list in _lists.Where(sppi
                            => !sppi.IsPrivate
                            && !sppi.IsSystemList
                            && !sppi.IsApplicationList))
            {
                var hasListFound = false;
                var listContext = _list.Context;
                LogVerbose("Enumerating List {0} URL:{1}", _list.Title, _list.RootFolder.ServerRelativeUrl);

                try
                {
                    listContext.Load(_list,
                        lssp => lssp.Id,
                        lssp => lssp.Title,
                        lssp => lssp.HasUniqueRoleAssignments,
                        lssp => lssp.Title,
                        lssp => lssp.Hidden,
                        lssp => lssp.IsSystemList,
                        lssp => lssp.IsPrivate,
                        lssp => lssp.IsApplicationList,
                        lssp => lssp.IsCatalog,
                        lssp => lssp.IsSiteAssetsLibrary,
                        lssp => lssp.RootFolder.ServerRelativeUrl,
                        lssp => lssp.ContentTypes.Include(
                            lcnt => lcnt.Id,
                            lcnt => lcnt.Name,
                            lcnt => lcnt.StringId,
                            lcnt => lcnt.Description,
                            lcnt => lcnt.DocumentTemplate,
                            lcnt => lcnt.Group,
                            lcnt => lcnt.Hidden,
                            lcnt => lcnt.JSLink,
                            lcnt => lcnt.SchemaXml,
                            lcnt => lcnt.Scope,
                            lcnt => lcnt.FieldLinks.Include(
                                lcntlnk => lcntlnk.Id,
                                lcntlnk => lcntlnk.Name,
                                lcntlnk => lcntlnk.Hidden,
                                lcntlnk => lcntlnk.Required
                                ),
                            lcnt => lcnt.Fields.Include(
                                lcntfld => lcntfld.FieldTypeKind,
                                lcntfld => lcntfld.InternalName,
                                lcntfld => lcntfld.Id,
                                lcntfld => lcntfld.Group,
                                lcntfld => lcntfld.Title,
                                lcntfld => lcntfld.Hidden,
                                lcntfld => lcntfld.Description,
                                lcntfld => lcntfld.JSLink,
                                lcntfld => lcntfld.Indexed,
                                lcntfld => lcntfld.Required,
                                lcntfld => lcntfld.SchemaXml)),
                        lssp => lssp.Fields.Include(
                            lcntfld => lcntfld.FieldTypeKind,
                            lcntfld => lcntfld.InternalName,
                            lcntfld => lcntfld.Id,
                            lcntfld => lcntfld.Group,
                            lcntfld => lcntfld.Title,
                            lcntfld => lcntfld.Hidden,
                            lcntfld => lcntfld.Description,
                            lcntfld => lcntfld.JSLink,
                            lcntfld => lcntfld.Indexed,
                            lcntfld => lcntfld.Required,
                            lcntfld => lcntfld.SchemaXml));
                    listContext.ExecuteQueryRetry();

                    var listModel = new SPListDefinition()
                    {
                        Id = _list.Id,
                        HasUniquePermission = _list.HasUniqueRoleAssignments,
                        ListName = _list.Title,
                        ServerRelativeUrl = _list.RootFolder.ServerRelativeUrl,
                        Hidden = _list.Hidden,
                        IsSystemList = _list.IsSystemList,
                        IsPrivate = _list.IsPrivate,
                        IsApplicationList = _list.IsApplicationList,
                        IsCatalog = _list.IsCatalog,
                        IsSiteAssetsLibrary = _list.IsSiteAssetsLibrary
                    };

                    /* Process Fields */
                    try
                    {
                        foreach (var _fields in _list.Fields)
                        {
                            if (string.IsNullOrEmpty(Opts.FieldColumnName) ||
                                _fields.Title.Equals(Opts.FieldColumnName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                hasListFound = true;

                                listModel.FieldDefinitions.Add(new SPFieldDefinitionModel()
                                {
                                    FieldTypeKind = _fields.FieldTypeKind,
                                    InternalName = _fields.InternalName,
                                    FieldGuid = _fields.Id,
                                    GroupName = _fields.Group,
                                    Title = _fields.Title,
                                    HiddenField = _fields.Hidden,
                                    Description = _fields.Description,
                                    JSLink = _fields.JSLink,
                                    FieldIndexed = _fields.Indexed,
                                    Required = _fields.Required,
                                    SchemaXml = _fields.SchemaXml
                                });
                            }
                        };
                    }
                    catch (Exception e)
                    {
                        LogError(e, "Failed to retrieve site owners {0}", _web.Url);
                    }

                    foreach (var _ctypes in _list.ContentTypes)
                    {
                        var cmodel = new SPContentTypeDefinition()
                        {
                            ContentTypeId = _ctypes.StringId,
                            Name = _ctypes.Name,
                            Description = _ctypes.Description,
                            DocumentTemplate = _ctypes.DocumentTemplate,
                            ContentTypeGroup = _ctypes.Group,
                            Hidden = _ctypes.Hidden,
                            JSLink = _ctypes.JSLink,
                            Scope = _ctypes.Scope
                        };

                        foreach (var _ctypeFields in _ctypes.FieldLinks)
                        {
                            if (string.IsNullOrEmpty(Opts.FieldColumnName) ||
                                _ctypeFields.Name.Equals(Opts.FieldColumnName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                cmodel.FieldLinks.Add(new SPFieldLinkDefinitionModel()
                                {
                                    Name = _ctypeFields.Name,
                                    Id = _ctypeFields.Id,
                                    Hidden = _ctypeFields.Hidden,
                                    Required = _ctypeFields.Required
                                });
                            }
                        }

                        if (cmodel.FieldLinks.Any())
                        {
                            hasListFound = true;
                            listModel.ContentTypes.Add(cmodel);
                        }
                    }

                    if (hasListFound)
                    {
                        model.Add(listModel);
                    }
                }
                catch (Exception e)
                {
                    LogError(e, "Failed in ProcessList");
                }
            }
            return model;
        }
    }
}
