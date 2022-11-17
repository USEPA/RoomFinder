using EPA.Office365;
using EPA.SharePoint.SysConsole.Models.Governance;
using OfficeDevPnP.Core.Entities;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Framework.Governance
{
    public class SiteTemplateEntity
    {
        public SiteTemplateEntity()
        {
            additionalOwners = new List<string>();
            additionalMembers = new List<string>();
            additionalVisitors = new List<string>();
            additionalAdministrators = new List<string>();
            EveryoneGroupTenantId = ConstantsTenant.EveryoneGroupTenantId;
        }

        /// <summary>
        /// Parent Web URL
        /// </summary>
        public string SiteCollectionUrl { get; set; }

        public Model_SiteRequestItem SiteRequest { get; set; }

        public string SiteOwnerEmail { get; set; }

        public SiteEntity siteEntity { get; set; }

        /// <summary>
        /// Site Collection administrators
        /// </summary>
        public List<string> additionalAdministrators { get; set; }

        /// <summary>
        /// Users to be associated with the Web Owner group
        /// </summary>
        public List<string> additionalOwners { get; set; }

        /// <summary>
        /// Users to be associated with the Members group
        /// </summary>
        public List<string> additionalMembers { get; set; }

        /// <summary>
        /// Users to be associated with the Visitors group
        /// </summary>
        public List<string> additionalVisitors { get; set; }

        /// <summary>
        /// Should the site inherit from its parent
        /// </summary>
        public bool inheritPermissions { get; set; }

        /// <summary>
        /// Should the site inherit navigation from the parent
        /// </summary>
        public bool inheritNavigation { get; set; }

        /// <summary>
        /// Should the site be shared with the Active Directory everyone group
        /// </summary>
        public bool shareSiteWithEveryone { get; set; }

        /// <summary>
        /// Contains the Tenant Claim Id for the EveryoneGroup
        /// </summary>
        public string EveryoneGroupTenantId { get; set; }
    }
}
