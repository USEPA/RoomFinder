using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using System;

namespace EPA.SharePoint.SysConsole.Framework.Governance
{
    /// <summary>
    /// Represents the Community Site collection Template
    /// </summary>
    public class SiteTemplateCommunity : SiteTemplateBase
    {
        /// <summary>
        /// Initialize the template
        /// </summary>
        /// <param name="watcherDirectory"></param>
        public SiteTemplateCommunity(string watcherDirectory) : base(watcherDirectory) { }
        
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


            template.Id = "EPASiteTemplate-Community";
            template.BaseSiteTemplate = "COMMUNITY#0";
            template.Version = 1;
            template.WebSettings.RequestAccessEmail = siteTemplate.siteEntity.SiteOwnerLogin;

            /*
             * Site Features
SocialSite          4326e7fc-f35a-4b0f-927c-36264b0a4cf0    15  Site
Ratings             915c240e-a6cc-49b8-8b2c-0bff8b553ed3    15  Site

             */
            template.Features.SiteFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("4326e7fc-f35a-4b0f-927c-36264b0a4cf0") // SocialSite
            });
            template.Features.SiteFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("915c240e-a6cc-49b8-8b2c-0bff8b553ed3") // RatingsFeatureReceiver
            });


            /*
             * Web Features
DiscussionList      00bfea71-6a49-43fa-b535-d15c05500108    15  Web
CategoriesList      d32700c7-9ec5-45e6-9c89-ea703efca1df    15  Web
MembershipList      947afd14-0ea1-46c6-be97-dea1bf6f5bae    15  Web
AbuseReportsList    c6a92dbf-6441-4b8b-882f-8d97cb12c83a    15  Web
WebPageLibrary      00bfea71-c796-4402-9f2f-0eb9a6e71b18    15  Web
SitePages           b6917cb1-93a0-4b97-a84d-7cf49975d4ec    15  Web  -- Disable if you want Modern Sites

             */
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("00bfea71-6a49-43fa-b535-d15c05500108")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature()
            {
                Id = new Guid("d32700c7-9ec5-45e6-9c89-ea703efca1df") 
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature()
            {
                Id = new Guid("947afd14-0ea1-46c6-be97-dea1bf6f5bae")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature()
            {
                Id = new Guid("c6a92dbf-6441-4b8b-882f-8d97cb12c83a")
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
