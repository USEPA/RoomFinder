using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.Governance
{
    public class EPASearchGroupModels
    {
        public EPASearchGroupModels()
        {
            AssociatedOwnerGroupMember = new List<string>();
        }

        public bool IsSiteCollection { get; set; }

        public string URL { get; set; }

        public string TrailingURL { get; set; }

        public string Title { get; set; }

        public string Region { get; set; }

        public string SiteType { get; set; }

        public bool HasUniquePerms { get; set; }

        public List<string> ActivityLog { get; set; }

        public string SiteOwner { get; set; }

        public bool HasEveryoneFolder { get; set; }

        public List<OfficeDevPnP.Core.Entities.UserEntity> Admins { get; set; }

        public List<string> AssociatedOwnerGroupMember { get; set; }

        public List<string> AssociatedMemberGroupMember { get; set; }

        public List<string> AssociatedVisitorGroupMember { get; set; }

        public List<SPGroupPrincipalModel> Groups { get; set; }

        public List<SPGroupPrincipalModel> GroupUsers { get; set; }

        public List<SPPrincipalModel> Users { get; set; }

        public List<EPAListDefinition> Lists { get; set; }

        public List<EPAListSkippedDefinition> SkippedLists { get; set; }
    }


    public class GroupSiteUserModel
    {
        public string WebUrl { get; set; }

        public string UserType { get; set; }

        public string UserTitle { get; set; }

        public string UserLoginName { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3}", WebUrl, UserType, UserTitle, UserLoginName);
        }
    }

    public class GroupSiteGroupModel
    {
        public string WebUrl { get; set; }

        public string GroupType { get; set; }

        public string GroupTitle { get; set; }

        public string GroupUserTitle { get; set; }

        public string GroupUserLoginName { get; set; }


        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4}", WebUrl, GroupType, GroupTitle, GroupUserTitle, GroupUserLoginName);
        }
    }

    public class GroupSiteRoleAssignmentModel
    {
        public string WebUrl { get; set; }

        public string RoleType { get; set; }

        public string ListTitle { get; set; }

        public string RoleMemberTitle { get; set; }

        public string RoleMemberLoginName { get; set; }


        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4}", WebUrl, RoleType, ListTitle, RoleMemberTitle, RoleMemberLoginName);
        }
    }

    public class GroupSiteRoleAssignmentItemModel
    {
        public string WebUrl { get; set; }

        public string RoleType { get; set; }

        public int ItemId { get; set; }

        public string RoleMemberTitle { get; set; }

        public string RoleMemberLoginName { get; set; }


        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4}", WebUrl, RoleType, ItemId, RoleMemberTitle, RoleMemberLoginName);
        }
    }

    public class EPAListSkippedDefinition : SPUniqueId
    {
        public EPAListSkippedDefinition() { }

        public DateTime? Created { get; set; }

        public string CreatedBy { get; set; }

        public int? ItemCount { get; set; }

        public bool Hidden { get; set; }

        public ListTemplateType ListTemplate { get; set; }

        public string ListTemplateName
        {
            get
            {
                if (ListTemplate == ListTemplateType.InvalidType) return string.Empty;
                return ListTemplate.ToString("f");
            }
        }

        public string ListName { get; set; }

        public string ServerRelativeUrl { get; set; }
    }

    public class EPAListDefinition : SPUniqueId
    {
        public EPAListDefinition() { }

        public IList<SPPrincipalModel> RoleBindings { get; set; }

        public List<EPAListItemDefinition> ListItems { get; set; }

        public DateTime? LastItemUserModifiedDate { get; set; }

        public DateTime? LastItemModifiedDate { get; set; }

        public DateTime? Created { get; set; }

        public SPPrincipalModel CreatedBy { get; set; }

        public int? ItemCount { get; set; }

        public bool Hidden { get; set; }

        public bool IsSystemList { get; set; }

        public bool IsPrivate { get; set; }

        public bool IsApplicationList { get; set; }

        public bool IsCatalog { get; set; }

        public bool IsSiteAssetsLibrary { get; set; }

        public bool Versioning { get; set; }

        public int BaseTemplate { get; set; }

        public ListTemplateType ListTemplate { get; set; }

        public string ListName { get; set; }

        public string ServerRelativeUrl { get; set; }
    }

    public class EPAListItemDefinition : SPId
    {
        public EPAListItemDefinition() { }

        public string Title { get; set; }

        public DateTime? Created { get; set; }

        public SPPrincipalUserDefinition CreatedBy { get; set; }

        public DateTime? Modified { get; set; }

        public SPPrincipalUserDefinition ModifiedBy { get; set; }

        public string FileRef { get; set; }

        public string FileDirRef { get; set; }

        public IList<SPPrincipalModel> RoleBindings { get; set; }
    }
}
