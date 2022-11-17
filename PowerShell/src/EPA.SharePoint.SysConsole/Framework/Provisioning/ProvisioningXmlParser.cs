using OfficeDevPnP.Core.Framework.Provisioning.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    /// <summary>
    /// Enables the parsing an of XML template file
    /// </summary>
    public class ProvisioningXmlParser
    {
        public ProvisioningXmlParser() { }

        public ProvisioningXmlParser(string xmlContent, ProvisioningTemplate template) : this()
        {
            m_configurationXml = xmlContent;
            m_template = template;
        }


        internal string m_configurationXml { get; private set; }

        internal ProvisioningTemplate m_template { get; private set; }


        private string GetElement(XElement xElement, string defaultValue = "")
        {
            if (xElement == null)
            {
                return defaultValue;
            }

            return xElement.Value;
        }

        private string GetAttribute(XAttribute xAttribute, string defaultValue = "")
        {
            if (xAttribute == null)
            {
                return defaultValue;
            }

            return xAttribute.Value;
        }

        private T TryParseInt<T>(string val)
        {
            T parsedInt = default(T);
            try
            {
                parsedInt = (T)Enum.ToObject(typeof(T), val);
            }
            catch
            {
                try
                {
                    parsedInt = val.ToEnum<T>();
                }
                catch { }
            };
            return parsedInt;
        }

        /// <summary>
        /// Parses the XML content into a collection of list intances
        /// </summary>
        /// <returns></returns>
        public Navigation GetNavigationFromConfiguration(OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenParser parser)
        {
            Navigation navigation = null;

            var ns = ProvisioningConstants.configns;

            var tokenedConfigurationXml = parser.ParseXmlString(m_configurationXml);

            XDocument doc = XDocument.Parse(tokenedConfigurationXml);
            var naviNode = doc.Root.Element(ns + "NavigationNodes");
            if (naviNode != null)
            {
                GlobalNavigation globalNavigation = null;
                var naviGlobalNodes = naviNode.Element(ns + "GlobalNodes");
                if (naviGlobalNodes != null)
                {
                    var removeExisting = GetAttribute(naviGlobalNodes.Attribute("RemoveExistingNodes"));

                    var structuralNavi = new StructuralNavigation()
                    {
                        RemoveExistingNodes = removeExisting.ToBoolean(false)
                    };


                    foreach (var p in naviGlobalNodes.Descendants(ns + "GlobalNode"))
                    {
                        var isExternal = GetAttribute(p.Attribute("IsExternal")).ToBoolean(false);

                        var gnode = new NavigationNode()
                        {
                            Url = GetAttribute(p.Attribute("Url")),
                            Title = GetAttribute(p.Attribute("Title"))
                        };
                        if (isExternal)
                        {
                            gnode.IsExternal = isExternal;
                        }

                        structuralNavi.NavigationNodes.Add(gnode);
                    }

                    globalNavigation = new GlobalNavigation(GlobalNavigationType.Structural, structuralNavi);
                }


                CurrentNavigation localNavigation = null;
                var naviLocalNodes = naviNode.Element(ns + "LocalNodes");
                if (naviLocalNodes != null)
                {
                    var removeExisting = GetAttribute(naviLocalNodes.Attribute("RemoveExistingNodes"));
                    var currentNavigationType = GetAttribute(naviLocalNodes.Attribute("CurrentNavigationType"));

                    var structuralNavi = new StructuralNavigation()
                    {
                        RemoveExistingNodes = removeExisting.ToBoolean(false)
                    };


                    foreach (var p in naviLocalNodes.Descendants(ns + "LocalNode"))
                    {
                        var isExternal = GetAttribute(p.Attribute("IsExternal")).ToBoolean(false);

                        var lnode = new NavigationNode()
                        {
                            Url = GetAttribute(p.Attribute("Url")),
                            Title = GetAttribute(p.Attribute("Title"))
                        };
                        if (isExternal)
                        {
                            lnode.IsExternal = isExternal;
                        }

                        structuralNavi.NavigationNodes.Add(lnode);
                    }

                    var localNavigationType = CurrentNavigationType.Structural;
                    if (!string.IsNullOrEmpty(currentNavigationType)
                        && Enum.TryParse(currentNavigationType, out localNavigationType))
                    {
                    }

                    localNavigation = new CurrentNavigation(localNavigationType, structuralNavi);
                }

                navigation = new Navigation(globalNavigation, localNavigation);
            }

            return navigation;
        }

        /// <summary>
        /// Parses the XML content into a collection of list intances
        /// </summary>
        /// <returns></returns>
        public List<PublishingList> GetListInstancesFromConfiguration()
        {
            var lists = new List<PublishingList>();

            var ns = ProvisioningConstants.configns;

            XDocument doc = XDocument.Parse(m_configurationXml);
            var listsNode = doc.Root.Element(ns + "ListInstances");
            foreach (var p in listsNode.Descendants(ns + "List"))
            {
                var list = new PublishingList
                {
                    ListUrl = GetAttribute(p.Attribute("ListUrl")),
                    ContentTypeBindings = new List<PublishingList.PublishingContentTypeBinding>()
                };

                foreach (var ctype in p.Descendants(ns + "ContentTypeBinding"))
                {
                    var contenttype = new PublishingList.PublishingContentTypeBinding
                    {
                        Name = GetAttribute(ctype.Attribute("Name")),
                        FieldLinks = new List<PublishingList.PublishingFieldLink>()
                    };

                    foreach (var fieldLink in p.Descendants(ns + "Field"))
                    {
                        contenttype.FieldLinks.Add(new PublishingList.PublishingFieldLink
                        {
                            InternalName = GetAttribute(fieldLink.Attribute("InternalName"))
                        });
                    }

                    list.ContentTypeBindings.Add(contenttype);
                }

                lists.Add(list);
            }

            return lists;
        }

        /// <summary>
        /// Parses the XML content into a collection of publishing pages
        /// </summary>
        /// <returns></returns>
        public List<PublishingPage> GetPublishingPagesListFromConfiguration()
        {
            List<PublishingPage> pages = new List<PublishingPage>();

            var ns = ProvisioningConstants.configns;

            XDocument doc = XDocument.Parse(m_configurationXml);
            var pagesNode = doc.Root.Element(ns + "Pages");
            if (pagesNode != null)
            {
                foreach (var p in pagesNode.Descendants(ns + "Page"))
                {
                    PublishingPage page = new PublishingPage
                    {
                        Title = GetAttribute(p.Attribute("Title")),
                        Layout = GetAttribute(p.Attribute("Layout")),
                        ListUrl = GetAttribute(p.Attribute("ListUrl")),
                        Overwrite = bool.Parse(GetAttribute(p.Attribute("Overwrite"), "false")),
                        FileName = GetAttribute(p.Attribute("FileName")),
                        TargetFileName = GetAttribute(p.Attribute("TargetFileName")),
                        Publish = bool.Parse(GetAttribute(p.Attribute("Publish"), "false")),
                        IsPublishingPage = bool.Parse(GetAttribute(p.Attribute("IsPublishingPage"), "false"))
                    };

                    if (p.Attribute("WelcomePage") != null)
                    {
                        page.WelcomePage = bool.Parse(GetAttribute(p.Attribute("WelcomePage"), "False"));
                    }

                    var pageContentNode = p.Descendants(ns + "PublishingPageContent").FirstOrDefault();
                    if (pageContentNode != null)
                    {
                        page.PublishingPageContent = GetAttribute(pageContentNode.Attribute("Value"));
                    }

                    foreach (var wp in p.Descendants(ns + "WebPart"))
                    {
                        PublishingPageWebPart publishingPageWebPart = new PublishingPageWebPart();

                        if (wp.Attribute("DefaultViewDisplayName") != null)
                        {
                            publishingPageWebPart.DefaultViewDisplayName = GetAttribute(wp.Attribute("DefaultViewDisplayName"));
                        }

                        publishingPageWebPart.Order = uint.Parse(GetAttribute(wp.Attribute("Order"), "0"));
                        publishingPageWebPart.Title = GetAttribute(wp.Attribute("Title"));
                        publishingPageWebPart.Zone = GetAttribute(wp.Attribute("Zone"));
                        publishingPageWebPart.Src = GetAttribute(wp.Attribute("Src"));

                        string webpartContents = GetElement(wp.Element(ns + "Contents"));

                        if (!string.IsNullOrEmpty(publishingPageWebPart.Src))
                        {
                            // Override to handle invalid XML Elements
                            using (var stream = m_template.Connector.GetFileStream(publishingPageWebPart.Src))
                            {
                                var mxml = new System.Xml.XmlDocument();
                                mxml.Load(stream);
                                publishingPageWebPart.Contents = mxml.OuterXml;
                            }
                        }
                        else
                        {
                            publishingPageWebPart.Contents = webpartContents.Trim(new[] { '\n', ' ' });
                        }

                        page.WebParts.Add(publishingPageWebPart);
                    }

                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    foreach (var property in p.Descendants(ns + "Property"))
                    {
                        properties.Add(
                            GetAttribute(property.Attribute("Name")),
                            GetAttribute(property.Attribute("Value")));
                    }
                    page.Properties = properties;

                    pages.Add(page);
                }
            }

            return pages;
        }


    }
}
