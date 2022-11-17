using EPA.Office365.Extensions;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using System;

namespace EPA.SharePoint.SysConsole.Framework.Governance
{
    /// <summary>
    /// Organization Web Template
    /// </summary>
    public class WebTemplateOrganization : WebTemplateBase
    {
        /// <summary>
        /// Initialize the template
        /// </summary>
        /// <param name="watcherDirectory"></param>
        public WebTemplateOrganization(string watcherDirectory) : base(watcherDirectory)
        {
            TemplateConfigurationFile = @"_replacementFiles\ProvisioningPostEvents_Organization.xml";
        }

        /// <summary>
        /// Initialize custom template portions for the Organization template
        /// </summary>
        /// <param name="template">The provisioning template</param>
        /// <param name="provisionedWeb">The web which has been provisioned</param>
        /// <param name="siteTemplate">The model containing the site to be provisioned and permission/membership</param>
        /// <returns></returns>
        internal override ProvisioningTemplate OnBuildTemplate(ProvisioningTemplate template, Web provisionedWeb, SiteTemplateEntity siteTemplate)
        {
            template.Id = "EPAWebTemplate-Organization";
            template.BaseSiteTemplate = "STS#0";
            template.Version = 2;
            template.WebSettings.RequestAccessEmail = siteTemplate.siteEntity.SiteOwnerLogin;

            #region Features

            /*
             * Site Features


             */




            /*
             * Web Features
ProjectSite         e2f2bb18-891d-4812-97df-c265afdba297    15  Web 
MetaDataNav         7201d6a4-a5d3-49a1-8c19-19c4bac6e668    15  Web
ObaSimpleSolution   d250636f-0a26-4019-8425-a5232d592c01    15  Web
WebPageLibrary      00bfea71-c796-4402-9f2f-0eb9a6e71b18    15  Web
SitePages           b6917cb1-93a0-4b97-a84d-7cf49975d4ec    15  Web  -- Disable if you want Modern Sites
GettingStarted      4aec7207-0d02-4f4f-aa07-b370199cd0c7    15  Web (Disable)

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
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature()
            {
                Id = new Guid("00bfea71-c796-4402-9f2f-0eb9a6e71b18")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature()
            {
                Id = new Guid("4aec7207-0d02-4f4f-aa07-b370199cd0c7"),
                Deactivate = true
            });

            #endregion

            #region Pages and Page Layouts

            var _pageMembers = new OfficeDevPnP.Core.Framework.Provisioning.Model.Page
            {
                Url = "{site}/SitePages/Members.aspx",
                Overwrite = true,
                Layout = OfficeDevPnP.Core.WikiPageLayout.OneColumnSideBar
            };

            _pageMembers.WebParts.Add(new WebPart
            {
                Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\SiteUsers.webpart"),
                Column = 1,
                Row = 1,
                Title = "Site users"
            });
            template.Pages.Add(_pageMembers);


            #endregion

            #region Site Files and Webparts


            var _workplaceImageFile = new OfficeDevPnP.Core.Framework.Provisioning.Model.File()
            {
                Folder = "{site}/SiteAssets/images/",
                Overwrite = true,
                Level = OfficeDevPnP.Core.Framework.Provisioning.Model.FileLevel.Published,
                Src = ".\\_replacementFiles\\SiteAssets\\images\\my_workplace_175.png"
            };
            _workplaceImageFile.Properties.Add("Title", "my_workplace_175.png");
            _workplaceImageFile.Security = new ObjectSecurity()
            {
                ClearSubscopes = false,
                CopyRoleAssignments = true
            };
            _workplaceImageFile.Security.RoleAssignments.AddRange(new OfficeDevPnP.Core.Framework.Provisioning.Model.RoleAssignment[]
            {
                new OfficeDevPnP.Core.Framework.Provisioning.Model.RoleAssignment
                {
                    Principal = siteTemplate.EveryoneGroupTenantId,
                    RoleDefinition = "Read"
                }
            });
            template.Files.Add(_workplaceImageFile);



            if (OverwriteWelcomePage)
            {


                var _welcomePage = new OfficeDevPnP.Core.Framework.Provisioning.Model.File()
                {
                    Src = ".\\_replacementFiles\\org_home.aspx",
                    Folder = "{site}/SitePages",
                    TargetFileName = "Home.aspx",
                    Overwrite = true,
                    Level = OfficeDevPnP.Core.Framework.Provisioning.Model.FileLevel.Published,
                };
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\titlebar.webpart"),
                    Title = "Title Bar",
                    Zone = "TitleBar",
                    Order = 0
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\org_discussions.webpart"),
                    Title = "Organization Discussions",
                    Zone = "LeftColumn",
                    Order = 0
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\calendar.webpart"),
                    Title = "Calendar",
                    Zone = "LeftColumn",
                    Order = 1
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\announcements.webpart"),
                    Title = "Announcements",
                    Zone = "RightColumn",
                    Order = 0
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\documents.webpart"),
                    Title = "Shared Documents",
                    Zone = "RightColumn",
                    Order = 1
                });
                template.Files.Add(_welcomePage);

            }


            var _pageAboutUs = new OfficeDevPnP.Core.Framework.Provisioning.Model.File()
            {
                Src = ".\\_replacementFiles\\aboutmetadata.aspx",
                Folder = "{site}/{listurl:MetaData}/",
                TargetFileName = "About.aspx",
                Overwrite = true,
                Level = OfficeDevPnP.Core.Framework.Provisioning.Model.FileLevel.Published
            };
            //_pageAboutUs.WebParts.Add(new WebPart
            //{
            //    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\aboutmetadata.webpart"),
            //    Title = "About",
            //    Zone = "Main",
            //    Order = 0,
            //    Column = 1,
            //    Row = 1
            //});
            template.Files.Add(_pageAboutUs);


            #endregion

            #region Site lists and attributes

            var list = new ListInstance();

            list = new ListInstance()
            {
                Title = "Documents",
                Description = "Organization template documents",
                Url = "Shared Documents",
                EnableVersioning = true,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)ListTemplateType.DocumentLibrary,
                TemplateFeatureID = Guid.Empty
            };
            template.Lists.Add(list);

            list = new ListInstance()
            {
                Title = "Announcements",
                Description = "Organization template Announcements",
                Url = "Lists/Announcements",
                EnableVersioning = false,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)ListTemplateType.Announcements,
                TemplateFeatureID = Guid.Empty
            };
            template.Lists.Add(list);

            list = new ListInstance()
            {
                Title = "Calendar",
                Description = "Organization template events",
                Url = "Lists/Calendar",
                EnableVersioning = false,
                EnableFolderCreation = false,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)ListTemplateType.Events,
                TemplateFeatureID = Guid.Empty
            };
            template.Lists.Add(list);

            list = new ListInstance()
            {
                Title = "Discussion Board",
                Description = "Organization template discussions",
                Url = "Lists/DiscussionBoard",
                EnableVersioning = false,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)ListTemplateType.DiscussionBoard,
                TemplateFeatureID = Guid.Empty
            };
            template.Lists.Add(list);

            list = new ListInstance()
            {
                Title = "Site Pages",
                Description = "Organization template site pages",
                Url = "SitePages",
                EnableVersioning = true,
                EnableAttachments = false,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)ListTemplateType.WebPageLibrary,
                TemplateFeatureID = Guid.Empty
            };
            template.Lists.Add(list);


            #endregion

            return template;
        }
    }
}
