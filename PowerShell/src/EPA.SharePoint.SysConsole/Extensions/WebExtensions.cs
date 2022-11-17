using EPA.SharePoint.SysConsole.Models;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.WebParts;
using OfficeDevPnP.Core.Entities;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Xml;

namespace EPA.SharePoint.SysConsole.Extensions
{
    public static class WebExtensions
    {
        public static Web GetAssociatedWeb(this SecurableObject securable)
        {
            if (securable is Web)
            {
                return (Web)securable;
            }

            if (securable is List list)
            {
                var web = list.ParentWeb;
                securable.Context.Load(web);
                securable.Context.ExecuteQueryRetry();

                return web;
            }

            if (securable is ListItem listItem)
            {
                var web = listItem.ParentList.ParentWeb;
                securable.Context.Load(web);
                securable.Context.ExecuteQueryRetry();

                return web;
            }

            throw new Exception("Only Web, List, ListItem supported as SecurableObjects");
        }

        /// <summary>
        /// Retreives the Web by GUID, if <paramref name="expressions"/> are specified those properties will be loaded into the context
        /// </summary>
        /// <param name="currentWeb"></param>
        /// <param name="guid"></param>
        /// <param name="expressions"></param>
        /// <returns></returns>
        public static Web GetWebById(this Web currentWeb, Guid guid, Expression<Func<Web, object>>[] expressions = null)
        {
            var clientContext = currentWeb.Context as ClientContext;
            Site site = clientContext.Site;
            Web web = site.OpenWebById(guid);

            if (expressions != null)
            {
                web.EnsureProperties(expressions);
            }
            else
            {
                web.EnsureProperties(w => w.Url, w => w.Title, w => w.Id, w => w.ServerRelativeUrl);
            }
            return web;
        }

        /// <summary>
        /// Retreives the Web by relative URL, if <paramref name="expressions"/> are specified those properties will be loaded into the context
        /// </summary>
        /// <param name="currentWeb"></param>
        /// <param name="url"></param>
        /// <param name="expressions"></param>
        /// <returns></returns>
        public static Web GetWebByUrl(this Web currentWeb, string url, Expression<Func<Web, object>>[] expressions = null)
        {
            var clientContext = currentWeb.Context as ClientContext;
            Site site = clientContext.Site;
            Web web = site.OpenWeb(url);

            if (expressions != null)
            {
                web.EnsureProperties(expressions);
            }
            else
            {
                web.EnsureProperties(w => w.Url, w => w.Title, w => w.Id, w => w.ServerRelativeUrl);
            }
            return web;
        }

        /// <summary>
        /// Recursively queries the Web Subwebs and recalls method until end of the tree
        /// </summary>
        /// <param name="currentWeb"></param>
        /// <param name="expressions"></param>
        /// <returns></returns>
        public static IEnumerable<Web> GetAllWebsRecursive(this Web currentWeb, Expression<Func<Web, object>>[] expressions = null)
        {
            List<Expression<Func<Web, object>>> exps = new List<Expression<Func<Web, object>>>();
            if (expressions != null) exps.AddRange(expressions);

            exps.Add(item => item.Webs);

            currentWeb.Context.Load(currentWeb, exps.ToArray());
            currentWeb.Context.ExecuteQueryRetry();

            foreach (var subWeb in currentWeb.Webs)
            {
                foreach (var subSubWeb in subWeb.GetAllWebsRecursive(expressions))
                {
                    yield return subSubWeb;
                }

                yield return subWeb;
            }
        }

        /// <summary>
        /// Will scan the web site groups for groups to provision and retrieve
        /// </summary>
        /// <param name="hostWeb">The host web to which the groups will be created</param>
        /// <param name="groupDef">The collection of groups to retrieve and/or provision</param>
        /// <returns></returns>
        public static Microsoft.SharePoint.Client.Group GetOrCreateSiteGroups(this Web hostWeb, SPGroupDefinitionModel groupDef)
        {
            hostWeb.EnsureProperties(hw => hw.CurrentUser);
            var context = hostWeb.Context;

            // Create Group
            var groupCreationInfo = new GroupCreationInformation
            {
                Title = groupDef.Title,
                Description = groupDef.Description
            };

            var oGroup = hostWeb.SiteGroups.Add(groupCreationInfo);
            context.Load(oGroup);

            oGroup.Owner = hostWeb.CurrentUser;
            oGroup.OnlyAllowMembersViewMembership = groupDef.OnlyAllowMembersViewMembership;
            oGroup.AllowMembersEditMembership = groupDef.AllowMembersEditMembership;
            oGroup.AllowRequestToJoinLeave = groupDef.AllowRequestToJoinLeave;
            oGroup.AutoAcceptRequestToJoinLeave = groupDef.AutoAcceptRequestToJoinLeave;
            oGroup.Update();

            context.Load(oGroup, g => g.Id, g => g.Title);
            context.ExecuteQueryRetry();


            return oGroup;
        }

        /// <summary>
        /// Add web part to a wiki style page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="folder">System name of the wiki page library - typically sitepages</param>
        /// <param name="webPart">Information about the web part to insert</param>
        /// <param name="page">Page to add the web part on</param>
        /// <param name="row">Row of the wiki table that should hold the inserted web part</param>
        /// <param name="col">Column of the wiki table that should hold the inserted web part</param>
        /// <param name="addSpace">Does a blank line need to be added after the web part (to space web parts)</param>
        /// <exception cref="System.ArgumentException">Thrown when folder or page is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when folder, webPart or page is null</exception>
        public static void AddWebPartToWikiPage(this Web web, string folder, WebPartEntity webPart, string page, int row, int col, bool addSpace, bool something)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw (folder == null)
                  ? new ArgumentNullException("folder")
                  : new ArgumentException("Empty string for folder", "folder");
            }

            if (webPart == null)
            {
                throw new ArgumentNullException("webPart");
            }

            if (string.IsNullOrEmpty(page))
            {
                throw (page == null)
                  ? new ArgumentNullException("page")
                  : new ArgumentException("Empty string for page", "page");
            }

            if (!web.IsObjectPropertyInstantiated("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }

            var webServerRelativeUrl = UrlUtility.EnsureTrailingSlash(web.ServerRelativeUrl);
            var serverRelativeUrl = UrlUtility.Combine(folder, page);
            AddWebPartToWikiPage(web, webServerRelativeUrl + serverRelativeUrl, webPart, row, col, addSpace);
        }

        /// <summary>
        /// Add web part to a wiki style page
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="serverRelativePageUrl">Server relative url of the page to add the webpart to</param>
        /// <param name="webPart">Information about the web part to insert</param>
        /// <param name="row">Row of the wiki table that should hold the inserted web part</param>
        /// <param name="col">Column of the wiki table that should hold the inserted web part</param>
        /// <param name="addSpace">Does a blank line need to be added after the web part (to space web parts)</param>
        /// <exception cref="System.ArgumentException">Thrown when serverRelativePageUrl is a zero-length string or contains only white space</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when serverRelativePageUrl or webPart is null</exception>
        public static void AddWebPartToWikiPage(this Web web, string serverRelativePageUrl, WebPartEntity webPart, int row, int col, bool addSpace)
        {
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                throw (serverRelativePageUrl == null)
                  ? new ArgumentNullException("serverRelativePageUrl")
                  : new ArgumentException("Empty parameter", "serverRelativePageUrl");
            }

            if (webPart == null)
            {
                throw new ArgumentNullException("webPart");
            }

            File webPartPage = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

            if (webPartPage == null)
            {
                return;
            }

            web.Context.Load(webPartPage, wp => wp.ListItemAllFields);
            web.Context.ExecuteQueryRetry();

            string wikiField = (string)webPartPage.ListItemAllFields["WikiField"];

            LimitedWebPartManager limitedWebPartManager = webPartPage.GetLimitedWebPartManager(PersonalizationScope.Shared);
            WebPartDefinition oWebPartDefinition = limitedWebPartManager.ImportWebPart(webPart.WebPartXml);
            WebPartDefinition wpdNew = limitedWebPartManager.AddWebPart(oWebPartDefinition.WebPart, "wpz", 0);
            web.Context.Load(wpdNew);
            web.Context.ExecuteQueryRetry();

            // Close all BR tags
            Regex brRegex = new Regex("<br>", RegexOptions.IgnoreCase);

            wikiField = brRegex.Replace(wikiField, "<br/>");

            XmlDocument xd = new XmlDocument
            {
                PreserveWhitespace = true
            };
            xd.LoadXml(wikiField);

            // Sometimes the wikifield content seems to be surrounded by an additional div? 
            XmlElement layoutsTable = xd.SelectSingleNode("div/div/table") as XmlElement;
            if (layoutsTable == null)
            {
                layoutsTable = xd.SelectSingleNode("div/table") as XmlElement;
            }

            XmlElement layoutsZoneInner = layoutsTable.SelectSingleNode(string.Format("tbody/tr[{0}]/td[{1}]/div/div", row, col)) as XmlElement;
            // - space element
            XmlElement space = xd.CreateElement("p");
            XmlText text = xd.CreateTextNode(" ");
            space.AppendChild(text);

            // - wpBoxDiv
            XmlElement wpBoxDiv = xd.CreateElement("div");
            layoutsZoneInner.AppendChild(wpBoxDiv);

            if (addSpace)
            {
                layoutsZoneInner.AppendChild(space);
            }

            XmlAttribute attribute = xd.CreateAttribute("class");
            wpBoxDiv.Attributes.Append(attribute);
            attribute.Value = "ms-rtestate-read ms-rte-wpbox";
            attribute = xd.CreateAttribute("contentEditable");
            wpBoxDiv.Attributes.Append(attribute);
            attribute.Value = "false";
            // - div1
            XmlElement div1 = xd.CreateElement("div");
            wpBoxDiv.AppendChild(div1);
            div1.IsEmpty = false;
            attribute = xd.CreateAttribute("class");
            div1.Attributes.Append(attribute);
            attribute.Value = "ms-rtestate-read " + wpdNew.Id.ToString("D");
            attribute = xd.CreateAttribute("id");
            div1.Attributes.Append(attribute);
            attribute.Value = "div_" + wpdNew.Id.ToString("D");
            // - div2
            XmlElement div2 = xd.CreateElement("div");
            wpBoxDiv.AppendChild(div2);
            div2.IsEmpty = false;
            attribute = xd.CreateAttribute("style");
            div2.Attributes.Append(attribute);
            attribute.Value = "display:none";
            attribute = xd.CreateAttribute("id");
            div2.Attributes.Append(attribute);
            attribute.Value = "vid_" + wpdNew.Id.ToString("D");

            ListItem listItem = webPartPage.ListItemAllFields;
            listItem["WikiField"] = xd.OuterXml;
            listItem.Update();
            web.Context.ExecuteQueryRetry();

        }

        /// <summary>
        /// Adds or Updates an existing Custom Action [ScriptSrc] into the [Web] Custom Actions
        /// </summary>
        /// <param name="web"></param>
        /// <param name="customactionname"></param>
        /// <param name="customactionurl"></param>
        /// <param name="sequence"></param>
        public static void AddOrUpdateCustomActionLink(this Web web, string customactionname, string customactionurl, int sequence)
        {
            var sitecustomActions = web.GetCustomActions();
            UserCustomAction cssAction = null;
            if (web.CustomActionExists(customactionname))
            {
                cssAction = sitecustomActions.FirstOrDefault(fod => fod.Name == customactionname);
            }
            else
            {
                // Build a custom action to write a link to our new CSS file
                cssAction = web.UserCustomActions.Add();
                cssAction.Name = customactionname;
                cssAction.Location = "ScriptLink";
            }

            cssAction.Sequence = sequence;
            cssAction.ScriptSrc = customactionurl;
            cssAction.Update();
            web.Context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Adds or Updates an existing Custom Action [ScriptBlock] into the [Web] Custom Actions
        /// </summary>
        /// <param name="web"></param>
        /// <param name="customactionname"></param>
        /// <param name="customActionBlock"></param>
        /// <param name="sequence"></param>
        public static void AddOrUpdateCustomActionLinkBlock(this Web web, string customactionname, string customActionBlock, int sequence)
        {
            var sitecustomActions = web.GetCustomActions();
            UserCustomAction cssAction = null;
            if (web.CustomActionExists(customactionname))
            {
                cssAction = sitecustomActions.FirstOrDefault(fod => fod.Name == customactionname);
            }
            else
            {
                // Build a custom action to write a link to our new CSS file
                cssAction = web.UserCustomActions.Add();
                cssAction.Name = customactionname;
                cssAction.Location = "ScriptLink";
            }

            cssAction.Sequence = sequence;
            cssAction.ScriptBlock = customActionBlock;
            cssAction.Update();
            web.Context.ExecuteQueryRetry();
        }

        /// <summary>
        /// Will remove the custom action if one exists
        /// </summary>
        /// <param name="web"></param>
        /// <param name="customactionname"></param>
        public static bool RemoveCustomActionLink(this Web web, string customactionname)
        {
            if (web.CustomActionExists(customactionname))
            {
                var cssAction = web.GetCustomActions().FirstOrDefault(fod => fod.Name == customactionname || fod.Title == customactionname);
                web.DeleteCustomAction(cssAction.Id);
                return true;
            }
            return false;
        }
    }
}
