using EPA.Office365;
using EPA.SharePoint.SysConsole.Framework.Governance;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class ProvisioningSecurityHandler
    {
        private ClientContext clientContext;
        private Web provisionedWeb;
        private TokenParser m_tokenParser;
        private ITraceLogger scope;


        public ProvisioningSecurityHandler(ClientContext ctx, ProvisioningTemplate template, ProvisioningTemplateApplyingInformation applyingInformation, TokenParser tokens, ITraceLogger diagnostics)
        {
            clientContext = ctx;
            provisionedWeb = ctx.Web;
            scope = diagnostics;
            m_tokenParser = tokens;
        }

        /// <summary>
        /// Provisions the Site Security based on the template and the associated membership groups
        /// </summary>
        /// <param name="web"></param>
        /// <param name="template"></param>
        /// <param name="applyingInformation"></param>
        /// <param name="siteRequestTemplate">The extracted site template from the site request</param>
        /// <param name="overwriteAssociatedGroups">(OPTIONAL) defaults to resetting the associated groups; false if leave existing associated groups</param>
        /// <returns></returns>
        /// <exception cref="FormatException">Throw exception if the Site Request does not match the current Site Title</exception>
        public TokenParser ProvisionObjects(Web web, ProvisioningTemplate template, ProvisioningTemplateApplyingInformation applyingInformation, SiteTemplateEntity siteRequestTemplate, bool overwriteAssociatedGroups = true)
        {
            if (!WillProvision(web, template))
            {
                scope.LogInformation("Template does not contain provisionable objects; skipping Security provisioning");
                return m_tokenParser;
            }

            var siteSecurity = template.Security;



            var currentAssociatedOwnerName = RetreiveGroupToken("{associatedownergroup}");
            var currentAssociatedMemberName = RetreiveGroupToken("{associatedmembergroup}");
            var currentAssociatedVisitorName = RetreiveGroupToken("{associatedvisitorgroup}");

            var associatedOwnerName = RetreiveGroupToken("{sitename} Owners");
            var associatedMemberName = RetreiveGroupToken("{sitename} Members");
            var associatedVisitorName = RetreiveGroupToken("{sitename} Visitors");



            // If this is a sub web and we are breaking inheritance
            if (web.IsSubSite() && siteSecurity.BreakRoleInheritance && siteSecurity.BreakRoleInheritance != web.HasUniqueRoleAssignments)
            {
                web.BreakRoleInheritance(siteSecurity.CopyRoleAssignments, siteSecurity.ClearSubscopes);
                web.Update();
                web.Context.ExecuteQueryRetry();
            }

            siteSecurity = SortSecurityGroups(siteSecurity);


            // Only process Associated Groups if Associated groups do not exist
            if (string.IsNullOrEmpty(currentAssociatedOwnerName)
                || string.IsNullOrEmpty(currentAssociatedMemberName)
                || string.IsNullOrEmpty(currentAssociatedVisitorName))
            {


            }


            foreach (var siteGroup in siteSecurity.SiteGroups)
            {
                Group group;
                var allGroups = web.Context.LoadQuery(web.SiteGroups.Include(gr => gr.LoginName));
                web.Context.ExecuteQueryRetry();

                string parsedGroupTitle = m_tokenParser.ParseString(siteGroup.Title);
                string parsedGroupOwner = m_tokenParser.ParseString(siteGroup.Owner);
                string parsedGroupDescription = m_tokenParser.ParseString(siteGroup.Description);
                bool descriptionHasHtml = HttpUtility.HtmlEncode(parsedGroupDescription) != parsedGroupDescription;

                if (!web.GroupExists(parsedGroupTitle))
                {
                    scope.LogInformation("Creating group {0}", parsedGroupTitle);
                    group = web.AddGroup(
                        parsedGroupTitle,
                        parsedGroupDescription,
                        parsedGroupTitle == parsedGroupOwner);
                    group.AllowMembersEditMembership = siteGroup.AllowMembersEditMembership;
                    group.AllowRequestToJoinLeave = siteGroup.AllowRequestToJoinLeave;
                    group.AutoAcceptRequestToJoinLeave = siteGroup.AutoAcceptRequestToJoinLeave;
                    group.OnlyAllowMembersViewMembership = siteGroup.OnlyAllowMembersViewMembership;
                    group.RequestToJoinLeaveEmailSetting = siteGroup.RequestToJoinLeaveEmailSetting;

                    if (parsedGroupTitle != parsedGroupOwner)
                    {
                        Principal ownerPrincipal = allGroups.FirstOrDefault(gr => gr.LoginName == parsedGroupOwner);
                        if (ownerPrincipal == null)
                        {
                            ownerPrincipal = web.EnsureUser(parsedGroupOwner);
                        }
                        group.Owner = ownerPrincipal;

                    }
                    group.Update();
                    web.Context.Load(group, g => g.Id, g => g.Title);
                    web.Context.ExecuteQueryRetry();

                    if (descriptionHasHtml)
                    {
                        var groupItem = web.SiteUserInfoList.GetItemById(group.Id);
                        groupItem["Notes"] = parsedGroupDescription;
                        groupItem.Update();
                        web.Context.ExecuteQueryRetry();
                    }
                }
            }


            // With the change from october, manage permission levels on subsites as well
            if (siteSecurity.SiteSecurityPermissions != null)
            {
                var existingRoleDefinitions = web.Context.LoadQuery(web.RoleDefinitions.Include(wr => wr.Name, wr => wr.BasePermissions, wr => wr.Description));
                web.Context.ExecuteQueryRetry();

                var webRoleDefinitions = web.Context.LoadQuery(web.RoleDefinitions);
                var webRoleAssignments = web.Context.LoadQuery(web.RoleAssignments);
                var groups = web.Context.LoadQuery(web.SiteGroups.Include(g => g.LoginName));
                web.Context.ExecuteQueryRetry();

                if (siteSecurity.SiteSecurityPermissions.RoleAssignments.Any())
                {
                    foreach (var roleAssignment in siteSecurity.SiteSecurityPermissions.RoleAssignments)
                    {
                        if (!roleAssignment.Remove)
                        {
                            var roleDefinition = webRoleDefinitions.FirstOrDefault(r => r.Name == m_tokenParser.ParseString(roleAssignment.RoleDefinition));
                            if (roleDefinition != null)
                            {
                                Principal principal = GetPrincipal(web, groups, roleAssignment);

                                if (principal != null)
                                {
                                    var roleDefinitionBindingCollection = new RoleDefinitionBindingCollection(web.Context);
                                    roleDefinitionBindingCollection.Add(roleDefinition);
                                    web.RoleAssignments.Add(principal, roleDefinitionBindingCollection);
                                    web.Context.ExecuteQueryRetry();
                                }
                            }
                            else
                            {
                                scope.LogWarning("Role assignment {0} not found in web", roleAssignment.RoleDefinition);
                            }
                        }
                        else
                        {
                            var principal = GetPrincipal(web, groups, roleAssignment);
                            var assignmentsForPrincipal = webRoleAssignments.Where(t => t.PrincipalId == principal.Id);
                            foreach (var assignmentForPrincipal in assignmentsForPrincipal)
                            {
                                var binding = assignmentForPrincipal.EnsureProperty(r => r.RoleDefinitionBindings).FirstOrDefault(b => b.Name == roleAssignment.RoleDefinition);
                                if (binding != null)
                                {
                                    assignmentForPrincipal.DeleteObject();
                                    web.Context.ExecuteQueryRetry();
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Associate groups
            if (web.IsSubSite() && siteSecurity.BreakRoleInheritance)
            {
                scope.LogInformation("ProcessRequest. AssociateDefaultGroups");

                var isDirty = false;

                var associatedGroups = provisionedWeb.Context.LoadQuery(provisionedWeb.SiteGroups.Where(w =>
                         w.Title == associatedOwnerName
                         || w.Title == associatedMemberName
                         || w.Title == associatedVisitorName));
                provisionedWeb.Context.ExecuteQueryRetry();


                var associatedOwnerGroup = provisionedWeb.AssociatedOwnerGroup;
                if (associatedOwnerGroup.ServerObjectIsNull()
                    || (overwriteAssociatedGroups && !associatedOwnerGroup.ServerObjectIsNull() && associatedOwnerGroup.Title != associatedOwnerName))
                {
                    if (associatedGroups.Any(w => w.Title == associatedOwnerName))
                    {
                        isDirty = true;
                        associatedOwnerGroup = associatedGroups.FirstOrDefault(w => w.Title == associatedOwnerName);
                    }
                }

                var associatedMemberGroup = provisionedWeb.AssociatedMemberGroup;
                if (associatedMemberGroup.ServerObjectIsNull()
                    || (overwriteAssociatedGroups && !associatedMemberGroup.ServerObjectIsNull() && !associatedMemberGroup.Title.Equals(associatedMemberName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    if (associatedGroups.Any(w => w.Title == associatedMemberName))
                    {
                        isDirty = true;
                        associatedMemberGroup = associatedGroups.FirstOrDefault(w => w.Title == associatedMemberName);
                    }
                }

                var associatedVisitorGroup = provisionedWeb.AssociatedVisitorGroup;
                if (associatedVisitorGroup.ServerObjectIsNull()
                    || (overwriteAssociatedGroups && !associatedVisitorGroup.ServerObjectIsNull() && associatedVisitorGroup.Title != associatedVisitorName))
                {
                    if (associatedGroups.Any(w => w.Title == associatedVisitorName))
                    {
                        isDirty = true;
                        associatedVisitorGroup = associatedGroups.FirstOrDefault(w => w.Title == associatedVisitorName);
                    }
                }

                if (isDirty)
                {
                    scope.LogWarning("Revising the associated membership groups");
                    provisionedWeb.AssociateDefaultGroups(associatedOwnerGroup, associatedMemberGroup, associatedVisitorGroup);
                }
            }

            return m_tokenParser;
        }

        /// <summary>
        /// Returns the token value from the collection
        /// </summary>
        /// <param name="tokenName"></param>
        /// <returns></returns>
        private string RetreiveGroupToken(string tokenName)
        {
            var tokenValue = string.Empty;

            try
            {
                tokenValue = m_tokenParser.ParseString(tokenName);

            }
            catch (Exception ex)
            {
                scope.LogError(ex, "Failed to obtain token {0} with {1}", tokenName, ex.StackTrace);
            }

            return tokenValue;
        }

        /// <summary>
        /// sorting groups with respect to possible dependency through Owner property. Groups that are owners of other groups must be processed prior owned groups.
        /// </summary>
        /// <param name="siteSecurity"></param>
        /// <returns></returns>
        private SiteSecurity SortSecurityGroups(SiteSecurity siteSecurity)
        {
            for (int i = siteSecurity.SiteGroups.Count - 1; i >= 0; i--)
            {
                var currentGroup = siteSecurity.SiteGroups[i];
                string currentGroupOwner = m_tokenParser.ParseString(currentGroup.Owner);
                string currentGroupTitle = m_tokenParser.ParseString(currentGroup.Title);

                if (currentGroupOwner != "SHAREPOINT\\system" && currentGroupOwner != currentGroupTitle && !(currentGroupOwner.StartsWith("{{associated") && currentGroupOwner.EndsWith("group}}")))
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (m_tokenParser.ParseString(siteSecurity.SiteGroups[j].Owner) == currentGroupTitle)
                        {
                            siteSecurity.SiteGroups.RemoveAt(i);
                            siteSecurity.SiteGroups.Insert(j, currentGroup);
                            i++;
                            break;
                        }
                    }
                }
            }

            return siteSecurity;
        }

        private Principal GetPrincipal(Web web, IEnumerable<Group> groups, OfficeDevPnP.Core.Framework.Provisioning.Model.RoleAssignment roleAssignment)
        {
            //TODO: Register this as a bug based on site of groups and parsing principal
            var principalName = m_tokenParser.ParseString(roleAssignment.Principal);
            Principal principal = groups.FirstOrDefault(g => g.LoginName == principalName);

            if (principal == null)
            {
                var parsedUser = m_tokenParser.ParseString(roleAssignment.Principal);
                if (parsedUser.Contains("#ext#"))
                {
                    principal = web.SiteUsers.FirstOrDefault(u => u.LoginName.Equals(parsedUser));

                    if (principal == null)
                    {
                        scope.LogInformation("Skipping external user {0}", parsedUser);
                    }
                }
                else
                {
                    try
                    {
                        principal = web.EnsureUser(parsedUser);
                        web.Context.ExecuteQueryRetry();
                    }
                    catch (Exception ex)
                    {
                        scope.LogError(ex, "Failed to EnsureUser {0}", parsedUser);
                    }
                }
            }
            principal.EnsureProperty(p => p.Id);
            return principal;
        }




        private Nullable<bool> _willProvision;
        public bool WillProvision(Web web, ProvisioningTemplate template)
        {
            if (!_willProvision.HasValue)
            {
                _willProvision = template.Security != null && (
                                  template.Security.SiteGroups.Any() ||
                                  template.Security.SiteSecurityPermissions.RoleAssignments.Any() ||
                                  template.Security.SiteSecurityPermissions.RoleDefinitions.Any());
                if (_willProvision == true)
                {
                    // if subweb and site inheritance is not broken
                    if (web.IsSubSite() && template.Security.BreakRoleInheritance == false
                        && web.EnsureProperty(w => w.HasUniqueRoleAssignments) == false)
                    {
                        _willProvision = false;
                    }
                }
            }

            return _willProvision.Value;

        }

    }
}
