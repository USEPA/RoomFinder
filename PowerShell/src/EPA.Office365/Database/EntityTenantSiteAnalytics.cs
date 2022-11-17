using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("TenantSiteAnalytics", Schema = "dbo")]
    public class EntityTenantSiteAnalytics : ModelBase
    {
        public EntityTenantSiteAnalytics()
        {
            this.Storage_Usage_MB = 0;
            this.Storage_Usage_GB = 0;
            this.Storage_Used_Perct = 0;
            this.Storage_Allocated_MB = 0;
            this.Storage_Allocated_GB = 0;
            this.TotalSiteUsers = 0;
            this.SubSiteCount = 0;
            this.TrackedSite = false;
            this.DTMETRIC = DateTime.UtcNow;
        }

        /// <summary>
        /// The unique identifier for sites
        /// </summary>
        [Column("SiteGuid")]
        public Guid SiteGuid { get; set; }

        [Required]
        [Column("fkTenantDateId")]
        public int TenantDateId { get; set; }

        [ForeignKey("TenantDateId")]
        public EntityTenantDates TenantDateLookup { get; set; }

        /// <summary>
        /// Indicates in the current Tenant ID run the site was successfully scanned
        /// </summary>
        [Column("ScanCompleted")]
        public bool ScanCompleted { get; set; }

        [Required]
        [Column("tenantSiteId")]
        public int TenantSiteId { get; set; }

        [ForeignKey("TenantSiteId")]
        public EntityTenantSite TenantSiteLookup { get; set; }


        public int TotalSiteUsers { get; set; }


        public int SubSiteCount { get; set; }


        public bool TrackedSite { get; set; }

        /// <summary>
        /// Tracks the site owners
        /// </summary>
        public string SiteOwners { get; set; }


        public decimal Storage_Usage_MB { get; set; }


        public decimal Storage_Usage_GB { get; set; }


        public decimal Storage_Used_Perct { get; set; }


        public decimal Storage_Allocated_MB { get; set; }


        public decimal Storage_Allocated_GB { get; set; }


        public DateTime DTMETRIC { get; set; }
    }
}
