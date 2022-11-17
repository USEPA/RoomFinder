using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("TenantWeb", Schema = "dbo")]
    public class EntityTenantWeb : ModelBase
    {
        public EntityTenantWeb()
        {
            this.TotalHits = 0;
            this.TotalHitsHomePage = 0;
            this.TotalUniqueVisitors = 0;
            this.TotalListsCalculated = 0;
            this.UniqueVisitorsHomePage = 0;
            this.SiteMetadata = true;
            this.SiteMetadataCount = 0;
            this.SiteMetadataPermissions = false;
            this.SiteMasterPage = false;
            this.PermUnique = false;
            this.PermAssociatedOwner = false;
            this.PermAssociatedMember = false;
            this.PermAssociatedVisitor = false;
            this.DocumentsCount = 0;
            this.MemberJoinCount = 0;
            this.DiscussionCount = 0;
            this.DiscussionReplyCount = 0;
            this.HasCommunity = false;
            this.DTADDED = DateTime.UtcNow;
            this.DTUPD = DateTime.UtcNow;
            this.SiteIsAddIn = false;
        }

        /// <summary>
        /// The unique identifier for sites
        /// </summary>
        [Column("WebGuid")]
        public Guid WebGuid { get; set; }

        [Required()]
        [MaxLength(255)]
        public string WebUrl { get; set; }

        [MaxLength(255)]
        public string WebTitle { get; set; }

        /// <summary>
        /// The Web Template
        /// </summary>
        public string SiteTemplateId { get; set; }

        /// <summary>
        /// Foreign Key to the Site Collection
        /// </summary>
        public int TenantSiteId { get; set; }

        /// <summary>
        /// Represents the owning Site Collection
        /// </summary>
        [ForeignKey("TenantSiteId")]
        public EntityTenantSite TenantSiteLookup { get; set; }

        public DateTime? SiteCreatedDate { get; set; }

        public DateTime? SiteLastModified { get; set; }


        public bool SiteIsAddIn { get; set; }

        /// <summary>
        /// DocumentActivityStatus Column - Indicates the level of document interaction
        /// </summary>
        [Column("DocumentActivityStatus")]
        public Nullable<Decimal> DocumentActivityStatus { get; set; }

        [Column("DocumentCount")]
        public Nullable<Int64> DocumentsCount { get; set; }

        [Column("DocumentLastEditedDate")]
        public DateTime? DocumentLastEditedDate { get; set; }

        /// <summary>
        /// Represents a timespan in which the site was active til today
        /// </summary>
        public Nullable<Decimal> SiteActivity { get; set; }

        /// <summary>
        /// Tracks the owners of the web
        /// </summary>
        public string SiteOwners { get; set; }

        /// <summary>
        /// Is metadata missing from this site
        /// </summary>
        public Nullable<bool> SiteMetadata { get; set; }

        public int SiteMetadataCount { get; set; }

        /// <summary>
        /// Does the web have a custom master page or an UnGhosted master page
        /// </summary>
        public Nullable<bool> SiteMasterPage { get; set; }

        /// <summary>
        /// Is the site missing the UG everyone group
        /// </summary>
        public Nullable<bool> SiteMetadataPermissions { get; set; }


        public bool PermUnique { get; set; }

        public bool PermAssociatedOwner { get; set; }

        public bool PermAssociatedMember { get; set; }

        public bool PermAssociatedVisitor { get; set; }

        /// <summary>
        /// Store the web welcome page
        /// </summary>
        public string WelcomePage { get; set; }
        /// <summary>
        /// Did we fail to process the welcomepage
        /// </summary>
        public bool WelcomePageError { get; set; }

        [Column("Total_Lists_Calculated")]
        public int TotalListsCalculated { get; set; }

        /// <summary>
        /// Represents a serialized collection of page information
        /// </summary>
        [Column("Total_Pages_Json")]
        public string TotalPagesJson { get; set; }

        [MaxLength(4000)]
        [Column("Total_URLs_Json")]
        public string TotalUrlsJson { get; set; }

        [Column("Total_Hits")]
        public Nullable<Int64> TotalHits { get; set; }


        [Column("Total_UniqueVisitors")]
        public Nullable<Int64> TotalUniqueVisitors { get; set; }


        [Column("Total_Hits_HomePage")]
        public Nullable<Int64> TotalHitsHomePage { get; set; }

        [Column("Unique_Visitors_Home_Page")]
        public Nullable<Int64> UniqueVisitorsHomePage { get; set; }


        public bool HasCommunity { get; set; }


        public Int64 MemberJoinCount { get; set; }


        public DateTime? MemberJoinLastDate { get; set; }

        /// <summary>
        /// Can members share the site with others
        /// </summary>
        public bool MembersCanShare { get; set; }


        public Int64 DiscussionCount { get; set; }

        public DateTime? DiscussionLastDate { get; set; }


        public Int64 DiscussionReplyCount { get; set; }


        public DateTime? DiscussionReplyLastDate { get; set; }


        /// <summary>
        /// Represents a serialized collection of discussion information
        /// </summary>
        [Column("DiscussionJSON")]
        public string DiscussionJSON { get; set; }


        public DateTime DTADDED { get; set; }

        /// <summary>
        /// The date this entity will be updated from a query
        /// </summary>
        public DateTime DTUPD { get; set; }

    }
}
