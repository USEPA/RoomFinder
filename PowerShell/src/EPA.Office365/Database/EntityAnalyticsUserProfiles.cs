using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("AnalyticsUserProfiles", Schema = "dbo")]
    public class EntityAnalyticsUserProfiles : ModelBase
    {
        public EntityAnalyticsUserProfiles()
        {
            this.TotalFiles = 0;
            this.StorageAllocated = 0;
            this.StorageUsed = 0;
            this.StorageUsedPerct = 0;
            this.ODFBSiteHits = 0;
            this.ODFBSiteVisits = 0;
            this.DTADDED = DateTime.UtcNow;
        }

        [Required()]
        [MaxLength(255)]
        public string Username { get; set; }


        [MaxLength(255)]
        public string ODFBSite { get; set; }


        public bool ODFBSiteProvisioned { get; set; }


        public Nullable<long> ODFBSiteHits { get; set; }


        public Nullable<long> ODFBSiteVisits { get; set; }


        public bool ExternalUserFlag { get; set; }


        public bool WorkPhone { get; set; }


        public bool ZipCode { get; set; }


        public bool Manager { get; set; }


        public bool Office { get; set; }


        public string OfficeName { get; set; }

        public string OfficeP1 { get; set; }
        public string OfficeP2 { get; set; }
        public string OfficeP3 { get; set; }
        public string OfficeP4 { get; set; }


        public bool ProfilePicture { get; set; }


        public bool AboutMe { get; set; }


        public bool Skills { get; set; }


        public long TotalFiles { get; set; }

        [Column("Storage_Allocated")]
        public decimal StorageAllocated { get; set; }


        [Column("Storage_Used")]
        public decimal StorageUsed { get; set; }


        [Column("Storage_Used_Perct")]
        public decimal StorageUsedPerct { get; set; }


        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }
}
