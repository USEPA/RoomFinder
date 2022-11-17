using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using System;

namespace EPA.SharePoint.SysConsole.Framework.Governance
{
    public abstract class SiteTemplateBase
    {
        public SiteTemplateBase()
        {
            TemplateScope = ProvisioningTemplateScope.RootSite;
        }

        public SiteTemplateBase(string watcherDirectory) : this()
        {
            WatchDirectory = watcherDirectory;
        }

        internal ProvisioningTemplateScope TemplateScope { get; set; }

        internal string WatchDirectory { get; set; }


        /// <summary>
        /// Build the PnP template from the specific site template request
        /// </summary>
        /// <param name="provisionedWeb">The provisioned web to which the template will be applied</param>
        /// <param name="siteTemplate">The site template</param>
        /// <returns></returns>
        public ProvisioningTemplate RetreiveTemplate(Web provisionedWeb, SiteTemplateEntity siteTemplate)
        {

            if (!provisionedWeb.IsPropertyAvailable(ctx => ctx.Url))
            {
                provisionedWeb.EnsureProperties(ctx => ctx.Url);
            }

            // Grab the context of the to be provisioned site
            var siteUrl = provisionedWeb.Url;

            // Core initialization
            var template = new ProvisioningTemplate()
            {
                Scope = TemplateScope,
                WebSettings = new WebSettings()
                {
                    NoCrawl = true,
                    SiteLogo = "{site}/SiteAssets/images/my_workplace_175.png"
                },
                RegionalSettings = new OfficeDevPnP.Core.Framework.Provisioning.Model.RegionalSettings()
                {
                    CalendarType = CalendarType.None,
                    FirstDayOfWeek = DayOfWeek.Sunday,
                    FirstWeekOfYear = 0,
                    LocaleId = 1033,
                    TimeZone = 10, // Eastern Time Zone
                    WorkDayEndHour = WorkHour.PM0500,
                    WorkDays = 62,
                    WorkDayStartHour = WorkHour.AM0800
                }
            };
            template.SupportedUILanguages.Add(new SupportedUILanguage
            {
                LCID = 1033
            });


            // Initialize the core template
            template = OnBuildTemplate(template, provisionedWeb, siteTemplate);


            return template;
        }

        /// <summary>
        /// Ensures the extending class implements the specifics for the template
        /// </summary>
        /// <param name="template">The provisioning template</param>
        /// <param name="provisionedWeb">The web which has been provisioned</param>
        /// <param name="siteTemplate">The model containing the site to be provisioned and permission/membership</param>
        /// <returns></returns>
        internal abstract ProvisioningTemplate OnBuildTemplate(ProvisioningTemplate template, Web provisionedWeb, SiteTemplateEntity siteTemplate);
    }
}
