using EPA.Office365.Extensions;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Publishing.Navigation;
using OfficeDevPnP.Core.Diagnostics;
using OfficeDevPnP.Core.Framework.Provisioning.Extensibility;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using Enums = OfficeDevPnP.Core.Enums;
using Model = OfficeDevPnP.Core.Framework.Provisioning.Model;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class ProvisioningExtensibilityHandler : IProvisioningExtensibilityHandler
    {
        #region Private Variables

        private ClientContext m_clientContext { get; set; }
        private ProvisioningTemplate m_template { get; set; }
        private ProvisioningTemplateApplyingInformation m_applyingInformation { get; set; }
        private TokenParser m_tokenParser { get; set; }
        private PnPMonitoredScope m_scope { get; set; }
        private ProvisioningXmlParser m_xmlParser { get; set; }

        #endregion

        #region IProvisioningExtensibilityHandler Implementation

        /// <summary>
        /// Provision items/objects after the ApplyTemplate
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="template"></param>
        /// <param name="applyingInformation"></param>
        /// <param name="tokenParser"></param>
        /// <param name="scope"></param>
        /// <param name="configurationData">NOTE: While this should contain XML; we are going to use a file path and parse the data POST application</param>
        public void Provision(ClientContext ctx, ProvisioningTemplate template, ProvisioningTemplateApplyingInformation applyingInformation, TokenParser tokenParser, PnPMonitoredScope scope, string configurationData)
        {
            m_clientContext = ctx;
            m_template = template;
            m_applyingInformation = applyingInformation;
            m_scope = scope;
            m_tokenParser = tokenParser;


            var web = m_clientContext.Web;
            m_clientContext.Load(web, wctx => wctx.RootFolder.ServerRelativeUrl);
            m_clientContext.ExecuteQueryRetry();

            // TODO: related to a previous bug if creating lists, invalid URL's show up
            m_tokenParser = new TokenParser(web, template);
            m_tokenParser.AddToken(new ProvisioningSiteToken(web)); // add ~fullsiteurl token

            // TODO: The provisioner is wack!
            var configurationDirectory = GetContainer();
            var configurationDataXml = configurationDirectory.RetreiveWebPartContents(configurationData);
            m_xmlParser = new ProvisioningXmlParser(configurationDataXml, template);


            Process();
        }

        public ProvisioningTemplate Extract(ClientContext ctx, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInformation, PnPMonitoredScope scope, string configurationData)
        {
            return template;
        }

        /// <summary>
        /// Extend and add fullsiteurl token to the tokens to be parsed
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="template"></param>
        /// <param name="configurationData"></param>
        /// <returns></returns>
        public IEnumerable<TokenDefinition> GetTokens(ClientContext ctx, ProvisioningTemplate template, string configurationData)
        {
            var existingTokens = new List<TokenDefinition>
            {
                new ProvisioningSiteToken(ctx.Web)
            };
            return existingTokens;
        }

        #endregion

        #region Utils

        public const string CONTAINER = "ConnectionString";

        internal string GetContainer()
        {
            if (m_template.Connector.Parameters.ContainsKey(CONTAINER))
            {
                return m_template.Connector.Parameters[CONTAINER].ToString();
            }
            else
            {
                throw new Exception("No container string specified");
            }
        }

        #endregion

        /// <summary>
        /// Process the Provisioning XML
        ///     Reconsititue the Token Parser to handle newly provisioned resources
        /// </summary>
        private void Process()
        {
            var web = m_clientContext.Web;
            m_clientContext.Load(web, wctx => wctx.RootFolder.ServerRelativeUrl);
            m_clientContext.ExecuteQueryRetry();


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


            m_scope.LogInfo("ProcessRequest. Template: {0}.  Publishing Pages", m_template.Id);
            var pages = m_xmlParser.GetPublishingPagesListFromConfiguration();
            foreach (var page in pages)
            {
                try
                {
                    if (page.IsPublishingPage)
                    {
                        ProvisioningHelper.AddPublishingPage(m_clientContext, web, page);
                    }
                    else
                    {
                        List listinstance = web.GetListByUrl(page.ListUrl, pctx => pctx.Id, pctx => pctx.RootFolder, pctx => pctx.RootFolder.ServerRelativeUrl);
                        ProvisioningHelper.AddWebpartsToPage(m_clientContext, web, listinstance, page, m_tokenParser, m_scope);
                    }
                }
                catch (Exception ex)
                {
                    m_scope.LogError(ex, "Error adding publishing page: {0}. Exception: {1}", page.FileName, ex.ToString());
                }
            }


            m_scope.LogInfo("ProcessRequest. Template: {0}.  Navigation nodes", m_template.Id);
            var navigation = m_xmlParser.GetNavigationFromConfiguration(m_tokenParser);
            if (navigation != null)
            {

                if (!WebSupportsProvisionNavigation(web, navigation))
                {
                    m_scope.LogDebug("Navigation Context web is not publishing");
                    return;
                }

                // Retrieve the current web navigation settings
                var navigationSettings = new WebNavigationSettings(web.Context, web);
                web.Context.Load(navigationSettings,
                    ns => ns.AddNewPagesToNavigation,
                    ns => ns.CreateFriendlyUrlsForNewPages,
                    ns => ns.CurrentNavigation,
                    ns => ns.GlobalNavigation);
                web.Context.ExecuteQueryRetry();

                if (navigation.GlobalNavigation != null
                    && navigation.GlobalNavigation.StructuralNavigation != null)
                {
                    if (navigationSettings.GlobalNavigation.Source != StandardNavigationSource.PortalProvider)
                    {
                        navigationSettings.GlobalNavigation.Source = StandardNavigationSource.PortalProvider;
                    }

                    ProvisionGlobalStructuralNavigation(web, navigationSettings,
                        navigation.GlobalNavigation.StructuralNavigation, m_tokenParser, m_applyingInformation.ClearNavigation);


                    navigationSettings.Update(Microsoft.SharePoint.Client.Taxonomy.TaxonomySession.GetTaxonomySession(web.Context));
                    web.Context.ExecuteQueryRetry();
                }

                if (navigation.CurrentNavigation != null)
                {
                    switch (navigation.CurrentNavigation.NavigationType)
                    {

                        case CurrentNavigationType.Inherit:
                            navigationSettings.CurrentNavigation.Source = StandardNavigationSource.InheritFromParentWeb;
                            break;
                        case CurrentNavigationType.Managed:
                            if (navigation.CurrentNavigation.ManagedNavigation == null)
                            {
                                throw new ApplicationException("Navigation: Failure with managed navigation not defined");
                            }
                            break;
                        case CurrentNavigationType.StructuralLocal:
                            // Render only the local site
                            if (navigation.CurrentNavigation.StructuralNavigation == null)
                            {
                                throw new ApplicationException("Navigation: Failure with structural local navigation not defined");
                            }
                            web.SetPropertyBagValue(ProvisioningConstants.NavigationShowSiblings, "false");
                            navigationSettings.CurrentNavigation.Source = StandardNavigationSource.PortalProvider;
                            ProvisionCurrentStructuralNavigation(web, navigationSettings,
                                navigation.CurrentNavigation.StructuralNavigation, m_tokenParser, m_applyingInformation.ClearNavigation);
                            break;
                        case CurrentNavigationType.Structural:
                        default:
                            // Ensure the Top site navigation is rendered with local site nested
                            if (navigation.CurrentNavigation.StructuralNavigation == null)
                            {
                                throw new ApplicationException("Navigation: Failure with structural navigation not defined");
                            }
                            web.SetPropertyBagValue(ProvisioningConstants.NavigationShowSiblings, "true");
                            navigationSettings.CurrentNavigation.Source = StandardNavigationSource.PortalProvider;
                            ProvisionCurrentStructuralNavigation(web, navigationSettings,
                                navigation.CurrentNavigation.StructuralNavigation, m_tokenParser, m_applyingInformation.ClearNavigation);
                            break;
                    }

                    navigationSettings.Update(Microsoft.SharePoint.Client.Taxonomy.TaxonomySession.GetTaxonomySession(web.Context));
                    web.Context.ExecuteQueryRetry();
                }
            }
        }

        private bool WebSupportsProvisionNavigation(Web web, Model.Navigation template)
        {
            bool isNavSupported = true;
            // The Navigation handler for managed metedata only works for sites with Publishing Features enabled
            if (!web.IsPublishingWeb())
            {
                // NOTE: Here there could be a very edge case for a site where publishing features were enabled, 
                // configured managed navigation, and then disabled, keeping one navigation managed and another
                // one structural. Just as a reminder ...
                if (template.GlobalNavigation != null
                    && template.GlobalNavigation.NavigationType == GlobalNavigationType.Managed)
                {
                    isNavSupported = false;
                }
                if (template.CurrentNavigation != null
                    && template.CurrentNavigation.NavigationType == CurrentNavigationType.Managed)
                {
                    isNavSupported = false;
                }
            }
            return isNavSupported;
        }

        private void ProvisionGlobalStructuralNavigation(Web web, WebNavigationSettings navigationSettings, StructuralNavigation structuralNavigation, TokenParser parser, bool clearNavigation)
        {
            ProvisionStructuralNavigation(web, navigationSettings, structuralNavigation, parser, false, clearNavigation);
        }

        private void ProvisionCurrentStructuralNavigation(Web web, WebNavigationSettings navigationSettings, StructuralNavigation structuralNavigation, TokenParser parser, bool clearNavigation)
        {
            ProvisionStructuralNavigation(web, navigationSettings, structuralNavigation, parser, true, clearNavigation);
        }

        private void ProvisionStructuralNavigation(Web web, WebNavigationSettings navigationSettings, StructuralNavigation structuralNavigation, TokenParser parser, bool currentNavigation, bool clearNavigation)
        {
            // Determine the target structural navigation
            var navigationType = currentNavigation ?
                Enums.NavigationType.QuickLaunch :
                Enums.NavigationType.TopNavigationBar;
            if (structuralNavigation != null)
            {
                // Remove existing nodes, if requested
                if (structuralNavigation.RemoveExistingNodes)
                {
                    web.DeleteAllNavigationNodes(navigationType);
                }

                // retreive the nodes [NOTE: if we removed the existing nodes, this should be an empty collection]
                var existingNodes = GetStructuralNavigation(web, navigationSettings, currentNavigation);
                if (existingNodes.NavigationNodes != null && existingNodes.NavigationNodes.Any(nn => nn.Title == "Recent"))
                {
                    web.DeleteNavigationNode("Recent", string.Empty, navigationType);
                }

                // Provision root level nodes, and children recursively
                if (structuralNavigation.NavigationNodes.Any())
                {
                    ProvisionStructuralNavigationNodes(
                        web,
                        parser,
                        navigationType,
                        structuralNavigation.NavigationNodes,
                        existingNodes
                    );
                }
            }
        }

        private void ProvisionStructuralNavigationNodes(Web web, TokenParser parser, Enums.NavigationType navigationType, Model.NavigationNodeCollection nodes, StructuralNavigation existingNodes, string parentNodeTitle = null)
        {
            foreach (var node in nodes)
            {
                var nodeTitle = parser.ParseString(node.Title);

                if (!existingNodes.NavigationNodes.Any(nn => nn.Title.Equals(nodeTitle, StringComparison.CurrentCultureIgnoreCase) || nn.Url.Equals(node.Url, StringComparison.CurrentCultureIgnoreCase)))
                {
                    m_scope.LogInfo("Adding navigation node {0} with element {1}", nodeTitle, node.Url);

                    var navNode = web.AddNavigationNode(nodeTitle,
                        new Uri(parser.ParseString(node.Url), UriKind.RelativeOrAbsolute),
                        parser.ParseString(parentNodeTitle),
                        navigationType,
                        node.IsExternal);
                }

                if (node.NavigationNodes != null && node.NavigationNodes.Any())
                {
                    ProvisionStructuralNavigationNodes(
                        web,
                        parser,
                        navigationType,
                        node.NavigationNodes,
                        existingNodes,
                        nodeTitle);
                }
            }
        }

        private StructuralNavigation GetStructuralNavigation(Web web, WebNavigationSettings navigationSettings, Boolean currentNavigation)
        {
            // By default avoid removing existing nodes
            var result = new StructuralNavigation { RemoveExistingNodes = false };
            Microsoft.SharePoint.Client.NavigationNodeCollection sourceNodes = currentNavigation ?
                web.Navigation.QuickLaunch : web.Navigation.TopNavigationBar;

            web.Context.Load(web, w => w.ServerRelativeUrl);
            web.Context.Load(sourceNodes);
            web.Context.ExecuteQueryRetry();

            if (sourceNodes != null && sourceNodes.Any())
            {
                result.NavigationNodes.AddRange(from n in sourceNodes.AsEnumerable()
                                                select n.ToDomainModelNavigationNode(web));
            }
            return (result);
        }

    }

    internal static class NavigationNodeExtensions
    {
        internal static Model.NavigationNode ToDomainModelNavigationNode(this Microsoft.SharePoint.Client.NavigationNode node, Web web)
        {

            var result = new Model.NavigationNode
            {
                Title = node.Title,
                IsExternal = node.IsExternal,
                Url = node.Url
            };

            node.Context.Load(node.Children);
            node.Context.ExecuteQueryRetry();

            result.NavigationNodes.AddRange(from n in node.Children.AsEnumerable()
                                            select n.ToDomainModelNavigationNode(web));

            return (result);
        }
    }
}
