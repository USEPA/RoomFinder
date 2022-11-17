using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("TenantDates", Schema = "dbo")]
    public class EntityTenantDates : ModelBase
    {
        public EntityTenantDates()
        {
            this.DTSTART = DateTime.UtcNow;
            this.IsCurrent = false;
        }

        public AnalyticTypeEnum AnalyticType { get; set; }

        [Required()]
        [MaxLength(255)]
        public string FormattedDate { get; set; }


        [Required()]
        public DateTime DTSTART { get; set; }


        [Required()]
        public DateTime DTEND { get; set; }

        /// <summary>
        /// This is the most recent data pull
        /// </summary>
        public bool IsCurrent { get; set; }

        /// <summary>
        /// This timeframe is completed
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Total Sites at runtime
        /// </summary>
        public Nullable<int> TotalSites { get; set; }

        /// <summary>
        /// Total Webs at runtime
        /// </summary>
        public Nullable<int> TotalWebs { get; set; }

        /// <summary>
        /// The collection of various analytic data runs
        /// </summary>
        public ICollection<EntityTenantSiteAnalytics> SiteAnalytics { get; set; }
    }
}
