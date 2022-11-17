using EPA.Office365.oAuth;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EPA.Office365.Graph
{
    /// <summary>
    /// HTTP Utility to interact with Office 365 Groups
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Too much effort to handle exceptions.")]
    public class GraphUtility
    {
        private const int defaultRetryCount = 10;
        private const int defaultDelay = 500;

        public GraphUtility() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCache">ADAL Tokens</param>
        /// <param name="logger">Serilog Logger</param>
        public GraphUtility(IOAuthTokenCache tokenCache, Serilog.ILogger logger)
        {
            TokenCache = tokenCache;
            TraceLogger = logger;
        }

        private Serilog.ILogger TraceLogger { get; set; }

        /// <summary>
        /// Provides cache store for ADAL Tokens
        /// </summary>
        private IOAuthTokenCache TokenCache { get; set; }

        /// <summary>
        /// Provides a MS Graph interface
        /// </summary>
        private GraphServiceClient GraphService { get; set; }

        /// <summary>
        /// Initializes the Client with a Bearer token
        /// </summary>
        /// <param name="retryCount">Number of times to retry the request in case of throttling</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <returns></returns>
        public GraphServiceClient CreateGraphClient(int retryCount = defaultRetryCount, int delay = defaultDelay)
        {
            // Creates a new GraphServiceClient instance using a custom GraphHttpProvider
            // which natively supports retry logic for throttled requests
            // Default are 10 retries with a base delay of 500ms
            var result = new GraphServiceClient(new DelegateAuthenticationProvider(
                        async (requestMessage) =>
                        {
                            // Configure the HTTP bearer Authorization Header
                            Diagnostics.Log.LogDebug($"Authenticate HttpRequest {requestMessage.RequestUri}");
                            await TokenCache.AuthenticateRequestMessageAsync(requestMessage, string.Empty);
                        }), new GraphHttpProvider(TraceLogger, retryCount, delay));

            GraphService = result;
            return (result);
        }

        /// <summary>
        /// Returns the URL of the Modern SharePoint Document Library backing an Office 365 Group (i.e. Unified Group)
        /// </summary>
        /// <param name="groupId">The ID of the Office 365 Group</param>
        /// <returns>The URL of the modern document library backing the Office 365 Group</returns>
        public async Task<string> GetUnifiedGroupDocumentUrl(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentNullException(nameof(groupId));
            }

            string documentUrl = null;
            try
            {
                var groupDrive = await GraphService.Groups[groupId].Drive.Request().GetAsync();
                if (groupDrive != null)
                {
                    documentUrl = groupDrive?.WebUrl;
                    if (string.IsNullOrEmpty(documentUrl))
                    {
                        var rootFolder = await GraphService.Groups[groupId].Drive.Root.Request().GetAsync();
                        if (rootFolder != null)
                        {
                            documentUrl = rootFolder.WebUrl;
                        }
                    }
                }
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
            }

            return documentUrl;
        }

        /// <summary>
        /// Returns the URL of the Modern SharePoint Site backing an Office 365 Group (i.e. Unified Group)
        /// </summary>
        /// <param name="documentUrl">Document Library fully qualified URL</param>
        /// <returns>The URL of the modern site backing the Office 365 Group</returns>
        public static string GetUnifiedGroupSiteUrl(string documentUrl)
        {
            string siteUrl = "";
            if (!string.IsNullOrEmpty(documentUrl))
            {
                var modernSiteUrl = documentUrl;
                siteUrl = modernSiteUrl.Substring(0, modernSiteUrl.LastIndexOf("/"));
            }
            return siteUrl;
        }

        /// <summary>
        /// Creates a new Office 365 Group (i.e. Unified Group) with its backing Modern SharePoint Site
        /// </summary>
        /// <param name="displayName">The Display Name for the Office 365 Group</param>
        /// <param name="description">The Description for the Office 365 Group</param>
        /// <param name="mailNickname">The Mail Nickname for the Office 365 Group</param>
        /// <param name="owners">A list of UPNs for group owners, if any</param>
        /// <param name="members">A list of UPNs for group members, if any</param>
        /// <param name="groupLogo">The binary stream of the logo for the Office 365 Group</param>
        /// <param name="isPrivate">Defines whether the group will be private or public, optional with default false (i.e. public)</param>
        /// <param name="createTeam">Defines whether to create MS Teams team associated with the group</param>
        /// <param name="retryCount">Number of times to retry the request in case of throttling</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <returns>The just created Office 365 Group</returns>
        public async Task<UnifiedGroupEntity> CreateUnifiedGroup(
            string displayName,
            string description,
            string mailNickname,
            User[] owners = null,
            User[] members = null,
            Stream groupLogo = null,
            bool isPrivate = false,
            bool createTeam = false,
            int retryCount = 10,
            int delay = 500)
        {
            UnifiedGroupEntity group = null;

            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (string.IsNullOrEmpty(mailNickname))
            {
                throw new ArgumentNullException(nameof(mailNickname));
            }

            try
            {
                // Use a synchronous model to invoke the asynchronous process
                group = new UnifiedGroupEntity();

                // Prepare the group resource object
                var newGroup = new GroupExtended
                {
                    DisplayName = displayName,
                    Description = description,
                    MailNickname = mailNickname,
                    MailEnabled = true,
                    SecurityEnabled = false,
                    Visibility = isPrivate == true ? "Private" : "Public",
                    GroupTypes = new List<string> { "Unified" },
                };

                if (owners != null && owners.Any())
                {
                    newGroup.OwnersODataBind = owners.Select(u => string.Format("https://graph.microsoft.com/v1.0/users/{0}", u.Id)).ToArray();
                }

                if (members != null && members.Any())
                {
                    newGroup.MembersODataBind = members.Select(u => string.Format("https://graph.microsoft.com/v1.0/users/{0}", u.Id)).ToArray();
                }

                Microsoft.Graph.Group addedGroup = null;
                string modernSiteUrl = null;

                // Add the group to the collection of groups (if it does not exist)
                if (addedGroup == null)
                {
                    addedGroup = await GraphService.Groups.Request().AddAsync(newGroup);

                    if (addedGroup != null)
                    {
                        group = new UnifiedGroupEntity(addedGroup);


                        if (groupLogo != null)
                        {
                            await UpdatedUnifiedGroupLogo(groupLogo, addedGroup, retryCount, delay);
                        }

                        int driveRetryCount = retryCount;

                        while (driveRetryCount > 0 && string.IsNullOrEmpty(modernSiteUrl))
                        {
                            try
                            {
                                var documentUrl = await GetUnifiedGroupDocumentUrl(addedGroup.Id);
                                group.DocumentsUrl = documentUrl;
                                modernSiteUrl = GetUnifiedGroupSiteUrl(documentUrl);
                            }
                            catch
                            {
                                // Skip any exception and simply retry
                            }

                            // In case of failure retry up to 10 times, with 500ms delay in between
                            if (string.IsNullOrEmpty(modernSiteUrl))
                            {
                                await Task.Delay(delay * (retryCount - driveRetryCount));
                                driveRetryCount--;
                            }
                        }

                        group.SiteUrl = modernSiteUrl;
                    }
                }

                if (createTeam)
                {
                    await CreateTeam(group.GroupId);
                }
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                throw;
            }

            return group;
        }

        private async Task UpdatedUnifiedGroupLogo(Stream groupLogo, Group addedGroup, int retryCount = 10, int delay = 500)
        {
            var memGroupLogo = new MemoryStream();
            groupLogo.CopyTo(memGroupLogo);

            int imageRetryCount = retryCount;
            while (imageRetryCount > 0)
            {
                bool groupLogoUpdated = false;
                memGroupLogo.Position = 0;

                using var tempGroupLogo = new MemoryStream();
                memGroupLogo.CopyTo(tempGroupLogo);
                tempGroupLogo.Position = 0;

                try
                {
                    groupLogoUpdated = await UpdateUnifiedGroup(addedGroup.Id, groupLogo: tempGroupLogo);
                }
                catch
                {
                    // Skip any exception and simply retry
                }

                // In case of failure retry up to 10 times, with 500ms delay in between
                if (!groupLogoUpdated)
                {
                    // Pop up the delay for the group image
                    await Task.Delay(delay * (retryCount - imageRetryCount));
                    imageRetryCount--;
                }
                else
                {
                    break;
                }
            }
        }

        private async Task UpdateMembers(string[] members, Group targetGroup)
        {
            foreach (var m in members)
            {
                // Search for the user object
                var memberQuery = await GraphService.Users
                    .Request()
                    .Filter($"userPrincipalName eq '{Uri.EscapeDataString(m.Replace("'", "''"))}'")
                    .GetAsync();

                var member = memberQuery.FirstOrDefault();
                if (member != null)
                {
                    try
                    {
                        // And if any, add it to the collection of group's owners
                        await GraphService.Groups[targetGroup.Id].Members.References.Request().AddAsync(member);
                    }
                    catch (ServiceException ex)
                    {
                        if (ex.Error.Code == "Request_BadRequest" &&
                            ex.Error.Message.Contains("added object references already exist"))
                        {
                            // Skip any already existing member
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }

            // Remove any leftover member
            var fullListOfMembers = await GraphService.Groups[targetGroup.Id].Members.Request().Select("userPrincipalName, Id").GetAsync();
            var pageExists = true;
            while (pageExists)
            {
                foreach (var member in fullListOfMembers)
                {
                    var currentMemberPrincipalName = (member as Microsoft.Graph.User)?.UserPrincipalName;
                    if (!string.IsNullOrEmpty(currentMemberPrincipalName) &&
                        !members.Contains(currentMemberPrincipalName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            // If it is not in the list of current owners, just remove it
                            await GraphService.Groups[targetGroup.Id].Members[member.Id].Reference.Request().DeleteAsync();
                        }
                        catch (ServiceException ex)
                        {
                            if (ex.Error.Code == "Request_BadRequest")
                            {
                                // Skip any failing removal
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }

                if (fullListOfMembers.NextPageRequest != null)
                {
                    fullListOfMembers = await fullListOfMembers.NextPageRequest.GetAsync();
                }
                else
                {
                    pageExists = false;
                }
            }
        }

        /// <summary>
        /// Removes a member from a Unified Group
        /// </summary>
        /// <param name="removeMember">Azure AD User to remove</param>
        /// <param name="targetGroup">Office 365 Group (i.e. Unified Group)</param>
        /// <returns></returns>
        public async Task GroupRemoveMember(User removeMember, Group targetGroup)
        {
            // Remove any leftover member
            var fullListOfMembers = await GraphService.Groups[targetGroup.Id].Members.Request().Select("userPrincipalName, Id").GetAsync();
            var pageExists = true;
            while (pageExists)
            {
                foreach (var member in fullListOfMembers)
                {
                    var currentMemberPrincipalName = (member as Microsoft.Graph.User)?.UserPrincipalName;
                    if (!string.IsNullOrEmpty(currentMemberPrincipalName) && removeMember.Id == member.Id)
                    {
                        try
                        {
                            // If it is not in the list of current owners, just remove it
                            await GraphService.Groups[targetGroup.Id].Members[member.Id].Reference.Request().DeleteAsync();
                        }
                        catch (ServiceException ex)
                        {
                            if (ex.Error.Code == "Request_BadRequest")
                            {
                                // Skip any failing removal
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }

                if (fullListOfMembers.NextPageRequest != null)
                {
                    fullListOfMembers = await fullListOfMembers.NextPageRequest.GetAsync();
                }
                else
                {
                    pageExists = false;
                }
            }
        }

        private async Task UpdateOwners(string[] owners, Group targetGroup)
        {
            foreach (var o in owners)
            {
                // Search for the user object
                var ownerQuery = await GraphService.Users
                    .Request()
                    .Filter($"userPrincipalName eq '{Uri.EscapeDataString(o.Replace("'", "''"))}'")
                    .GetAsync();

                var owner = ownerQuery.FirstOrDefault();

                if (owner != null)
                {
                    try
                    {
                        // And if any, add it to the collection of group's owners
                        await GraphService.Groups[targetGroup.Id].Owners.References.Request().AddAsync(owner);
                    }
                    catch (ServiceException ex)
                    {
                        if (ex.Error.Code == "Request_BadRequest" &&
                            ex.Error.Message.Contains("added object references already exist"))
                        {
                            // Skip any already existing owner
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }

            // Remove any leftover owner
            var fullListOfOwners = await GraphService.Groups[targetGroup.Id].Owners.Request().Select("userPrincipalName, Id").GetAsync();
            var pageExists = true;

            while (pageExists)
            {
                foreach (var owner in fullListOfOwners)
                {
                    var currentOwnerPrincipalName = (owner as Microsoft.Graph.User)?.UserPrincipalName;
                    if (!string.IsNullOrEmpty(currentOwnerPrincipalName) &&
                        !owners.Contains(currentOwnerPrincipalName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            // If it is not in the list of current owners, just remove it
                            await GraphService.Groups[targetGroup.Id].Owners[owner.Id].Reference.Request().DeleteAsync();
                        }
                        catch (ServiceException ex)
                        {
                            if (ex.Error.Code == "Request_BadRequest")
                            {
                                // Skip any failing removal
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }

                if (fullListOfOwners.NextPageRequest != null)
                {
                    fullListOfOwners = await fullListOfOwners.NextPageRequest.GetAsync();
                }
                else
                {
                    pageExists = false;
                }
            }
        }

        /// <summary>
        /// Removes an owner from a Unified Group
        /// </summary>
        /// <param name="removeOwner">Azure AD User to remove</param>
        /// <param name="targetGroup">Office 365 Group (i.e. Unified Group)</param>
        /// <returns></returns>
        public async Task GroupRemoveOwner(User removeOwner, Group targetGroup)
        {
            // Remove any leftover owner
            var fullListOfOwners = await GraphService.Groups[targetGroup.Id].Owners.Request().Select("userPrincipalName, Id").GetAsync();
            var pageExists = true;

            while (pageExists)
            {
                foreach (var owner in fullListOfOwners)
                {
                    var currentOwnerPrincipalName = (owner as User)?.UserPrincipalName;
                    if (!string.IsNullOrEmpty(currentOwnerPrincipalName) && removeOwner.Id == owner.Id)
                    {
                        try
                        {
                            // If it is not in the list of current owners, just remove it
                            await GraphService.Groups[targetGroup.Id].Owners[owner.Id].Reference.Request().DeleteAsync();
                        }
                        catch (ServiceException ex)
                        {
                            if (ex.Error.Code == "Request_BadRequest")
                            {
                                // Skip any failing removal
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }

                if (fullListOfOwners.NextPageRequest != null)
                {
                    fullListOfOwners = await fullListOfOwners.NextPageRequest.GetAsync();
                }
                else
                {
                    pageExists = false;
                }
            }
        }

        /// <summary>
        /// Updates the logo, members or visibility state of an Office 365 Group
        /// </summary>
        /// <param name="groupId">The ID of the Office 365 Group</param>
        /// <param name="displayName">The Display Name for the Office 365 Group</param>
        /// <param name="description">The Description for the Office 365 Group</param>
        /// <param name="owners">A list of UPNs for group owners, if any, to be added to the site</param>
        /// <param name="members">A list of UPNs for group members, if any, to be added to the site</param>
        /// <param name="isPrivate">Defines whether the group will be private or public, optional with default false (i.e. public)</param>
        /// <param name="createTeam">Defines whether to create MS Teams team associated with the group</param>
        /// <param name="groupLogo">The binary stream of the logo for the Office 365 Group</param>
        /// <returns>Declares whether the Office 365 Group has been updated or not</returns>
        public async Task<bool> UpdateUnifiedGroup(
            string groupId,
            string displayName = null,
            string description = null,
            string[] owners = null,
            string[] members = null,
            Stream groupLogo = null,
            bool? isPrivate = null,
            bool createTeam = false)
        {
            try
            {
                var groupToUpdate = await GraphService.Groups[groupId].Request().GetAsync();

                // Workaround for the PATCH request, needed after update to Graph Library
                var clonedGroup = new Group
                {
                    Id = groupToUpdate.Id
                };

                #region Logic to update the group DisplayName and Description

                var updateGroup = false;
                var groupUpdated = false;

                // Check if we have to update the DisplayName
                if (!string.IsNullOrEmpty(displayName) && groupToUpdate.DisplayName != displayName)
                {
                    clonedGroup.DisplayName = displayName;
                    updateGroup = true;
                }

                // Check if we have to update the Description
                if (!string.IsNullOrEmpty(description) && groupToUpdate.Description != description)
                {
                    clonedGroup.Description = description;
                    updateGroup = true;
                }

                // Check if visibility has changed for the Group
                bool existingIsPrivate = groupToUpdate.Visibility == "Private";
                if (isPrivate.HasValue && existingIsPrivate != isPrivate)
                {
                    clonedGroup.Visibility = isPrivate == true ? "Private" : "Public";
                    updateGroup = true;
                }

                // Check if we need to update owners
                if (owners != null && owners.Length > 0)
                {
                    // For each and every owner
                    await UpdateOwners(owners, groupToUpdate);
                    updateGroup = true;
                }

                // Check if we need to update members
                if (members != null && members.Length > 0)
                {
                    // For each and every owner
                    await UpdateMembers(members, groupToUpdate);
                    updateGroup = true;
                }

                if (createTeam)
                {
                    await CreateTeam(groupId);
                    updateGroup = true;
                }

                // If the Group has to be updated, just do it
                if (updateGroup)
                {
                    var updatedGroup = await GraphService.Groups[groupId]
                        .Request()
                        .UpdateAsync(clonedGroup);

                    groupUpdated = true;
                }

                #endregion

                #region Logic to update the group Logo

                var logoUpdated = false;

                if (groupLogo != null)
                {
                    await GraphService.Groups[groupId].Photo.Content.Request().PutAsync(groupLogo);
                    logoUpdated = true;
                }

                #endregion

                // If any of the previous update actions has been completed
                return (groupUpdated || logoUpdated);
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                throw;
            }
        }

        /// <summary>
        /// Updates the subscription state of an Office 365 Group
        /// </summary>
        /// <remarks>
        /// KNOWN ISSUE: Must use Delegate or User Credentials for this to work correctly
        /// https://docs.microsoft.com/en-us/graph/known-issues#groups
        /// </remarks>
        /// <param name="groupId">The ID of the Office 365 Group</param>
        /// <param name="autoSubscribeNewMembers">All new members should be auto subscribed to the O365 group email</param>
        /// <returns>Declares whether the Office 365 Group has been updated or not</returns>
        public async Task<bool> UpdateUnifiedGroupSubscription(
            string groupId,
            bool autoSubscribeNewMembers = false)
        {
            try
            {
                var groupToUpdate = await GraphService.Groups[groupId]
                    .Request().Select("AutoSubscribeNewMembers, Id")
                    .GetAsync();

                // Workaround for the PATCH request, needed after update to Graph Library
                var clonedGroup = new Group
                {
                    Id = groupToUpdate.Id
                };

                #region Logic to update the group AutoSubscribeNewMembers

                var updateGroup = false;
                var groupUpdated = false;


                // Check if we auto subscribe is set
                if (groupToUpdate.AutoSubscribeNewMembers != autoSubscribeNewMembers)
                {
                    clonedGroup.AutoSubscribeNewMembers = autoSubscribeNewMembers;
                    updateGroup = true;
                }

                // If the Group has to be updated, just do it
                if (updateGroup)
                {
                    var updatedGroup = await GraphService.Groups[groupId]
                        .Request()
                        .UpdateAsync(clonedGroup);

                    groupUpdated = true;
                }

                #endregion

                // If any of the previous update actions has been completed
                return (groupUpdated);
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                throw;
            }
        }

        /// <summary>
        /// Creates a new Office 365 Group (i.e. Unified Group) with its backing Modern SharePoint Site
        /// </summary>
        /// <param name="displayName">The Display Name for the Office 365 Group</param>
        /// <param name="description">The Description for the Office 365 Group</param>
        /// <param name="mailNickname">The Mail Nickname for the Office 365 Group</param>
        /// <param name="owners">A list of UPNs for group owners, if any</param>
        /// <param name="members">A list of UPNs for group members, if any</param>
        /// <param name="groupLogoPath">The path of the logo for the Office 365 Group</param>
        /// <param name="isPrivate">Defines whether the group will be private or public, optional with default false (i.e. public)</param>
        /// <param name="createTeam">Defines whether to create MS Teams team associated with the group</param>
        /// <param name="retryCount">Number of times to retry the request in case of throttling</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <returns>The just created Office 365 Group</returns>
        public async Task<UnifiedGroupEntity> CreateUnifiedGroup(
            string displayName,
            string description,
            string mailNickname,
            User[] owners = null,
            User[] members = null,
            string groupLogoPath = null,
            bool isPrivate = false,
            bool createTeam = false,
            int retryCount = 10,
            int delay = 500)
        {
            if (!string.IsNullOrEmpty(groupLogoPath) && !System.IO.File.Exists(groupLogoPath))
            {
                throw new FileNotFoundException(CoreResources.GraphExtensions_GroupLogoFileDoesNotExist, groupLogoPath);
            }
            else if (!string.IsNullOrEmpty(groupLogoPath))
            {
                using var groupLogoStream = new FileStream(groupLogoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return await CreateUnifiedGroup(displayName, description,
                    mailNickname, owners, members,
                    groupLogo: groupLogoStream, isPrivate: isPrivate,
                    createTeam: createTeam, retryCount: retryCount, delay: delay);
            }
            else
            {
                return await CreateUnifiedGroup(displayName, description,
                    mailNickname, owners, members,
                    groupLogo: null, isPrivate: isPrivate,
                    createTeam: createTeam, retryCount: retryCount, delay: delay);
            }
        }

        /// <summary>
        /// Creates a new Office 365 Group (i.e. Unified Group) with its backing Modern SharePoint Site
        /// </summary>
        /// <param name="displayName">The Display Name for the Office 365 Group</param>
        /// <param name="description">The Description for the Office 365 Group</param>
        /// <param name="mailNickname">The Mail Nickname for the Office 365 Group</param>
        /// <param name="owners">A list of UPNs for group owners, if any</param>
        /// <param name="members">A list of UPNs for group members, if any</param>
        /// <param name="isPrivate">Defines whether the group will be private or public, optional with default false (i.e. public)</param>
        /// <param name="createTeam">Defines whether to create MS Teams team associated with the group</param>
        /// <param name="retryCount">Number of times to retry the request in case of throttling</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <returns>The just created Office 365 Group</returns>
        public async Task<UnifiedGroupEntity> CreateUnifiedGroup(
            string displayName,
            string description,
            string mailNickname,
            User[] owners = null,
            User[] members = null,
            bool isPrivate = false,
            bool createTeam = false,
            int retryCount = 10,
            int delay = 500)
        => await CreateUnifiedGroup(displayName, description, mailNickname, owners, members, groupLogo: null, isPrivate: isPrivate, createTeam: createTeam, retryCount: retryCount, delay: delay);

        /// <summary>
        /// Deletes an Office 365 Group (i.e. Unified Group)
        /// </summary>
        /// <param name="groupId">The ID of the Office 365 Group</param>
        public async Task DeleteUnifiedGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentNullException(nameof(groupId));
            }

            try
            {
                await GraphService.Groups[groupId].Request().DeleteAsync();
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                throw;
            }
        }

        /// <summary>
        /// Get an Office 365 Group (i.e. Unified Group) by Id
        /// </summary>
        /// <param name="groupId">The ID of the Office 365 Group</param>
        /// <param name="includeSite">Defines whether to return details about the Modern SharePoint Site backing the group. Default is true.</param>
        /// <param name="includeClassification">Defines whether to return classification value of the unified group. Default is false.</param>
        /// <param name="includeHasTeam">Defines whether to check for each unified group if it has a Microsoft Team provisioned for it. Default is false.</param>
        public async Task<UnifiedGroupEntity> GetUnifiedGroup(string groupId, bool includeSite = true, bool includeClassification = false, bool includeHasTeam = false)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentNullException(nameof(groupId));
            }

            try
            {
                UnifiedGroupEntity group = null;

                var g = await GraphService.Groups[groupId].Request().GetAsync();
                group = new UnifiedGroupEntity(g);

                if (includeSite)
                {
                    try
                    {
                        group.DocumentsUrl = await GetUnifiedGroupDocumentUrl(groupId);
                        group.SiteUrl = GetUnifiedGroupSiteUrl(group.DocumentsUrl);
                    }
                    catch (ServiceException e)
                    {
                        group.SiteUrl = e.Error.Message;
                    }
                }

                if (includeClassification)
                {
                    group.Classification = g.Classification;
                }

                if (includeHasTeam)
                {
                    group.HasTeam = await HasTeamsTeam(group.GroupId);
                }

                return (group);
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                throw;
            }
        }

        /// <summary>
        /// Returns all the Office 365 Groups in the current Tenant based on a startIndex. IncludeSite adds additional properties about the Modern SharePoint Site backing the group
        /// </summary>
        /// <param name="displayName">The DisplayName of the Office 365 Group</param>
        /// <param name="mailNickname">The MailNickname of the Office 365 Group</param>
        /// <param name="startsWith">Enables a starts with displayname text</param>
        /// <param name="startIndex">Not relevant anymore</param>
        /// <param name="includeSite">Defines whether to return details about the Modern SharePoint Site backing the group. Default is true.</param>
        /// <param name="includeClassification">Defines whether or not to return details about the Modern Site classification value.</param>
        /// <param name="includeHasTeam">Defines whether to check for each unified group if it has a Microsoft Team provisioned for it. Default is false.</param>
        /// <param name="includeOwner">(optional) if true then enumerate ownership for the team</param>
        /// <param name="includeMembership">(optional) if true then enumerate membership for the team</param>
        /// <param name="includeCreatedOnBehalf">(optional) if true then call createdOnBehalf endpoint</param>
        /// <returns>An IList of SiteEntity objects</returns>
        public async Task<List<UnifiedGroupEntity>> ListUnifiedGroups(
            string displayName = null,
            string mailNickname = null,
            string startsWith = null,
            int startIndex = 0,
            bool includeSite = false,
            bool includeClassification = false,
            bool includeHasTeam = false,
            bool includeOwner = false,
            bool includeMembership = false,
            bool includeCreatedOnBehalf = false)
        {
            try
            {
                var groups = new List<UnifiedGroupEntity>();

                // Apply the DisplayName filter, if any
                var displayNameFilter = !string.IsNullOrEmpty(displayName) ? $" and (DisplayName eq '{Uri.EscapeDataString(displayName.Replace("'", "''"))}')" : string.Empty;
                var mailNicknameFilter = !string.IsNullOrEmpty(mailNickname) ? $" and (MailNickname eq '{Uri.EscapeDataString(mailNickname.Replace("'", "''"))}')" : string.Empty;
                var startsWithFilter = !string.IsNullOrEmpty(startsWith) ? $" and (startswith(displayName,'{Uri.EscapeDataString(startsWith.Replace("'", "''"))}'))" : string.Empty;

                var pagedGroups = await GraphService.Groups
                    .Request()
                    .Filter($"groupTypes/any(grp: grp eq 'Unified'){displayNameFilter}{mailNicknameFilter}{startsWithFilter}")
                    .GetAsync();

                int pageCount = 0;
                int currentIndex = 0;
                bool pageExists = true;

                while (pageExists)
                {
                    pageCount++;

                    foreach (var g in pagedGroups)
                    {
                        currentIndex++;

                        if (currentIndex >= startIndex)
                        {
                            var group = new UnifiedGroupEntity(g);
                            if (g.CreatedDateTime.HasValue == true)
                            {
                                group.Created = g.CreatedDateTime.Value.LocalDateTime;
                            }
                            TraceLogger.Information($"Group ID:{g.Id} | Display Name:{g.DisplayName} | Current Index:{currentIndex} | Page Count:{pageCount}");

                            if (includeSite)
                            {
                                try
                                {
                                    group.DocumentsUrl = await GetUnifiedGroupDocumentUrl(g.Id);
                                    group.SiteUrl = GetUnifiedGroupSiteUrl(group.DocumentsUrl);
                                }
                                catch (ServiceException e)
                                {
                                    group.SiteUrl = e.Error.Message;
                                }
                            }

                            if (includeClassification)
                            {
                                group.Classification = g.Classification;
                            }

                            if (includeOwner)
                            {
                                var groupOwners = await GetUnifiedGroupOwners(group);
                                if (groupOwners?.Any() == true)
                                {
                                    bool isPrimaryOwner = true;
                                    foreach (var item in groupOwners)
                                    {
                                        if (isPrimaryOwner)
                                        {
                                            group.PrimaryOwner = item;
                                            isPrimaryOwner = false;
                                        }
                                        group.GroupOwners.Add(item);
                                    }
                                }
                            }

                            if (includeMembership)
                            {
                                List<UnifiedGroupUser> groupMembers = await GetUnifiedGroupMembers(group);
                                if (groupMembers != null)
                                {
                                    foreach (var item in groupMembers)
                                    {
                                        group.GroupMembers.Add(item);
                                    }
                                }
                            }

                            if (includeHasTeam)
                            {
                                try
                                {
                                    var teamDetails = GraphService.Groups[group.GroupId].Team.Request().GetAsync().GetAwaiter().GetResult();
                                    group.HasTeam = true;
                                    group.TeamDisplayName = teamDetails.AdditionalData["displayName"]?.ToString();
                                    group.TeamDescription = teamDetails.AdditionalData["description"]?.ToString();
                                    group.TeamInternalId = teamDetails.AdditionalData["internalId"]?.ToString();
                                    group.TeamClassification = teamDetails.AdditionalData["classification"]?.ToString();
                                    group.TeamSpecialization = teamDetails.AdditionalData["specialization"]?.ToString();
                                    group.TeamVisibility = teamDetails.AdditionalData["visibility"]?.ToString();
                                    group.TeamDiscoverySettings = teamDetails.AdditionalData["discoverySettings"]?.ToString();
                                    group.TeamResponseHeaders = teamDetails.AdditionalData["responseHeaders"]?.ToString();
                                    group.TeamStatusCode = teamDetails.AdditionalData["statusCode"]?.ToString();
                                    group.TeamWebUrl = teamDetails.WebUrl;
                                    group.TeamIsArchived = teamDetails.IsArchived;
                                }
                                catch
                                {
                                    TraceLogger.Information($"Could not find a Team for the Group:{group.GroupId}");
                                    group.HasTeam = false;
                                }
                            }

                            if (includeCreatedOnBehalf)
                            {
                                string createdOnBehalfOf = await GroupCreatedOnBehalfOf(group.GroupId);
                                if (!string.IsNullOrEmpty(createdOnBehalfOf))
                                {
                                    group.PrimaryOwner = new UnifiedGroupUser { UserPrincipalName = createdOnBehalfOf };
                                    group.IsSiteProvisioned = false;
                                }
                            }

                            groups.Add(group);
                            TraceLogger.Debug($"The Group, {group.DisplayName}, was added.");
                        }
                    }

                    if (pagedGroups.NextPageRequest != null)
                    {
                        pagedGroups = await pagedGroups.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        pageExists = false;
                    }
                }

                return (groups);
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                throw;
            }
        }

        /// <summary>
        /// Returns all the Members of an Office 365 group.
        /// </summary>
        /// <param name="group">The Office 365 group object of type UnifiedGroupEntity</param>
        /// <returns>Members of an Office 365 group as a list of UnifiedGroupUser entity</returns>
        public async Task<List<UnifiedGroupUser>> GetUnifiedGroupMembers(UnifiedGroupEntity group)
        {
            List<UnifiedGroupUser> unifiedGroupUsers = null;
            List<User> unifiedGroupGraphUsers = null;
            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            try
            {
                // Get the members of an Office 365 group.
                IGroupMembersCollectionWithReferencesPage groupUsers = await GraphService.Groups[group.GroupId].Members.Request().GetAsync();
                if (groupUsers.CurrentPage != null && groupUsers.CurrentPage.Count > 0)
                {
                    unifiedGroupGraphUsers = new List<User>();

                    GenerateGraphUserCollection(groupUsers.CurrentPage, unifiedGroupGraphUsers);
                }

                // Retrieve users when the results are paged.
                while (groupUsers.NextPageRequest != null)
                {
                    groupUsers = groupUsers.NextPageRequest.GetAsync().GetAwaiter().GetResult();
                    if (groupUsers.CurrentPage != null && groupUsers.CurrentPage.Count > 0)
                    {
                        GenerateGraphUserCollection(groupUsers.CurrentPage, unifiedGroupGraphUsers);
                    }
                }

                // Create the collection of type OfficeDevPnP 'UnifiedGroupUser' after all users are retrieved, including paged data.
                if (unifiedGroupGraphUsers != null && unifiedGroupGraphUsers.Count > 0)
                {
                    unifiedGroupUsers = new List<UnifiedGroupUser>();
                    foreach (User usr in unifiedGroupGraphUsers)
                    {
                        UnifiedGroupUser groupUser = new UnifiedGroupUser
                        {
                            UserPrincipalName = usr.UserPrincipalName ?? string.Empty,
                            DisplayName = usr.DisplayName ?? string.Empty
                        };
                        unifiedGroupUsers.Add(groupUser);
                    }
                }
                return unifiedGroupUsers;
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                throw;
            }
        }

        /// <summary>
        /// Returns all the Owners of an Office 365 group.
        /// </summary>
        /// <param name="group">The Office 365 group object of type UnifiedGroupEntity</param>
        /// <returns>Owners of an Office 365 group as a list of UnifiedGroupUser entity</returns>
        public async Task<List<UnifiedGroupUser>> GetUnifiedGroupOwners(UnifiedGroupEntity group)
        {
            List<UnifiedGroupUser> unifiedGroupUsers = null;
            List<User> unifiedGroupGraphUsers = null;
            try
            {
                // Get the owners of an Office 365 group.
                IGroupOwnersCollectionWithReferencesPage groupUsers = await GraphService.Groups[group.GroupId].Owners.Request().GetAsync();
                if (groupUsers.CurrentPage != null && groupUsers.CurrentPage.Count > 0)
                {
                    unifiedGroupGraphUsers = new List<User>();
                    GenerateGraphUserCollection(groupUsers.CurrentPage, unifiedGroupGraphUsers);
                }

                // Retrieve users when the results are paged.
                while (groupUsers.NextPageRequest != null)
                {
                    groupUsers = groupUsers.NextPageRequest.GetAsync().GetAwaiter().GetResult();
                    if (groupUsers.CurrentPage != null && groupUsers.CurrentPage.Count > 0)
                    {
                        GenerateGraphUserCollection(groupUsers.CurrentPage, unifiedGroupGraphUsers);
                    }
                }

                // Create the collection of type OfficeDevPnP 'UnifiedGroupUser' after all users are retrieved, including paged data.
                if (unifiedGroupGraphUsers != null && unifiedGroupGraphUsers.Count > 0)
                {
                    unifiedGroupUsers = new List<UnifiedGroupUser>();
                    foreach (User usr in unifiedGroupGraphUsers)
                    {
                        UnifiedGroupUser groupUser = new UnifiedGroupUser
                        {
                            UserPrincipalName = usr.UserPrincipalName ?? string.Empty,
                            DisplayName = usr.DisplayName ?? string.Empty
                        };
                        unifiedGroupUsers.Add(groupUser);
                    }
                }
                return unifiedGroupUsers;
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
            }
            return unifiedGroupUsers;
        }


        /// <summary>
        /// Returns all the Owners of an Office 365 group.
        /// </summary>
        /// <param name="group">The Office 365 group object of type UnifiedGroupEntity</param>
        /// <returns>Owners of an Office 365 group as a list of UnifiedGroupUser entity</returns>
        public async Task<List<UnifiedGroupUser>> GetUnifiedGroupTeam(UnifiedGroupEntity group)
        {
            List<UnifiedGroupUser> unifiedGroupUsers = null;
            List<User> unifiedGroupGraphUsers = null;
            IGroupOwnersCollectionWithReferencesPage groupUsers = null;

            try
            {
                // Get the owners of an Office 365 group.
                var unifiedGroupTeam = await GraphService.Groups[group.GroupId].Team.Request().GetAsync();

                // unifiedGroupTeam.AdditionalData
                if (groupUsers.CurrentPage != null && groupUsers.CurrentPage.Count > 0)
                {
                    unifiedGroupGraphUsers = new List<User>();
                    GenerateGraphUserCollection(groupUsers.CurrentPage, unifiedGroupGraphUsers);
                }

                // Retrieve users when the results are paged.
                while (groupUsers.NextPageRequest != null)
                {
                    groupUsers = groupUsers.NextPageRequest.GetAsync().GetAwaiter().GetResult();
                    if (groupUsers.CurrentPage != null && groupUsers.CurrentPage.Count > 0)
                    {
                        GenerateGraphUserCollection(groupUsers.CurrentPage, unifiedGroupGraphUsers);
                    }
                }

                // Create the collection of type OfficeDevPnP 'UnifiedGroupUser' after all users are retrieved, including paged data.
                if (unifiedGroupGraphUsers != null && unifiedGroupGraphUsers.Count > 0)
                {
                    unifiedGroupUsers = new List<UnifiedGroupUser>();
                    foreach (User usr in unifiedGroupGraphUsers)
                    {
                        UnifiedGroupUser groupUser = new UnifiedGroupUser
                        {
                            UserPrincipalName = usr.UserPrincipalName ?? string.Empty,
                            DisplayName = usr.DisplayName ?? string.Empty
                        };
                        unifiedGroupUsers.Add(groupUser);
                    }
                }
                return unifiedGroupUsers;
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
            }

            return unifiedGroupUsers;
        }

        /// <summary>
        /// Helper method. Generates a collection of Microsoft.Graph.User entity from directory objects.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="unifiedGroupGraphUsers"></param>
        /// <returns>Returns a collection of Microsoft.Graph.User entity</returns>
        private List<User> GenerateGraphUserCollection(IList<DirectoryObject> page, List<User> unifiedGroupGraphUsers)
        {
            // Create a collection of Microsoft.Graph.User type
            foreach (User usr in page)
            {
                if (usr != null)
                {
                    unifiedGroupGraphUsers.Add(usr);
                }
            }

            return unifiedGroupGraphUsers;
        }

        /// <summary>
        /// Helper method. Generates a collection of Microsoft.Graph.User entity from string array
        /// </summary>
        /// <param name="userPrincipalName">User Principal Name as lookup into Azure Active Directory</param>
        /// <returns></returns>
        public async Task<User> GetUserId(string userPrincipalName)
        {
            if (string.IsNullOrEmpty(userPrincipalName))
            {
                throw new ArgumentNullException(nameof(userPrincipalName));
            }

            try
            {
                // Search for the user object
                IGraphServiceUsersCollectionPage userQuery = await GraphService.Users
                                    .Request()
                                    .Select("Id")
                                    .Filter($"userPrincipalName eq '{Uri.EscapeDataString(userPrincipalName.Replace("'", "''"))}'")
                                    .GetAsync();

                User user = userQuery.FirstOrDefault();
                return user;
            }
            catch (ServiceException ex)
            {
                // skip, group provisioning shouldnt stop because of error in user object
                TraceLogger.Error(ex, $"ServiceException {ex.Message} occurred for {userPrincipalName}");
            }
            return null;
        }

        /// <summary>
        /// Does this group have a Teams team?
        /// </summary>
        /// <param name="groupId">Id of the group to check</param>
        /// <returns>True if there's a Teams linked to this group</returns>
        public async Task<bool> HasTeamsTeam(string groupId, bool continueOnError = false)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentNullException(nameof(groupId));
            }

            bool hasTeamsTeam = false;

            try
            {
                var accessToken = await TokenCache.AccessTokenAsync("");
                groupId = groupId.ToLower();
                string getGroupsWithATeamsTeam = $"{GraphHttpHelper.MicrosoftGraphBetaBaseUri}groups?$filter=resourceProvisioningOptions/Any(x:x eq 'Team')&select=id,resourceProvisioningOptions";

                var getGroupResult = GraphHttpHelper.MakeGetRequestForString(
                    getGroupsWithATeamsTeam,
                    accessToken: accessToken);

                JObject groupObject = JObject.Parse(getGroupResult);

                foreach (var item in groupObject["value"])
                {
                    if (item["id"].ToString().Equals(groupId, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (ServiceException ex)
            {
                if (continueOnError)
                {
                    TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                }
                else
                {
                    TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                    throw;
                }
            }

            return hasTeamsTeam;
        }

        /// <summary>
        /// Who Created the Group?
        /// </summary>
        /// <param name="groupId">Id of the group to check</param>
        /// <returns>The name of the Group Creator</returns>
        public async Task<string> GroupCreatedOnBehalfOf(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentNullException(nameof(groupId));
            }

            try
            {
                var accessToken = await TokenCache.AccessTokenAsync("");
                groupId = groupId.ToLower();
                string queryCreatedOnBehalfOf = $"{GraphHttpHelper.MicrosoftGraphV1BaseUri}groups/{groupId}/createdOnBehalfOf";

                var getCreatedOnBehalfOf = GraphHttpHelper.MakeGetRequestForString(
                    queryCreatedOnBehalfOf,
                    accessToken: accessToken,
                    continueOnError: true);

                if (getCreatedOnBehalfOf == null)
                    return string.Empty;

                JObject createdOnBehalfOfObject = JObject.Parse(getCreatedOnBehalfOf);

                if (createdOnBehalfOfObject["userPrincipalName"] != null)
                    return createdOnBehalfOfObject["userPrincipalName"].ToString();
                return string.Empty;
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
            }

            return string.Empty;
        }

        /// <summary>
        /// Creates a team associated with an Office 365 group
        /// </summary>
        /// <param name="groupId">The ID of the Office 365 Group</param>
        /// <returns></returns>
        public async Task CreateTeam(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentNullException(nameof(groupId));
            }

            var createTeamEndPoint = GraphHttpHelper.MicrosoftGraphV1BaseUri + $"groups/{groupId}/team";
            try
            {
                var accessToken = await TokenCache.AccessTokenAsync("");
                GraphHttpHelper.MakePutRequest(createTeamEndPoint, new { }, "application/json", accessToken);
            }
            catch (ServiceException ex)
            {
                TraceLogger.Error(ex, string.Format(CoreResources.GraphExtensions_ErrorOccured, ex.Error.Message));
                throw;
            }
        }

        /// <summary>
        /// Gets one deleted unified group based on its ID.
        /// </summary>
        /// <param name="groupId">The ID of the deleted group.</param>
        /// <returns>The unified group object of the deleted group that matches the provided ID.</returns>
        public async Task<UnifiedGroupEntity> GetDeletedUnifiedGroup(string groupId)
        {
            try
            {
                var accessToken = await TokenCache.AccessTokenAsync("");
                var response = GraphHttpHelper.MakeGetRequestForString($"{GraphHttpHelper.GraphBaseUrl}/beta/directory/deleteditems/microsoft.graph.group/{groupId}", accessToken);

                var group = JToken.Parse(response);

                var deletedGroup = new UnifiedGroupEntity
                {
                    GroupId = group["id"].ToString(),
                    Classification = group["classification"].ToString(),
                    Description = group["description"].ToString(),
                    DisplayName = group["displayName"].ToString(),
                    Mail = group["mail"].ToString(),
                    MailNickname = group["mailNickname"].ToString(),
                    Visibility = group["visibility"].ToString()
                };

                return deletedGroup;
            }
            catch (Exception e)
            {
                TraceLogger.Error(e, string.Format(CoreResources.GraphExtensions_ErrorOccured, e.Message));
                throw;
            }
        }

        /// <summary>
        ///  Lists deleted unified groups.
        /// </summary>
        /// <returns>A list of unified group objects for the deleted groups.</returns>
        public async Task<List<UnifiedGroupEntity>> ListDeletedUnifiedGroups()
            => await ListDeletedUnifiedGroups(null, null);

        private async Task<List<UnifiedGroupEntity>> ListDeletedUnifiedGroups(List<UnifiedGroupEntity> deletedGroups, string nextPageUrl)
        {
            try
            {
                var accessToken = await TokenCache.AccessTokenAsync("");

                if (deletedGroups == null) deletedGroups = new List<UnifiedGroupEntity>();

                var requestUrl = nextPageUrl ?? $"{GraphHttpHelper.GraphBaseUrl}/beta/directory/deleteditems/microsoft.graph.group?filter=groupTypes/Any(x:x eq 'Unified')";
                var response = JToken.Parse(GraphHttpHelper.MakeGetRequestForString(requestUrl, accessToken));

                var groups = response["value"];

                foreach (var group in groups)
                {
                    var deletedGroup = new UnifiedGroupEntity
                    {
                        GroupId = group["id"].ToString(),
                        Classification = group["classification"].ToString(),
                        Description = group["description"].ToString(),
                        DisplayName = group["displayName"].ToString(),
                        Mail = group["mail"].ToString(),
                        MailNickname = group["mailNickname"].ToString(),
                        Visibility = group["visibility"].ToString()
                    };

                    deletedGroups.Add(deletedGroup);
                }

                // has paging?
                return response["@odata.nextLink"] != null ? await ListDeletedUnifiedGroups(deletedGroups, response["@odata.nextLink"].ToString()) : deletedGroups;
            }
            catch (Exception e)
            {
                TraceLogger.Error(e, string.Format(CoreResources.GraphExtensions_ErrorOccured, e.Message));
                throw;
            }
        }

        /// <summary>
        /// Restores one deleted unified group based on its ID.
        /// </summary>
        /// <param name="groupId">The ID of the deleted group.</param>
        /// <returns></returns>
        public async Task RestoreDeletedUnifiedGroup(string groupId)
        {
            try
            {
                var accessToken = await TokenCache.AccessTokenAsync("");

                GraphHttpHelper.MakePostRequest($"{GraphHttpHelper.GraphBaseUrl}/beta/directory/deleteditems/{groupId}/restore", contentType: "application/json", accessToken: accessToken);
            }
            catch (Exception e)
            {
                TraceLogger.Error(e, string.Format(CoreResources.GraphExtensions_ErrorOccured, e.Message));
                throw;
            }
        }

        /// <summary>
        /// Permanently deletes one deleted unified group based on its ID.
        /// </summary>
        /// <param name="groupId">The ID of the deleted group.</param>
        /// <returns></returns>
        public async Task PermanentlyDeleteUnifiedGroup(string groupId)
        {
            try
            {
                var accessToken = await TokenCache.AccessTokenAsync("");

                GraphHttpHelper.MakeDeleteRequest($"{GraphHttpHelper.GraphBaseUrl}/beta/directory/deleteditems/{groupId}", accessToken);
            }
            catch (Exception e)
            {
                TraceLogger.Error(e, string.Format(CoreResources.GraphExtensions_ErrorOccured, e.Message));
                throw;
            }
        }
    }
}
