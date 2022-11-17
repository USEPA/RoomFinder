using EPA.SharePoint.SysConsole.Framework.Provisioning;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Framework.Governance
{
    public abstract class WebTemplateBase
    {
        public WebTemplateBase()
        {
            TemplateScope = ProvisioningTemplateScope.Web;
        }

        /// <summary>
        /// Instantiate the Web Template
        /// </summary>
        /// <param name="watcherDirectory"></param>
        public WebTemplateBase(string watcherDirectory) : this()
        {
            WatchDirectory = watcherDirectory;
        }

        internal ProvisioningTemplateScope TemplateScope { get; set; }

        internal string WatchDirectory { get; set; }

        /// <summary>
        /// Should the template overwrite the Welcome Page contents
        /// </summary>
        internal bool OverwriteWelcomePage { get; set; }

        /// <summary>
        /// Contains a Leaf path to the postevent configuration file
        /// </summary>
        public string TemplateConfigurationFile { get; internal set; }


        /// <summary>
        /// Build the PnP template from the specific site template request
        /// </summary>
        /// <param name="provisionedWeb">The provisioned web to which the template will be applied</param>
        /// <param name="siteTemplate">The site template</param>
        /// <param name="overwriteDefault">(OPTIONAL) default to true and overwrite the WelcomePage; false skip and leave existing page in place</param>
        /// <returns></returns>
        public ProvisioningTemplate RetreiveTemplate(Web provisionedWeb, SiteTemplateEntity siteTemplate, bool overwriteDefault = true)
        {
            OverwriteWelcomePage = overwriteDefault;

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
                    RequestAccessEmail = siteTemplate.SiteOwnerEmail,
                    Description = siteTemplate.SiteRequest.Description
                },
                RegionalSettings = new OfficeDevPnP.Core.Framework.Provisioning.Model.RegionalSettings()
                {
                    CalendarType = CalendarType.None,
                    FirstDayOfWeek = DayOfWeek.Sunday,
                    FirstWeekOfYear = 1,
                    LocaleId = 1033,
                    TimeZone = 10, // Eastern Time Zone
                    WorkDayEndHour = WorkHour.PM0500,
                    WorkDays = 62,
                    WorkDayStartHour = WorkHour.AM0800,
                    ShowWeeks = true,
                    Time24 = false
                },
                Security = new SiteSecurity
                {
                    BreakRoleInheritance = (!siteTemplate.inheritPermissions), // Opposite of permission choice
                    CopyRoleAssignments = false,
                    ClearSubscopes = false
                }
            };

            if (OverwriteWelcomePage)
            {
                template.WebSettings.SiteLogo = "{site}/SiteAssets/images/my_workplace_175.png";
                template.WebSettings.Title = siteTemplate.SiteRequest.Title;
                template.WebSettings.WelcomePage = "SitePages/Home.aspx";
            }

            template.SupportedUILanguages.Add(new SupportedUILanguage
            {
                LCID = 1033
            });

            // Associated Site Owner Group
            var gOwner = new SiteGroup
            {
                Title = "{sitename} Owners",
                AllowMembersEditMembership = true,
                RequestToJoinLeaveEmailSetting = siteTemplate.siteEntity.SiteOwnerLogin,
                Owner = string.Format("{0}{1}", ProvisioningConstants.ClaimPrefix, siteTemplate.siteEntity.SiteOwnerLogin),
                Description = "Use this group to grant people full control permissions to the SharePoint site <a href=\"{site}\">{sitename}</a>"
            };
            foreach (var owner in siteTemplate.additionalOwners)
            {
                gOwner.Members.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.User
                {
                    Name = string.Format("{0}{1}", ProvisioningConstants.ClaimPrefix, owner)
                });
            }
            template.Security.SiteGroups.Add(gOwner);
            template.Security.SiteSecurityPermissions.RoleAssignments.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.RoleAssignment
            {
                Principal = gOwner.Title,
                RoleDefinition = "Full Control"
            });

            // Associated Site Member Group
            var gMember = new SiteGroup
            {
                Title = "{sitename} Members",
                AllowMembersEditMembership = true,
                OnlyAllowMembersViewMembership = false,
                AllowRequestToJoinLeave = true,
                RequestToJoinLeaveEmailSetting = siteTemplate.siteEntity.SiteOwnerLogin,
                Owner = gOwner.Title,
                Description = "Use this group to grant people contribute permissions to the SharePoint site <a href=\"{site}\">{sitename}</a>"
            };
            foreach (var member in siteTemplate.additionalMembers)
            {
                gMember.Members.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.User
                {
                    Name = string.Format("{0}{1}", ProvisioningConstants.ClaimPrefix, member)
                });
            }
            template.Security.SiteGroups.Add(gMember);
            template.Security.SiteSecurityPermissions.RoleAssignments.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.RoleAssignment
            {
                Principal = gMember.Title,
                RoleDefinition = "Contribute"
            });

            // Associated Site Visitors Group
            var gVisitor = new SiteGroup
            {
                Title = "{sitename} Visitors",
                AllowMembersEditMembership = true,
                OnlyAllowMembersViewMembership = false,
                AllowRequestToJoinLeave = true,
                RequestToJoinLeaveEmailSetting = siteTemplate.siteEntity.SiteOwnerLogin,
                Owner = gOwner.Title,
                Description = "Use this group to grant people read permissions to the SharePoint site <a href=\"{site}\">{sitename}</a>"
            };
            template.Security.SiteSecurityPermissions.RoleAssignments.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.RoleAssignment
            {
                Principal = gVisitor.Title,
                RoleDefinition = "Read"
            });

            foreach (var visitor in siteTemplate.additionalVisitors)
            {
                gVisitor.Members.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.User
                {
                    Name = string.Format("{0}{1}", ProvisioningConstants.ClaimPrefix, visitor)
                });
            }

            if (siteTemplate.shareSiteWithEveryone && !string.IsNullOrEmpty(siteTemplate.EveryoneGroupTenantId))
            {
                gVisitor.Members.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.User
                {
                    Name = siteTemplate.EveryoneGroupTenantId
                });
            }
            template.Security.SiteGroups.Add(gVisitor);


            foreach (var admin in siteTemplate.additionalAdministrators)
            {
                template.Security.AdditionalAdministrators.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.User
                {
                    Name = string.Format("{0}{1}", ProvisioningConstants.ClaimPrefix, admin)
                });
            }



            // Initialize the core template
            template = OnBuildTemplate(template, provisionedWeb, siteTemplate);

            // Add common core capabilities
            template = ExtendMetadata(template, siteTemplate, siteUrl);



            // Configuration Data
            if (!string.IsNullOrEmpty(TemplateConfigurationFile))
            {
                // Associate the handler for the PostEvents
                template.ExtensibilityHandlers.Add(new ExtensibilityHandler
                {
                    Enabled = true,
                    Assembly = "EPA.SharePoint.SysConsole",
                    Type = "EPA.SharePoint.SysConsole.Framework.Provisioning.ProvisioningExtensibilityHandler",
                    Configuration = TemplateConfigurationFile
                });
            }

            return template;
        }

        /// <summary>
        /// Extends the PnP model with the Metadata List
        /// </summary>
        /// <param name="template"></param>
        /// <param name="siteTemplate"></param>
        /// <param name="siteUrl"></param>
        /// <returns></returns>
        private ProvisioningTemplate ExtendMetadata(ProvisioningTemplate template, SiteTemplateEntity siteTemplate, string siteUrl)
        {

            var list = new ListInstance()
            {
                Title = "MetaData",
                Description = "Work template metadata used for search and discovery",
                Url = "Lists/MetaData",
                EnableVersioning = false,
                EnableFolderCreation = false,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)ListTemplateType.GenericList,
                TemplateFeatureID = Guid.Empty,
                ContentTypesEnabled = false
            };
            list.Security = new ObjectSecurity()
            {
                ClearSubscopes = false,
                CopyRoleAssignments = true
            };
            list.Security.RoleAssignments.AddRange(new OfficeDevPnP.Core.Framework.Provisioning.Model.RoleAssignment[]
            {
                new OfficeDevPnP.Core.Framework.Provisioning.Model.RoleAssignment
                {
                    Principal = siteTemplate.EveryoneGroupTenantId,
                    RoleDefinition = "Read"
                }
            });
            list.ContentTypeBindings.Add(new ContentTypeBinding
            {
                ContentTypeId = "0x01",
                Default = true
            });
            list.ContentTypeBindings.Add(new ContentTypeBinding
            {
                ContentTypeId = "0x0120"
            });

            var xmlFields = new Dictionary<string, string>();
            xmlFields.Add("Link", "<Field ID='{e601695e-46ba-4d17-b208-33cb54278a02}' Type='Text' DisplayName='Link' Required='FALSE' EnforceUniqueValues='FALSE' Indexed='FALSE' MaxLength='255' StaticName='Link' Name='Link' ColName='nvarchar3' RowOrdinal='0' />");
            xmlFields.Add("SiteType", "<Field ID='{588b6612-68dc-4740-8b97-95e07a3fd8d0}' Type='Choice' DisplayName='SiteType' Required='FALSE' EnforceUniqueValues='FALSE' Indexed='FALSE' Format='Dropdown' FillInChoice='FALSE' StaticName='SiteType' Name='SiteType' ColName='nvarchar4' RowOrdinal='0'><Default>Organization</Default><CHOICES><CHOICE>Work Site</CHOICE><CHOICE>Organization</CHOICE><CHOICE>Community Site</CHOICE></CHOICES></Field>");
            xmlFields.Add("Description", "<Field ID='{9a1200ff-ab9b-41f5-8d97-7b86b22e97c7}' Type='Note' DisplayName='Description' Required='FALSE' EnforceUniqueValues='FALSE' Indexed='FALSE' NumLines='6' RichText='FALSE' RichTextMode='Compatible' IsolateStyles='FALSE' Sortable='FALSE' StaticName='Description' Name='Description' ColName='ntext2' RowOrdinal='0' RestrictedMode='TRUE' AppendOnly='FALSE' />");
            xmlFields.Add("SiteName", "<Field ID='{bb4f265e-f9ac-4bb2-a456-42cc9f3f2bd8}' DisplayName='SiteName' Name='SiteName' Type='Text' StaticName='SiteName' ColName='nvarchar5' RowOrdinal='0' />");
            xmlFields.Add("JoinFlag", "<Field ID='{84699a62-ee70-4363-b582-a3b6e814ae69}' DisplayName='JoinFlag' Name='JoinFlag' Type='Text' StaticName='JoinFlag' ColName='nvarchar6' RowOrdinal='0' />");
            xmlFields.Add("OpenFlag", "<Field ID='{c5479db6-6634-4d02-abc0-e69cfe82869b}' DisplayName='OpenFlag' Name='OpenFlag' Type='Text' StaticName='OpenFlag' ColName='nvarchar7' RowOrdinal='0' />");
            xmlFields.Add("AAShipRegionOffice", "<Field ID='{4924ea1e-2821-4c29-9fb7-b1772392a906}' DisplayName='AAShipRegionOffice' Name='AAShipRegionOffice' Type='Text' StaticName='AAShipRegionOffice' ColName='nvarchar8' RowOrdinal='0' />");
            xmlFields.Add("Topics", "<Field ID='{e317532d-1bd8-4db0-95ed-77e41b0d141b}' DisplayName='Topics' Name='Topics' Type='Text' StaticName='Topics' ColName='nvarchar9' RowOrdinal='0' />");
            xmlFields.Add("OfficeTeamCommunityName", "<Field ID='{2713409e-a655-46ba-bc39-5b038f323690}' DisplayName='OfficeTeamCommunityName' Name='OfficeTeamCommunityName' Type='Text' StaticName='OfficeTeamCommunityName'  ColName='nvarchar10' RowOrdinal='0' />");
            xmlFields.Add("EpaLineOfBusiness", "<Field ID='{b4d38d22-cf3b-4f91-96d3-c9a0b23c4d63}' DisplayName='EpaLineOfBusiness' Name='EpaLineOfBusiness' Type='Text' StaticName='EpaLineOfBusiness' ColName='nvarchar11' RowOrdinal='0' />");
            xmlFields.Add("SiteOwner", "<Field ID='{86ea40dd-cb26-44bb-a0ba-b439de4123d7}' DisplayName='SiteOwner' Name='SiteOwner' Type='Text'  StaticName='SiteOwner'  ColName='nvarchar12' RowOrdinal='0' />");
            xmlFields.Add("SiteOwnerName", "<Field ID='{dc31c5e0-88c4-4ee1-96f3-7751b7e6d127}' DisplayName='SiteOwnerName' Name='SiteOwnerName' Type='Text'  StaticName='SiteOwnerName'  ColName='nvarchar12' RowOrdinal='0' />");
            xmlFields.Add("SiteSponsor", "<Field ID='{8fb4b860-6118-4a7f-a37b-5e9309232529}' DisplayName='SiteSponsor' Name='SiteSponsor' Type='Text'  StaticName='SiteSponsor'  ColName='nvarchar13' RowOrdinal='0' />");
            xmlFields.Add("OrganizationAcronym", "<Field ID='{10889873-5cd3-4bf0-989f-b5f67dfa0141}' DisplayName='OrganizationAcronym' Name='OrganizationAcronym' Type='Text'  StaticName='OrganizationAcronym'  ColName='nvarchar14' RowOrdinal='0' />");

            list.Fields.AddRange(xmlFields.Select(s => new OfficeDevPnP.Core.Framework.Provisioning.Model.Field { SchemaXml = s.Value }));

            var viewFields = new List<string>();
            viewFields.Add("LinkTitle");
            viewFields.AddRange(xmlFields.Select(s => s.Key));

            var camlFields = CAML.ViewFields(viewFields.Select(s => CAML.FieldRef(s)).ToArray());
            list.Views.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.View
            {
                SchemaXml = @"<View Name='{257078DD-3147-40BF-AB0E-79D2285AB2AF}' DefaultView='TRUE' MobileView='TRUE' MobileDefaultView='TRUE' Type='HTML' DisplayName='All Items' Url='{site}/Lists/MetaData/AllItems.aspx' Level='1' BaseViewID='1' ContentTypeID='0x' ImageUrl='/_layouts/15/images/generic.png?rev=44'>"
                            + string.Format("<Query>{0}{1}</Query>{2}<RowLimit Paged='TRUE'>{3}</RowLimit><JSLink>clienttemplates.js</JSLink></View>",
                                CAML.OrderBy(new OrderByField("ID")),
                                string.Empty,
                                camlFields,
                                30)
            });

            // Site Request MetaData
            var siteRequestDetails = new DataRow
            {
                Key = siteTemplate.SiteRequest.SiteName
            };
            siteRequestDetails.Values.Add("Title", "{sitename}");
            siteRequestDetails.Values.Add("Link", UrlUtility.Combine(siteUrl, "/Lists/MetaData", "/About.aspx"));
            siteRequestDetails.Values.Add("SiteName", siteTemplate.SiteRequest.SiteName);
            siteRequestDetails.Values.Add("Description", siteTemplate.SiteRequest.Description);
            siteRequestDetails.Values.Add("JoinFlag", siteTemplate.SiteRequest.JoinFlag);
            siteRequestDetails.Values.Add("OpenFlag", siteTemplate.SiteRequest.OpenFlag);
            siteRequestDetails.Values.Add("SiteOwner", siteTemplate.SiteRequest.SiteOwner);
            siteRequestDetails.Values.Add("SiteOwnerName", siteTemplate.SiteRequest.SiteOwnerName);
            siteRequestDetails.Values.Add("SiteSponsor", siteTemplate.SiteRequest.SiteSponsor);
            siteRequestDetails.Values.Add("AAShipRegionOffice", siteTemplate.SiteRequest.AAShipRegionOffice);
            siteRequestDetails.Values.Add("Topics", siteTemplate.SiteRequest.Topics);
            siteRequestDetails.Values.Add("SiteType", siteTemplate.SiteRequest.TypeOfSite);
            siteRequestDetails.Values.Add("OfficeTeamCommunityName", siteTemplate.SiteRequest.OfficeTeamCommunityName);
            siteRequestDetails.Values.Add("EpaLineOfBusiness", siteTemplate.SiteRequest.EpaLineOfBusiness);
            siteRequestDetails.Values.Add("OrganizationAcronym", siteTemplate.SiteRequest.OrganizationAcronym);

            list.DataRows.KeyColumn = "SiteName";
            list.DataRows.UpdateBehavior = UpdateBehavior.Overwrite;
            list.DataRows.Add(siteRequestDetails);

            template.Lists.Add(list);


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
