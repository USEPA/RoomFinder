using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Diagnostics;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Linq;
using SPPublishing = Microsoft.SharePoint.Client.Publishing;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public static class ProvisioningHelper
    {
        private static void RemovePublishingPage(SPPublishing.PublishingPage publishingPage, PublishingPage page, ClientContext ctx, Web web)
        {
            if (publishingPage != null && publishingPage.ServerObjectIsNull.Value == false)
            {
                if (!web.IsPropertyAvailable("RootFolder"))
                {
                    web.Context.Load(web.RootFolder);
                    web.Context.ExecuteQueryRetry();
                }

                if (page.Overwrite)
                {
                    if (page.WelcomePage && web.RootFolder.WelcomePage.Contains(page.FileName + ".aspx"))
                    {
                        //set the welcome page to a Temp page to allow remove the page
                        web.RootFolder.WelcomePage = "home.aspx";
                        web.RootFolder.Update();
                        web.Update();

                        ctx.Load(publishingPage);
                        ctx.ExecuteQuery();
                    }

                    publishingPage.ListItem.DeleteObject();
                    ctx.ExecuteQuery();
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Add the defined <see cref="PublishingPage"/> to the SPWeb
        /// </summary>
        /// <param name="ctx">The context containing the Web</param>
        /// <param name="web">The web to which publishing pages will be written</param>
        /// <param name="page">The page definition</param>
        public static void AddPublishingPage(ClientContext ctx, Web web, PublishingPage page)
        {
            // construct the server relative path to the page
            var targetFileName = (!string.IsNullOrEmpty(page.TargetFileName) ? page.TargetFileName : page.FileName);

            SPPublishing.PublishingPage publishingPage = web.GetPublishingPage(targetFileName);

            RemovePublishingPage(publishingPage, page, ctx, web);

            web.AddPublishingPage(page.FileName, page.Layout, page.Title, false); //DO NOT Publish here or it will fail if library doesn't enable Minor versions (PnP bug)

            publishingPage = web.GetPublishingPage(targetFileName);

            Microsoft.SharePoint.Client.File pageFile = publishingPage.ListItem.File;
            pageFile.CheckOut();

            if (page.Properties != null && page.Properties.Count > 0)
            {
                ctx.Load(pageFile, p => p.Name, p => p.CheckOutType); //need these values in SetFileProperties
                ctx.ExecuteQuery();
                pageFile.SetFileProperties(page.Properties, false);
            }

            if (page.WebParts != null && page.WebParts.Count > 0)
            {
                Microsoft.SharePoint.Client.WebParts.LimitedWebPartManager mgr = pageFile.GetLimitedWebPartManager(Microsoft.SharePoint.Client.WebParts.PersonalizationScope.Shared);

                ctx.Load(mgr);
                ctx.ExecuteQuery();

                AddWebpartsToPublishingPage(page, ctx, mgr);
            }

            List pagesLibrary = publishingPage.ListItem.ParentList;
            ctx.Load(pagesLibrary);
            ctx.ExecuteQueryRetry();

            ListItem pageItem = publishingPage.ListItem;
            web.Context.Load(pageItem, p => p.File.CheckOutType);
            web.Context.ExecuteQueryRetry();

            if (pageItem.File.CheckOutType != CheckOutType.None)
            {
                pageItem.File.CheckIn(String.Empty, CheckinType.MajorCheckIn);
            }

            if (page.Publish && pagesLibrary.EnableMinorVersions)
            {
                pageItem.File.Publish(String.Empty);
                if (pagesLibrary.EnableModeration)
                {
                    pageItem.File.Approve(String.Empty);
                }
            }


            if (page.WelcomePage)
            {
                SetWelcomePage(web, pageFile);
            }

            ctx.ExecuteQuery();
        }


        public static void SetWelcomePage(Web web, Microsoft.SharePoint.Client.File pageFile)
        {
            if (!web.IsPropertyAvailable("RootFolder"))
            {
                web.Context.Load(web.RootFolder);
                web.Context.ExecuteQueryRetry();
            }

            if (!pageFile.IsPropertyAvailable("ServerRelativeUrl"))
            {
                web.Context.Load(pageFile, p => p.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }

            var rootFolderRelativeUrl = pageFile.ServerRelativeUrl.Substring(web.RootFolder.ServerRelativeUrl.Length);

            web.SetHomePage(rootFolderRelativeUrl);
        }

        /// <summary>
        /// Add the collection of webparts as defined in the <see cref="PublishingPage"/> to the specified Web Page
        /// </summary>
        /// <param name="context"></param>
        /// <param name="web"></param>
        /// <param name="page"></param>
        /// <param name="parser"></param>
        /// <param name="source"></param>
        public static void AddWebpartsToPage(this ClientContext context, Web web, PublishingPage page, TokenParser parser, PnPMonitoredScope source)
        {
            if (!web.IsObjectPropertyInstantiated(lctx => lctx.ServerRelativeUrl))
            {
                web.EnsureProperties(lctx => lctx.ServerRelativeUrl);
            }

            // construct the server relative path to the page
            var targetFileName = (!string.IsNullOrEmpty(page.TargetFileName) ? page.TargetFileName : page.FileName);
            var serverRelativePageUrl = UrlUtility.Combine(web.ServerRelativeUrl, targetFileName);

            var filedestination = web.GetFileByServerRelativeUrl(serverRelativePageUrl);
            web.Context.Load(filedestination);
            web.Context.ExecuteQueryRetry();

            AddWebpartsToFile(filedestination, page, parser, source);

            if (page.WelcomePage)
            {
                SetWelcomePage(web, filedestination);
            }
        }

        /// <summary>
        /// Add the collection of webparts as defined in the <see cref="PublishingPage"/> to the specified List Page
        /// </summary>
        /// <param name="context"></param>
        /// <param name="web"></param>
        /// <param name="list"></param>
        /// <param name="page"></param>
        /// <param name="parser"></param>
        /// <param name="source"></param>
        public static void AddWebpartsToPage(this ClientContext context, Web web, List list, PublishingPage page, TokenParser parser, PnPMonitoredScope source)
        {
            if (!list.IsObjectPropertyInstantiated(lctx => lctx.RootFolder))
            {
                list.EnsureProperties(lctx => lctx.RootFolder, lctx => lctx.RootFolder.ServerRelativeUrl);
            }

            var targetFileName = (!string.IsNullOrEmpty(page.TargetFileName) ? page.TargetFileName : page.FileName);
            var serverRelativePageUrl = UrlUtility.Combine(list.RootFolder.ServerRelativeUrl, targetFileName);

            var filedestination = list.RootFolder.GetFile(targetFileName);
            web.Context.Load(filedestination);
            web.Context.ExecuteQueryRetry();

            AddWebpartsToFile(filedestination, page, parser, source);

            if (page.WelcomePage)
            {
                SetWelcomePage(web, filedestination);
            }
        }

        /// <summary>
        /// Import the webpart into the detination file
        /// </summary>
        /// <param name="filedestination"></param>
        /// <param name="page"></param>
        /// <param name="parser"></param>
        /// <param name="source"></param>
        private static void AddWebpartsToFile(File filedestination, PublishingPage page, TokenParser parser, PnPMonitoredScope source, int attempt = 0, int maxRetries = 1)
        {
            if (!filedestination.IsObjectPropertyInstantiated(fctx => fctx.ServerRelativeUrl))
            {
                filedestination.EnsureProperties(fctx => fctx.ServerRelativeUrl);
            }

            var serverRelativePageUrl = filedestination.ServerRelativeUrl;


            var limitedWebPartManager = filedestination.GetLimitedWebPartManager(Microsoft.SharePoint.Client.WebParts.PersonalizationScope.Shared);
            filedestination.Context.Load(limitedWebPartManager.WebParts, wpx => wpx.Include(wp => wp.WebPart.Title));
            filedestination.Context.ExecuteQueryRetry();

            // Enumerate collection and provision them
            foreach (var pageWebPart in page.WebParts)
            {
                var webPartName = string.Empty;
                var webPartXml = string.Empty;

                try
                {
                    if (limitedWebPartManager.WebParts.Any(wp => wp.WebPart.Title == pageWebPart.Title))
                    {
                        source.LogInfo("Webpart {0} already exists on the page", pageWebPart.Title);
                        continue;
                    }

                    webPartXml = parser.ParseXmlString(pageWebPart.Contents);
                    webPartName = string.Format("Zone {0} Order {1} Title {2}", pageWebPart.Zone, pageWebPart.Order, pageWebPart.Title);


                    var oWebPartDefinition = limitedWebPartManager.ImportWebPart(webPartXml);
                    var oWebPart = oWebPartDefinition.WebPart;

                    var wpDefinition = limitedWebPartManager.AddWebPart(oWebPart, pageWebPart.Zone, (int)pageWebPart.Order);
                    filedestination.Context.Load(oWebPart);
                    filedestination.Context.Load(wpDefinition, wpdctx => wpdctx.Id);
                    filedestination.Context.ExecuteQueryRetry();
                    source.LogInfo("Added webpart {0} to {1}", wpDefinition.Id, serverRelativePageUrl);
                }
                catch (Exception ex)
                {
                    if (ex.HResult == -2146233088
                        && attempt < maxRetries)
                    {
                        source.LogDebug("Adding Webpart => {0} Failed => {1} Attempt iteration {2}", webPartName, ex.Message, attempt);
                        attempt++;
                        AddWebpartsToFile(filedestination, page, parser, source, attempt, maxRetries);
                    }

                    source.LogDebug(ex, "Adding Webparts to page {0}", serverRelativePageUrl);
                }
            }
        }

        private static void AddWebpartsToPublishingPage(PublishingPage page, ClientContext ctx, Microsoft.SharePoint.Client.WebParts.LimitedWebPartManager mgr)
        {
            foreach (var wp in page.WebParts)
            {
                var wpContentsTokenResolved = wp.Contents;
                var webPart = mgr.ImportWebPart(wpContentsTokenResolved).WebPart;
                var definition = mgr.AddWebPart(webPart, wp.Zone, (int)wp.Order);
                var webPartProperties = definition.WebPart.Properties;
                ctx.Load(definition.WebPart);
                ctx.Load(webPartProperties);
                ctx.ExecuteQuery();

                if (wp.IsListViewWebPart)
                {
                    AddListViewWebpart(ctx, wp, definition, webPartProperties);
                }
            }
        }

        private static void AddListViewWebpart(ClientContext ctx, PublishingPageWebPart wp, Microsoft.SharePoint.Client.WebParts.WebPartDefinition definition, PropertyValues webPartProperties)
        {
            string defaultViewDisplayName = wp.DefaultViewDisplayName;

            if (!String.IsNullOrEmpty(defaultViewDisplayName))
            {
                string listUrl = webPartProperties.FieldValues["ListUrl"].ToString();

                ctx.Load(definition, d => d.Id); // Id of the hidden view which gets automatically created
                ctx.ExecuteQuery();

                Guid viewId = definition.Id;
                List list = ctx.Web.GetListByUrl(listUrl);

                Microsoft.SharePoint.Client.View viewCreatedFromWebpart = list.Views.GetById(viewId);
                ctx.Load(viewCreatedFromWebpart);

                Microsoft.SharePoint.Client.View viewCreatedFromList = list.Views.GetByTitle(defaultViewDisplayName);
                ctx.Load(
                    viewCreatedFromList,
                    v => v.ViewFields,
                    v => v.ListViewXml,
                    v => v.ViewQuery,
                    v => v.ViewData,
                    v => v.ViewJoins,
                    v => v.ViewProjectedFields);

                ctx.ExecuteQuery();

                //need to copy the same View definition to the new View added by the Webpart manager
                viewCreatedFromWebpart.ViewQuery = viewCreatedFromList.ViewQuery;
                viewCreatedFromWebpart.ViewData = viewCreatedFromList.ViewData;
                viewCreatedFromWebpart.ViewJoins = viewCreatedFromList.ViewJoins;
                viewCreatedFromWebpart.ViewProjectedFields = viewCreatedFromList.ViewProjectedFields;
                viewCreatedFromWebpart.ViewFields.RemoveAll();

                foreach (var field in viewCreatedFromList.ViewFields)
                {
                    viewCreatedFromWebpart.ViewFields.Add(field);
                }

                //need to set the JSLink to the new View added by the Webpart manager.
                //This is because there's no way to change the BaseViewID property of the new View,
                //and we needed to do that because the custom JSLink was bound to a specific BaseViewID (overrideCtx.BaseViewID = 3;)
                //The work around to this is to add the JSLink to the specific new View created when you add the xsltViewWebpart to the page
                //and remove the "overrideCtx.BaseViewID = 3;" from the JSLink file
                //that way, the JSLink will be executed only for this View, that is only used in the xsltViewWebpart,
                //so the effect is the same that bind the JSLink to the BaseViewID
                if (webPartProperties.FieldValues.ContainsKey("JSLink") && webPartProperties.FieldValues["JSLink"] != null)
                {
                    viewCreatedFromWebpart.JSLink = webPartProperties.FieldValues["JSLink"].ToString();
                }

                viewCreatedFromWebpart.Update();

                ctx.ExecuteQuery();
            }
        }
    }
}
