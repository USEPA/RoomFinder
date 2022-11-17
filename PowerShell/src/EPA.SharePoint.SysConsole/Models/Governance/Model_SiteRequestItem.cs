using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Governance
{
    public class Model_SiteRequestItem : SPListItemDefinition
    {

        public Model_SiteRequestItem() : base()
        {
            this.ColumnValues = new List<SPListItemFieldDefinition>();
        }

        public string SiteUrl { get; set; }
        public string SiteUrlTrailingSlash { get; set; }

        /// <summary>
        /// Title for the Site
        /// </summary>
        public string SiteName { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Contains the user email address
        /// </summary>
        public string SiteOwner { get; set; }

        /// <summary>
        /// Display name for the user who will own the site
        /// </summary>
        public string SiteOwnerName { get; set; }

        /// <summary>
        /// The sponsor of the site, also the user who approved it
        /// </summary>
        public string SiteSponsor { get; set; }

        /// <summary>
        /// Represents the Site Type or the Template for which the site will be tailored
        /// </summary>
        public string TypeOfSite { get; set; }

        /// <summary>
        /// Represents the Site Collection to which the site will be provisioned
        /// </summary>
        public int TypeOfSiteID { get; set; }

        /// <summary>
        /// Organization to which this site will be registered
        /// </summary>
        public string AAShipRegionOffice { get; set; }

        public string Topics { get; set; }

        public string OfficeTeamCommunityName { get; set; }

        public string EpaLineOfBusiness { get; set; }

        public string OrganizationAcronym { get; set; }

        public string TemplateName { get; set; }

        public string IntendedAudience { get; set; }

        public string missingMetaDataId { get; set; }

        /// <summary>
        /// Allow joining
        /// </summary>
        public string JoinFlag { get; set; }

        /// <summary>
        /// Should the site be open to everyone
        /// </summary>
        public string OpenFlag { get; set; }

        public string SiteSponsorApprovedFlag { get; set; }

        public string RequestorEmail { get; set; }

        public string RequestRejectedFlag { get; set; }

        public string RequestCompletedFlag { get; set; }

        /// <summary>
        /// Guid in the List
        /// </summary>
        public string WebGuid { get; set; }

        /// <summary>
        /// Site check after opening with Tenant context
        /// </summary>
        public bool SiteExists { get; set; }

        /// <summary>
        /// Indicates we could not find the URL in the Tenant
        /// </summary>
        public bool SiteMovedOrDeleted { get; set; }

        /// <summary>
        /// An item was inserted
        /// </summary>
        public bool PrcInserted { get; set; }

        /// <summary>
        /// The item was updated
        /// </summary>
        public bool PrcUpdated { get; set; }

        public override string ToString()
        {
            return string.Format("ID {0} Title {1} exists=>{2}", Id, SiteUrl, SiteExists);
        }
    }
}
