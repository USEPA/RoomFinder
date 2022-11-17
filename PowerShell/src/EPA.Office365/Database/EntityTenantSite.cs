using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("TenantSite", Schema = "dbo")]
    public class EntityTenantSite : ModelBase
    {
        public EntityTenantSite()
        {
            this.DTADDED = DateTime.UtcNow;
            this.DTUPD = DateTime.UtcNow;
        }

        /// <summary>
        /// The unique identifier for sites
        /// </summary>
        [Column("SiteGuid")]
        public Guid SiteGuid { get; set; }

        [Required()]
        [MaxLength(255)]
        public string SiteUrl { get; set; }


        [MaxLength(255)]
        public string Region { get; set; }


        [MaxLength(255)]
        public string SiteType { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime DTUPD { get; set; }

        /// <summary>
        /// The collection of various SPSite analytic data runs
        /// </summary>
        public ICollection<EntityTenantSiteAnalytics> SiteAnalytics { get; set; }

        /// <summary>
        /// The collection of various SPWeb analytic data runs
        /// </summary>
        public ICollection<EntityTenantWeb> WebAnalytics { get; set; }
    }
}
