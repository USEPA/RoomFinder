using EPA.Office365;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.Models.Governance;
using EPA.SharePoint.SysConsole.Framework.Governance;
using EPA.SharePoint.SysConsole.Models.Apps;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Entities;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    /// <summary>
    /// Provides a core capability to provision EPA Sites based on the respective templates
    /// </summary>
    public class EPAProvisioner
    {
        public EPAProvisioner()
        {
            AdditionalAdministrators = new List<string>();
        }

        public EPAProvisioner(ITraceLogger traceLogger) : this()
        {
            Ilogger = traceLogger;
        }

        /// <summary>
        /// Initialize the provisioner with the Azure AD Attributes
        /// </summary>
        /// <param name="traceLogger"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="realmId">The Azure AD / Tenant ID Realm to which this is authenticating</param>
        public EPAProvisioner(ITraceLogger traceLogger, string clientId, string clientSecret, string realmId = null) : this(traceLogger)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            RealmId = realmId;
        }

        private readonly string ClientId;
        private readonly string ClientSecret;
        private readonly string RealmId;
        private static string EveryoneGroupTenantId = ConstantsTenant.EveryoneGroupTenantId;
        private bool SendEmailMessage;

        /// <summary>
        /// Contains a log implementation for extraction at the calling location
        /// </summary>
        internal ITraceLogger Ilogger { get; private set; }

        /// <summary>
        /// A collection of user Ids that will be granted SCA access to the Site Collection
        /// </summary>
        internal List<string> AdditionalAdministrators { get; set; }

        /// <summary>
        /// Update the collection of Additional Admins
        /// </summary>
        /// <param name="admins"></param>
        public void UpdateAdditionalAdministrators(IList<string> admins)
        {
            AdditionalAdministrators = admins.ToList();
        }

        /// <summary>
        /// Update the send email message flag
        /// </summary>
        /// <param name="sendMessage"></param>
        public void RelayEmailNotification(bool sendMessage)
        {
            SendEmailMessage = sendMessage;
        }

        /// <summary>
        /// Apply the constructed template to the specified <paramref name="siteRequestId"/> site request
        /// </summary>
        /// <param name="WatchDirectory">Contains the Full Directory path to the root folder containing the template files</param>
        /// <param name="tenantAdminUrl">The Tenant Admin URI ex: https://[tenant]-admin.sharepoint.com</param>
        /// <param name="siteRequestUrl">The Site request URI ex: https://[tenant].sharepoint.com/sites/siterequest</param>
        /// <param name="siteRequestId">The Site request to be provisioned</param>
        /// <param name="overwriteWelcomePage">(OPTIONAL) should we overwrite the home/welcomepage</param>
        /// <param name="overwriteAssociatedGroups">(OPTIONAL) defaults to overwriting the associated membership groups</param>
        /// <param name="overwriteNavigationNodes">(OPTIONAL) defaults to removing nodes, false to add to</param>
        /// <remarks>
        /// Ensure your app/web config contains a valid ClientID,ClientSecret that has been registered with Tenant authorization
        /// </remarks>
        public void ExecuteProvisioner(string WatchDirectory, Uri tenantAdminUrl, string siteRequestUrl, int siteRequestId, bool overwriteWelcomePage = true, bool overwriteAssociatedGroups = true, bool overwriteNavigationNodes = true)
        {

            var authManager = new OfficeDevPnP.Core.AuthenticationManager();
            using (var tenantContext = authManager.GetAppOnlyAuthenticatedContext(tenantAdminUrl.ToString(), ClientId, ClientSecret))
            {
                var tenant = new Tenant(tenantContext);
                if (tenant.Context == null)
                {
                    tenantContext.Load(tenant);
                    tenantContext.ExecuteQueryRetry();
                }


                // Retreive the Site Request
                var siteRequestTemplate = new SiteTemplateEntity();
                var siteRequestAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
                using (var siteRequestCtx = siteRequestAuthManager.GetAppOnlyAuthenticatedContext(siteRequestUrl, ClientId, ClientSecret))
                {
                    siteRequestTemplate = GetSiteRequest(siteRequestCtx, siteRequestId);
                }

                // Request was previously completed
                if (siteRequestTemplate.SiteRequest.RequestCompletedFlag.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
                {
                    Ilogger.LogWarning("The site request {0} was previously completed.", siteRequestId);
                }
                else
                {
                    // Open the site collection
                    var targetSiteCollectionUrl = siteRequestTemplate.SiteCollectionUrl;
                    var newWebAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
                    using (var ctx = newWebAuthManager.GetAppOnlyAuthenticatedContext(targetSiteCollectionUrl, ClientId, ClientSecret))
                    {

                        try
                        {
                            try
                            {
                                ctx.Load(ctx.Site, sctx => sctx.Id);
                                ctx.Load(ctx.Web, w => w.Title, sctx => sctx.Id);
                                ctx.ExecuteQueryRetry();
                            }
                            catch (IdcrlException iex)
                            {
                                Ilogger.LogError(iex, "{0}: {1}", "ctx.ExecuteQueryRetry", iex.Message);
                                return;
                            }
                            catch (XmlException xex)
                            {
                                Ilogger.LogError(xex, "{0}: {1}", "ctx.ExecuteQueryRetry", xex.Message);
                                return;
                            }


                            Ilogger.LogInformation("The Destination URL of the SharePoint Online Site Collection site title is: {0}", ctx.Web.Title);



                            var targetWebUrl = UrlUtility.Combine(targetSiteCollectionUrl, siteRequestTemplate.siteEntity.Url);
                            var targetWebUri = new Uri(targetWebUrl);

                            try
                            {
                                Web provisionedWeb = null;
                                if (!ctx.WebExistsFullUrl(targetWebUrl))
                                {
                                    try
                                    {
                                        provisionedWeb = ctx.Web.CreateWeb(siteRequestTemplate.siteEntity, siteRequestTemplate.inheritPermissions, siteRequestTemplate.inheritNavigation); //NOTE: Site is created, but receiving an access error: https://github.com/SharePoint/PnP/issues/1619
                                    }
                                    catch (Microsoft.SharePoint.Client.ServerUnauthorizedAccessException suaEx)
                                    {
                                        System.Diagnostics.Trace.TraceError("Unauthorized Access Exception {0}", suaEx.Message);
                                        Ilogger.LogError(suaEx, "ERROR: {0}", suaEx.Message);
                                    }
                                    catch (Exception ex)
                                    {
                                        Ilogger.LogError(ex, ex.Message);
                                        return;
                                    }
                                    finally
                                    {
                                        Ilogger.LogInformation("The Site, {0}, has been successfully created: {1}.", siteRequestTemplate.siteEntity.Title, targetWebUrl);
                                    }
                                }
                                else
                                {
                                    provisionedWeb = ctx.Web.GetWeb(siteRequestTemplate.siteEntity.Url);
                                }


                                ctx.Load(provisionedWeb,
                                    wctx => wctx.Navigation,
                                    wctx => wctx.HasUniqueRoleAssignments,
                                    cwtx => cwtx.ServerRelativeUrl,
                                    cwtx => cwtx.Title,
                                    cwtx => cwtx.Url,
                                    cwtx => cwtx.Id);
                                ctx.ExecuteQueryRetry();



                                // Get the template from existing site and serialize
                                WebTemplateBase webTemplate = null;
                                if (siteRequestTemplate.SiteRequest.TypeOfSite.Equals("organization", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    Ilogger.LogInformation("Grabbing template {0}", siteRequestTemplate.SiteRequest.TypeOfSite);
                                    webTemplate = new WebTemplateOrganization(WatchDirectory);
                                }
                                else if (siteRequestTemplate.SiteRequest.TypeOfSite.Equals("community site", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    Ilogger.LogInformation("Grabbing template {0}", siteRequestTemplate.SiteRequest.TypeOfSite);
                                    webTemplate = new WebTemplateCommunity(WatchDirectory);
                                }
                                else if (siteRequestTemplate.SiteRequest.TypeOfSite.Equals("work site", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    Ilogger.LogInformation("Grabbing template {0}", siteRequestTemplate.SiteRequest.TypeOfSite);
                                    webTemplate = new WebTemplateWork(WatchDirectory);
                                }

                                ProvisioningTemplate template = webTemplate.RetreiveTemplate(provisionedWeb, siteRequestTemplate, overwriteWelcomePage);

                                // Associate file connector for assets
                                template.Connector = new FileSystemConnector(WatchDirectory, "");


                                ProvisioningTemplateApplyingInformation ptai = new ProvisioningTemplateApplyingInformation
                                {
                                    ClearNavigation = overwriteNavigationNodes,
                                    ProgressDelegate = delegate (String message, Int32 progress, Int32 total)
                                    {
                                        Ilogger.LogInformation("{0:00}/{1:00} - {2}", progress, total, message);
                                    }
                                };


                                if (template != null)
                                {
                                    Ilogger.LogInformation("Template constructed => Begin provisioning template {0}", targetWebUrl);

                                    var applyTemplateAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
                                    using (var provisioningctx = applyTemplateAuthManager.GetAppOnlyAuthenticatedContext(targetWebUrl, ClientId, ClientSecret))
                                    {
                                        // Apply template to new site 
                                        ApplyProvisioningTemplate(provisioningctx, template, ptai, siteRequestTemplate, overwriteAssociatedGroups);
                                    }

                                    // The template was applied successfully, lets update the request to be completed
                                    using (var siteRequestCtx = siteRequestAuthManager.GetAppOnlyAuthenticatedContext(siteRequestUrl, ClientId, ClientSecret))
                                    {
                                        UpdateSiteRequestAndSendMail(siteRequestCtx, siteRequestTemplate, siteRequestId, provisionedWeb);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Ilogger.LogError(ex, ex.Message);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Ilogger.LogError(ex, "Failed to retrieve Site Collection {0}", ex.Message);
                            return;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Will update the request with the provisioned web attributes and relay an email to the User
        /// </summary>
        /// <param name="siteRequestCtx">The client context for the Site Request application</param>
        /// <param name="siteRequestTemplate">The extracted site template from the site request</param>
        /// <param name="requestId">The unique id of the site request</param>
        /// <param name="provisionedWeb"></param>
        internal void UpdateSiteRequestAndSendMail(ClientContext siteRequestCtx, SiteTemplateEntity siteRequestTemplate, int requestId, Web provisionedWeb)
        {
            var provisionedWebId = provisionedWeb.Id;
            var provisionedWebUrl = provisionedWeb.Url;
            var provisionedWebTitle = provisionedWeb.Title;

            var typeOfSiteId = siteRequestTemplate.SiteRequest.TypeOfSiteID;
            var requestorEmail = siteRequestTemplate.SiteRequest.RequestorEmail;
            var siteOwner = siteRequestTemplate.SiteRequest.SiteOwner;
            var siteUrl = siteRequestTemplate.SiteRequest.SiteUrl;
            var siteTitle = siteRequestTemplate.siteEntity.Title;


            var fields = new string[] { "AdminEmail", "AdminEmailObject", "EmailTo" };

            var scaListing = new List<string>();
            var adminLkList = siteRequestCtx.Web.GetListByTitle(Constants_SiteRequest.AdminsLK);
            ListItemCollectionPosition adminPosition = null;
            var adminCamlQuery = new CamlQuery()
            {
                ViewXml = CAML.ViewQuery(
                    ViewScope.RecursiveAll,
                    CAML.Where("<Eq><FieldRef Name='CollectionSiteType' LookupId='TRUE' /><Value Type='Lookup'>" + typeOfSiteId + "</Value></Eq>"),
                    string.Empty,
                    CAML.ViewFields(fields.Select(s => CAML.FieldRef(s)).ToArray()),
                    100)
            };

            while (true)
            {
                adminCamlQuery.ListItemCollectionPosition = adminPosition;
                var adminItems = adminLkList.GetItems(adminCamlQuery);
                adminLkList.Context.Load(adminItems, actx => actx.ListItemCollectionPosition);
                adminLkList.Context.ExecuteQueryRetry();
                adminPosition = adminItems.ListItemCollectionPosition;


                foreach (var adminItem in adminItems)
                {
                    var adminEmail = adminItem.RetrieveListItemValue(Constants_SiteRequest.AdminsLKFields.FieldText_AdminEmail);
                    var adminUser = adminItem.RetrieveListItemUserValue(Constants_SiteRequest.AdminsLKFields.FieldUser_AdminEmailObject);
                    if (adminUser != null)
                    {
                        scaListing.Add(adminUser.ToUserEmailValue());
                    }
                    else
                    {
                        scaListing.Add(adminEmail.Trim());
                    }

                }

                if (adminPosition == null)
                {
                    break;
                }
            }



            var siterequestlist = siteRequestCtx.Web.GetListByTitle(Constants_SiteRequest.SiteRequestListName);
            var siterequestitem = siterequestlist.GetItemById(requestId);
            siteRequestCtx.Load(siterequestlist);
            siteRequestCtx.Load(siterequestitem);
            siterequestitem[Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteExists] = 1;
            siterequestitem[Constants_SiteRequest.SiteRequestFields.FieldText_WebGuid] = provisionedWebId;
            siterequestitem[Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag] = "Yes";
            siterequestitem.Update();


            if (SendEmailMessage)
            {
                var emailBody = new StringBuilder();
                emailBody.AppendLine("<html><head><title>Site Created successfully</title></head><body>");
                emailBody.AppendLine("<div><span style=\"font-size:.95em\">Hello</span></div>");
                emailBody.AppendFormat("<div>The site {0} has been created.  It can be found at: <a href=\"{1}\" title=\"{0}\">{0}</a></div>", provisionedWebTitle, provisionedWebUrl);
                emailBody.AppendLine("<p>Thank You, </br>Site request application</p>");
                emailBody.AppendLine("<div style=\"font-size:.75em\">*note* this is an unmonitored inbox</div>");


                var emailTo = string.Empty;
                emailTo = requestorEmail + (!string.IsNullOrEmpty(siteOwner) ? string.Format(";{0}", siteOwner) : "");

                var emailPlaceholder = siteRequestCtx.Web.GetListByTitle(Constants_SiteRequest.EmailerPlaceholderListName);
                var emailCreation = new ListItemCreationInformation();
                var emailItem = emailPlaceholder.AddItem(emailCreation);
                emailItem[Constants_SiteRequest.EmailerPlaceholderFields.Field_Title] = "Site Created Successfully";
                emailItem[Constants_SiteRequest.EmailerPlaceholderFields.FieldMultiText_EmailTo] = emailTo;
                emailItem[Constants_SiteRequest.EmailerPlaceholderFields.FieldText_EmailCC] = string.Join(";", scaListing);
                emailItem[Constants_SiteRequest.EmailerPlaceholderFields.FieldText_EmailFrom] = "sharepointadmin@usepa.onmicrosoft.com";
                emailItem[Constants_SiteRequest.EmailerPlaceholderFields.FieldMultiText_Body] = emailBody.ToString();
                emailItem.Update();

                siteRequestCtx.Load(emailPlaceholder);
                siteRequestCtx.Load(emailItem);
            }
            siteRequestCtx.ExecuteQueryRetry();


        }


        internal SiteTemplateEntity GetSiteRequest(ClientContext siteRequestCtx, int requestId)
        {
            var template = new SiteTemplateEntity
            {
                inheritPermissions = false,
                inheritNavigation = false,
                additionalAdministrators = AdditionalAdministrators
            };


            try
            {
                // Pull the Everyone Group from memory
                var visitorGroups = siteRequestCtx.LoadQuery(siteRequestCtx.Web.SiteGroups.Where(w => w.Title == ConstantsTenant.uggroup).Include(i => i.LoginName));
                siteRequestCtx.ExecuteQueryRetry();

                foreach (var visitor in visitorGroups)
                {
                    EveryoneGroupTenantId = visitor.LoginName;
                    break;
                }

                var visitorUsers = siteRequestCtx.LoadQuery(siteRequestCtx.Web.SiteUsers.Where(w => w.Title == ConstantsTenant.uggroup).Include(i => i.LoginName));
                siteRequestCtx.ExecuteQueryRetry();

                foreach (var visitor in visitorUsers)
                {
                    EveryoneGroupTenantId = visitor.LoginName;
                    break;
                }
            }
            catch (IdcrlException iex)
            {
                Ilogger.LogError(iex, "{0}: {1}", "ctx.ExecuteQueryRetry", iex.Message);
                return null;
            }
            catch (XmlException xex)
            {
                Ilogger.LogError(xex, "{0}: {1}", "ctx.ExecuteQueryRetry", xex.Message);
                return null;
            }



            var siterequestlist = siteRequestCtx.Web.GetListByTitle(Constants_SiteRequest.SiteRequestListName);
            var siterequestitem = siterequestlist.GetItemById(requestId);
            siteRequestCtx.Load(siterequestlist);
            siteRequestCtx.Load(siterequestitem);
            siteRequestCtx.ExecuteQueryRetry();

            var SiteOwner = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteOwner);
            var SiteRequestor = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestorEmail);
            var SiteSponsor = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteSponsor);
            var typeOfSiteId = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_TypeOfSiteID).ToInt32(0);
            var siteCollection = siterequestitem.RetrieveListItemValueAsLookup(Constants_SiteRequest.SiteRequestFields.FieldLookup_SiteCollectionName);
            var aashipOffice = siterequestitem.RetrieveListItemValueAsLookup(Constants_SiteRequest.SiteRequestFields.FieldLookup_AAShipRegionOffice);

            template.SiteRequest = new Model_SiteRequestItem()
            {
                Id = siterequestitem.Id,
                SiteUrl = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteURL).ToLower(),
                Title = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.Field_Title).Trim(),
                SiteName = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteName),
                Description = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldMultiText_Description).Trim(),
                missingMetaDataId = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_missingMetaDataId),
                TypeOfSite = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.ChoiceField_TypeOfSite),
                TypeOfSiteID = typeOfSiteId,
                RequestorEmail = SiteRequestor,
                SiteSponsorApprovedFlag = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteSponsorApprovedFlag),
                RequestCompletedFlag = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag),
                RequestRejectedFlag = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestRejectedFlag),
                OpenFlag = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_OpenFlag),
                JoinFlag = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_JoinFlag),
                SiteOwner = SiteOwner,
                SiteOwnerName = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteOwnerName),
                EpaLineOfBusiness = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_EpaLineOfBusiness),
                Topics = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_Topics),
                TemplateName = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_TemplateName),
                SiteSponsor = SiteSponsor,
                IntendedAudience = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_IntendedAudience),
                OfficeTeamCommunityName = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_OfficeTeamCommunityName),
                OrganizationAcronym = siterequestitem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_OrganizationAcronym),
                AAShipRegionOffice = siteCollection.ToLookupValue()
            };


            var sitecollectiontypeslist = siteRequestCtx.Web.GetListByTitle(Constants_SiteRequest.CollectionSiteTypesLK);
            var sitecollectiontypesitem = sitecollectiontypeslist.GetItemById(typeOfSiteId);
            siteRequestCtx.Load(sitecollectiontypeslist);
            siteRequestCtx.Load(sitecollectiontypesitem);
            siteRequestCtx.ExecuteQueryRetry();

            var templateName = sitecollectiontypesitem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_TemplateName);
            var siteCollectionUrl = sitecollectiontypesitem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_CollectionURL);
            var SiteType = sitecollectiontypesitem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteType);

            siteCollectionUrl = siteCollectionUrl.EnsureTrailingSlashLowered();
            var newSiteUrl = template.SiteRequest.SiteUrl.Replace(siteCollectionUrl.ToLower(), string.Empty);

            // Add the Site Owner 
            template.additionalOwners.Add(SiteOwner);
            template.additionalOwners.Add(SiteRequestor);

            // Add the members
            if (!string.IsNullOrEmpty(SiteSponsor))
            {
                template.additionalMembers.Add(SiteSponsor);
            }

            template.SiteCollectionUrl = siteCollectionUrl;
            template.EveryoneGroupTenantId = EveryoneGroupTenantId;
            template.shareSiteWithEveryone = template.SiteRequest.OpenFlag.Equals("on", StringComparison.CurrentCultureIgnoreCase);
            template.SiteOwnerEmail = SiteOwner;
            template.siteEntity = new SiteEntity
            {
                Url = newSiteUrl,
                Title = template.SiteRequest.Title,
                Description = template.SiteRequest.Description,
                SiteOwnerLogin = template.SiteRequest.SiteOwner,
                Template = templateName
            };



            return template;
        }

        /// <summary>
        /// Apply the Provisioning template to the site
        /// </summary>
        /// <param name="ctx">Context of the Web being modified</param>
        /// <param name="template">Provisioning Template Definition</param>
        /// <param name="ptai">Applying Template handlers</param>
        /// <param name="siteRequestTemplate">The extracted site template from the site request</param>
        /// <param name="overwriteAssociatedGroups">(OPTIONAL) defaults to overwriting the associated groups if not already created/specified</param>
        internal void ApplyProvisioningTemplate(ClientContext ctx, ProvisioningTemplate template, ProvisioningTemplateApplyingInformation ptai, SiteTemplateEntity siteRequestTemplate, bool overwriteAssociatedGroups = true)
        {

            // Just to output the site details
            Web provisionedWeb = ctx.Web;
            ctx.Load(provisionedWeb,
                w => w.Title,
                wctx => wctx.Navigation,
                wctx => wctx.HasUniqueRoleAssignments,
                cwtx => cwtx.ServerRelativeUrl,
                cwtx => cwtx.Title,
                wctx => wctx.HasUniqueRoleAssignments,
                wctx => wctx.AssociatedMemberGroup,
                wctx => wctx.AssociatedOwnerGroup,
                wctx => wctx.AssociatedVisitorGroup);

            try
            {
                ctx.ExecuteQueryRetry(50, 1000);
            }
            catch (IdcrlException iex)
            {
                Ilogger.LogError(iex, "ApplyProvisioningTemplate ERROR: {0}", iex.Message);
                return;
            }
            catch (XmlException xex)
            {
                Ilogger.LogError(xex, "ApplyProvisioningTemplate ERROR: {0}", xex.Message);
                return;
            }


            // Validate if the Navigation is inheriting or not and set to site config
            var inheritNavigation = siteRequestTemplate.inheritNavigation;
            if (provisionedWeb.Navigation.UseShared != inheritNavigation)
            {
                provisionedWeb.UpdateNavigationInheritance(inheritNavigation);
            }

            Ilogger.LogInformation("Building token parser for pre-provisioning details");

            // grab tokens for preprocessing
            var tokenParser = new TokenParser(provisionedWeb, template);

            var siteName = tokenParser.ParseString("{sitename}");
            if (!siteName.Equals(siteRequestTemplate.siteEntity.Title, StringComparison.CurrentCulture))
            {
                throw new FormatException(string.Format("The site request {0} does not match the current site settings", siteRequestTemplate.siteEntity.Title));
            }

            // Load the parser into memory to handle breaking permissions
            var securityHandler = new ProvisioningSecurityHandler(ctx, template, ptai, tokenParser, Ilogger);
            tokenParser = securityHandler.ProvisionObjects(provisionedWeb, template, ptai, siteRequestTemplate, overwriteAssociatedGroups);

            // Apply the standard template
            provisionedWeb.ApplyProvisioningTemplate(template, ptai);

        }

    }
}
