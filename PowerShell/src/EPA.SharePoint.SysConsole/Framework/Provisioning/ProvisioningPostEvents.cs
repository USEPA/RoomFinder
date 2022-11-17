using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Diagnostics;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using System;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class ProvisioningPostEvents
    {
        private readonly string logSource = "EPA.SharePoint.SysConsole.Framework.Provisioning.ProvisioningPostEvents";

        private TokenParser m_tokenParser;
        private PnPMonitoredScope m_scope;
        private ProvisioningTemplate m_template;
        private ProvisioningXmlParser m_xmlParser;

        public ProvisioningPostEvents(ProvisioningTemplate template, ProvisioningTemplateApplyingInformation applyingInformation, string configurationData)
        {
            m_template = template;
            m_xmlParser = new ProvisioningXmlParser(configurationData, template);
            m_scope = new PnPMonitoredScope(logSource);
        }


        public TokenParser ProvisionObjects(ClientContext clientContext)
        {

            var web = clientContext.Web;
            clientContext.Load(web, ctx => ctx.RootFolder.ServerRelativeUrl);
            clientContext.ExecuteQueryRetry();


            m_tokenParser = new TokenParser(web, m_template);


            m_scope.LogInfo("ProcessRequest. Template: {0}.  List Instances", m_template.Id);
            var listInstances = m_xmlParser.GetListInstancesFromConfiguration();
            foreach (var list in listInstances)
            {
                try
                {
                    List listInstance = web.GetListByUrl(list.ListUrl, pctx => pctx.Id, pctx => pctx.RootFolder.ServerRelativeUrl);
                    foreach (var ctype in list.ContentTypeBindings)
                    {
                        var contentType = listInstance.GetContentTypeByName(ctype.Name);
                        if (contentType != null)
                        {

                            web.Context.Load(contentType.FieldLinks, cf => cf.Include(inc => inc.Id, inc => inc.Name, flctx => flctx.DisplayName));
                            web.Context.ExecuteQueryRetry();

                            // Retreive the FieldLinks from the List
                            // These lists columns should have provisioned successfully from the PnP Provisioner
                            var sourceListColumns = listInstance.GetFields(ctype.FieldLinks.Select(s => s.InternalName).ToArray());
                            foreach (var column in sourceListColumns)
                            {
                                if (!contentType.FieldLinks.Any(a => a.Name == column.InternalName))
                                {
                                    m_scope.LogInfo("List {0} => Content Type {1} Adding Field {2}", list.ListUrl, ctype.Name, column.InternalName);

                                    var flink = new FieldLinkCreationInformation
                                    {
                                        Field = column
                                    };
                                    var flinkstub = contentType.FieldLinks.Add(flink);
                                    contentType.Update(false);
                                    listInstance.Context.Load(flinkstub, inc => inc.Id, inc => inc.Name, flctx => flctx.DisplayName);
                                    listInstance.Context.ExecuteQueryRetry();
                                }
                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    m_scope.LogError(ex, "Error updating list instance: {0}. Exception: {1}", list.ListUrl, ex.ToString());
                }
            }


            m_scope.LogInfo("ProcessRequest. Publishing Pages");
            var pages = m_xmlParser.GetPublishingPagesListFromConfiguration();
            foreach (var page in pages)
            {
                try
                {
                    if (page.IsPublishingPage)
                    {
                        ProvisioningHelper.AddPublishingPage(clientContext, web, page);
                    }
                    else
                    {
                        // This is a List Page File
                        List listinstance = web.GetListByUrl(page.ListUrl, pctx => pctx.Id, pctx => pctx.RootFolder.ServerRelativeUrl);
                        if (!listinstance.IsPropertyAvailable(lctx => lctx.RootFolder))
                        {
                            listinstance.EnsureProperties(lctx => lctx.RootFolder, lctx => lctx.RootFolder.ServerRelativeUrl);
                        }

                        ProvisioningHelper.AddWebpartsToPage(clientContext, web, listinstance, page, m_tokenParser, m_scope);
                    }
                }
                catch (Exception ex)
                {
                    m_scope.LogError(ex, "Error adding publishing page: {0}. Exception: {1}", page.FileName, ex.ToString());
                }
            }

            return m_tokenParser;
        }



        private Nullable<bool> _willProvision;
        public bool WillProvision(Web web, ProvisioningTemplate template)
        {
            if (!_willProvision.HasValue)
            {
                _willProvision = template.Security != null && (
                                  template.Security.SiteGroups.Any() ||
                                  template.Security.SiteSecurityPermissions.RoleAssignments.Any() ||
                                  template.Security.SiteSecurityPermissions.RoleDefinitions.Any());
                if (_willProvision == true)
                {
                    // if subweb and site inheritance is not broken
                    if (web.IsSubSite() && template.Security.BreakRoleInheritance == false
                        && web.EnsureProperty(w => w.HasUniqueRoleAssignments) == false)
                    {
                        _willProvision = false;
                    }
                }
            }

            return _willProvision.Value;

        }

    }
}
