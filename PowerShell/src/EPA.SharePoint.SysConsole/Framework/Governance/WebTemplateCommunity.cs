using EPA.Office365.Extensions;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using System;

namespace EPA.SharePoint.SysConsole.Framework.Governance
{
    /// <summary>
    /// Community Web Template
    /// </summary>
    public class WebTemplateCommunity : WebTemplateBase
    {
        /// <summary>
        /// Initialize the template
        /// </summary>
        /// <param name="watcherDirectory"></param>
        public WebTemplateCommunity(string watcherDirectory) : base(watcherDirectory)
        {
            TemplateConfigurationFile = @"_replacementFiles\ProvisioningPostEvents_Community.xml";
        }

        /// <summary>
        /// Initialize custom template portions for the Community template
        /// </summary>
        /// <param name="template">The provisioning template</param>
        /// <param name="provisionedWeb">The web which has been provisioned</param>
        /// <param name="siteTemplate">The model containing the site to be provisioned and permission/membership</param>
        /// <returns></returns>
        internal override ProvisioningTemplate OnBuildTemplate(ProvisioningTemplate template, Web provisionedWeb, SiteTemplateEntity siteTemplate)
        {
            template.Id = "EPAWebTemplate-Community";
            template.BaseSiteTemplate = "COMMUNITY#0";
            template.Version = 1;
            template.WebSettings.RequestAccessEmail = siteTemplate.siteEntity.SiteOwnerLogin;

            #region Features

            /*
             * Site Features
SocialSite          4326e7fc-f35a-4b0f-927c-36264b0a4cf0    15  Site
Ratings             915c240e-a6cc-49b8-8b2c-0bff8b553ed3    15  Site

             */
            template.Features.SiteFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("4326e7fc-f35a-4b0f-927c-36264b0a4cf0")
            });
            template.Features.SiteFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("915c240e-a6cc-49b8-8b2c-0bff8b553ed3")
            });


            /*
             * Web Features
CommunitySite       961d6a9c-4388-4cf2-9733-38ee8c89afd4    15  Web
ProjectSite         e2f2bb18-891d-4812-97df-c265afdba297    15  Web 
DiscussionList      00bfea71-6a49-43fa-b535-d15c05500108    15  Web
MetaDataNav         7201d6a4-a5d3-49a1-8c19-19c4bac6e668    15  Web
ObaSimpleSolution   d250636f-0a26-4019-8425-a5232d592c01    15  Web
CategoriesList      d32700c7-9ec5-45e6-9c89-ea703efca1df    15  Web
MembershipList      947afd14-0ea1-46c6-be97-dea1bf6f5bae    15  Web
AbuseReportsList    c6a92dbf-6441-4b8b-882f-8d97cb12c83a    15  Web
WebPageLibrary      00bfea71-c796-4402-9f2f-0eb9a6e71b18    15  Web
GettingStarted      4aec7207-0d02-4f4f-aa07-b370199cd0c7    15  Web (Disable)

             */
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("7201d6a4-a5d3-49a1-8c19-19c4bac6e668")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("961d6a9c-4388-4cf2-9733-38ee8c89afd4")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("e2f2bb18-891d-4812-97df-c265afdba297")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature()
            {
                Id = new Guid("00bfea71-6a49-43fa-b535-d15c05500108")
            });
            template.Features.WebFeatures.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.Feature
            {
                Id = new Guid("d250636f-0a26-4019-8425-a5232d592c01")
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
                    Src = ".\\_replacementFiles\\community_home.aspx",
                    Folder = "{site}/SitePages",
                    TargetFileName = "Home.aspx",
                    Overwrite = true,
                    Level = OfficeDevPnP.Core.Framework.Provisioning.Model.FileLevel.Published,
                };
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\titlebar.webpart"),
                    Zone = "TitleBar",
                    Order = 0,
                    Title = "Title bar"
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\ContentEditor.webpart"),
                    Zone = "Body",
                    Order = 0,
                    Title = "Community Detail"
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\community_discussions.webpart"),
                    Zone = "Body",
                    Order = 1,
                    Title = "Community discussions"
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\announcements.webpart"),
                    Title = "Announcements",
                    Zone = "Body",
                    Order = 2
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\documents.webpart"),
                    Zone = "Body",
                    Order = 3,
                    Title = "Documents webpart"
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\Tools.webpart"),
                    Zone = "RightColumn",
                    Order = 0,
                    Title = "Tools webpart"
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\WhatsHappening.webpart"),
                    Zone = "RightColumn",
                    Order = 1,
                    Title = "What's happening"
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\Join.webpart"),
                    Zone = "RightColumn",
                    Order = 2,
                    Title = "Join the site webpart"
                });
                _welcomePage.WebParts.Add(new WebPart
                {
                    Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\community_Members.webpart"),
                    Zone = "RightColumn",
                    Order = 3,
                    Title = "Community members"
                });
                template.Files.Add(_welcomePage);

            }

            var _metadataFile = new OfficeDevPnP.Core.Framework.Provisioning.Model.File()
            {
                Src = ".\\_replacementFiles\\aboutmetadata.aspx",
                Folder = "{site}/{listurl:MetaData}/",
                TargetFileName = "About.aspx",
                Overwrite = true
            };
            _metadataFile.WebParts.Add(new WebPart
            {
                Contents = WatchDirectory.RetreiveWebPartContents(@"_replacementFiles\aboutmetadata.webpart"),
                Title = "About webpart",
                Zone = "Main",
                Order = 0,
                Column = 1,
                Row = 1
            });
            template.Files.Add(_metadataFile);


            #endregion

            #region Site lists and attributes

            var list = new ListInstance();

            list = new ListInstance()
            {
                Title = "Documents",
                Description = "Community template documents",
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
                Description = "Community template Announcements",
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
                Description = "Community template events",
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
                Title = "Discussions List",
                Description = "Community template discussions",
                Url = "Lists/Community Discussion",
                EnableVersioning = false,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)ListTemplateType.DiscussionBoard,
                TemplateFeatureID = Guid.Empty
            };
            template.Lists.Add(list);

            list = new ListInstance()
            {
                Title = "Community Members",
                Description = "Community template contains a list of members for the community",
                Url = "Lists/Members",
                EnableVersioning = false,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)880, // MembershipList
                TemplateFeatureID = Guid.Empty
            };
            template.Lists.Add(list);

            list = new ListInstance()
            {
                Title = "Links and Resources",
                Description = "Community template links and resources",
                Url = "Lists/Links and Resources",
                EnableVersioning = false,
                ListExperience = OfficeDevPnP.Core.Framework.Provisioning.Model.ListExperience.ClassicExperience,
                OnQuickLaunch = false,
                TemplateType = (int)ListTemplateType.Links,
                TemplateFeatureID = Guid.Empty
            };
            template.Lists.Add(list);

            list = new ListInstance()
            {
                Title = "Site Pages",
                Description = "Community template site pages",
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
