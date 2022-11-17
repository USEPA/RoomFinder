using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using System;

namespace EPA.SharePoint.SysConsole.Framework.Governance
{
    /// <summary>
    /// Represents the Organization Site collection Template
    /// </summary>
    public class SiteTemplateOrganization : SiteTemplateBase
    {
        /// <summary>
        /// Initialize the template
        /// </summary>
        /// <param name="watcherDirectory"></param>
        public SiteTemplateOrganization(string watcherDirectory) : base(watcherDirectory) { }
        
        /// <summary>
        /// Ensures the extending class implements the specifics for the template
        /// </summary>
        /// <param name="template">The provisioning template</param>
        /// <param name="provisionedWeb">The web which has been provisioned</param>
        /// <param name="siteTemplate">The model containing the site to be provisioned and permission/membership</param>
        /// <returns></returns>
        internal override ProvisioningTemplate OnBuildTemplate(ProvisioningTemplate template, Web provisionedWeb, SiteTemplateEntity siteTemplate)
        {

            var siteUrl = provisionedWeb.Url;


            template.Id = "EPASiteTemplate-Organization";
            template.BaseSiteTemplate = "STS#0";
            template.Version = 1;
            template.WebSettings.RequestAccessEmail = siteTemplate.siteEntity.SiteOwnerLogin;

            /*
             * Site Features


             */




            /*
             * Web Features
ProjectSite         e2f2bb18-891d-4812-97df-c265afdba297    15  Web 
MetaDataNav         7201d6a4-a5d3-49a1-8c19-19c4bac6e668    15  Web
ObaSimpleSolution   d250636f-0a26-4019-8425-a5232d592c01    15  Web
SitePages           b6917cb1-93a0-4b97-a84d-7cf49975d4ec    15  Web  -- Disable if you want Modern Sites

             */
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("e2f2bb18-891d-4812-97df-c265afdba297")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("7201d6a4-a5d3-49a1-8c19-19c4bac6e668")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("d250636f-0a26-4019-8425-a5232d592c01")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("b6917cb1-93a0-4b97-a84d-7cf49975d4ec")
            });


            /*
             * Upload Images to the Site Collection files
             */
            template.Directories.Add(new Directory
            {
                Src = ".\\_replacementFiles\\SiteAssets\\images",
                Folder = "{site}/SiteAssets/images/",
                Overwrite = true,
                Recursive = false,
                Level = OfficeDevPnP.Core.Framework.Provisioning.Model.FileLevel.Published
            });


            /*
             * Master Pages / JavaScript files
             */




            return template;
        }
    }
}
