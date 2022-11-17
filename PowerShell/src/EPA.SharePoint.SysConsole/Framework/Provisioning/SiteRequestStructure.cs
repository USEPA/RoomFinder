using EPA.Office365;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.Framework.Models;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    /// <summary>
    /// Applies the PnP Template along with the pre-provisioner to push files specified
    /// </summary>
    public class SiteRequestStructure
    {
        /// <summary>
        /// http://schemas.usepa.com/ProvisioningExtensibilityHandlerConfiguration
        /// </summary>
        private static readonly XNamespace configns = "http://schemas.usepa.com/ProvisioningExtensibilityHandlerConfiguration";

        /// <summary>
        /// Process the specified PnP Template files and the pre-processing file
        /// </summary>
        /// <param name="WatchDirectory"></param>
        /// <param name="ilogger"></param>
        /// <param name="siteRequestCtx"></param>
        /// <param name="PnPTemplateFile"></param>
        /// <param name="PnPTemplatePages"></param>
        /// <param name="PnPTemplateId"></param>
        public void ApplyTemplate(string WatchDirectory,
            ITraceLogger ilogger,
            ClientContext siteRequestCtx,
            string PnPTemplateFile,
            string PnPTemplatePages,
            string PnPTemplateId)
        {


            FileSystemConnector fileConnector = new FileSystemConnector(WatchDirectory, "");
            var stream = fileConnector.GetFileStream(PnPTemplateFile);
            var xmlContent = fileConnector.GetFile(PnPTemplatePages);


            var provider = new XMLFileSystemTemplateProvider(fileConnector.Parameters[FileConnectorBase.CONNECTIONSTRING] + "", "");
            var provisioningTemplate = provider.GetTemplate(PnPTemplateFile, identifier: PnPTemplateId);

            // Associate file connector for assets
            provisioningTemplate.Connector = fileConnector;


            var ptai = new ProvisioningTemplateApplyingInformation
            {
                ClearNavigation = true,
                ProgressDelegate = delegate (String message, Int32 progress, Int32 total)
                {
                    ilogger.LogInformation("{0:00}/{1:00} - {2}", progress, total, message);
                }
            };



            var provisionedWeb = siteRequestCtx.Web;
            try
            {
                siteRequestCtx.Load(provisionedWeb,
                    wctx => wctx.Navigation,
                    wctx => wctx.HasUniqueRoleAssignments,
                    cwtx => cwtx.ServerRelativeUrl,
                    cwtx => cwtx.RootFolder,
                    cwtx => cwtx.Title,
                    cwtx => cwtx.Url,
                    cwtx => cwtx.Id);
                siteRequestCtx.ExecuteQueryRetry();


            }
            catch (IdcrlException iex)
            {
                ilogger.LogError(iex, "{0}: {1}", "ctx.ExecuteQueryRetry", iex.Message);
                return;
            }
            catch (XmlException xex)
            {
                ilogger.LogError(xex, "{0}: {1}", "ctx.ExecuteQueryRetry", xex.Message);
                return;
            }


            var foldersToProcess = GetXmlData(xmlContent);
            foreach (var folder in foldersToProcess)
            {
                // get root folder Pages
                var pagesFolder = provisionedWeb.RootFolder.GetOrCreateFolder(folder.Url);


                foreach (var file in folder.Files)
                {
                    var fileInfo = new System.IO.FileInfo(file.Src);
                    var overwriteIfExists = file.Overwrite;

                    var fileFullPath = System.IO.Path.Combine(WatchDirectory, file.Src);
                    ilogger.LogInformation("Uploading {0} to {1}", file.Src, folder.Url);
                    var uploaded = siteRequestCtx.UploadFileViaREST(folder.Url, fileFullPath);
                    ilogger.LogInformation("File {0} uploaded with status [{1}]", file.Src, uploaded);
                }
            }

            // Apply the standard template
            provisionedWeb.ApplyProvisioningTemplate(provisioningTemplate, ptai);
        }

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


        private List<EntityFolder> GetXmlData(string configurationXml)
        {
            var lists = new List<EntityFolder>();


            XDocument doc = XDocument.Parse(configurationXml);
            var listsNode = doc.Root.Element(configns + "Folders");
            foreach (var p in listsNode.Descendants(configns + "Folder"))
            {
                var list = new EntityFolder
                {
                    Url = GetAttribute(p.Attribute("Url")),
                    Files = new List<EntityFile>()
                };

                foreach (var folderFile in p.Descendants(configns + "File"))
                {
                    var eFile = new EntityFile
                    {
                        Src = GetAttribute(folderFile.Attribute("Src")),
                        Overwrite = GetAttribute(folderFile.Attribute("Overwrite")).ToBoolean(false),
                        IsWelcomePage = GetAttribute(folderFile.Attribute("IsWelcomePage")).ToBoolean(false)
                    };

                    list.Files.Add(eFile);
                }

                lists.Add(list);
            }

            return lists;
        }

    }




}
