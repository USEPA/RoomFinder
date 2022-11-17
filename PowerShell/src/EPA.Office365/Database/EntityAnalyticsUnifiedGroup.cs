using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("AnalyticsO365Groups", Schema = "dbo")]
    public class EntityAnalyticsUnifiedGroup : ModelBase
    {
        public EntityAnalyticsUnifiedGroup()
        {
            this.StorageUsedGB = 0;
            this.StorageUsedPercent = 0;
            this.StorageAllocatedGB = 0;
            this.MailEnabled = false;
            this.AllowExternalSenders = false;
            this.AutoSubscribeNewMembers = false;
            this.EPAWide = false;
            this.DTADDED = DateTime.Now;
        }

        [Required()]
        [MaxLength(255)]
        public string GroupId { get; set; }


        [MaxLength(255)]
        public string GroupName { get; set; }


        [MaxLength(255)]
        public string MailAddress { get; set; }

        [MaxLength(255)]
        public string PrimaryOwner { get; set; }

        public string Owners { get; set; }

        // Report Information
        public DateTime? ReportRefreshDate { get; set; }
        public Nullable<bool> IsDeleted { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public Nullable<long> MemberCount { get; set; }
        public Nullable<long> ExchangeReceivedEmailCount { get; set; }
        public Nullable<long> SharePointActiveFileCount { get; set; }
        public Nullable<long> ExchangeMailboxTotalItemCount { get; set; }
        public Nullable<long> ExchangeMailboxStorageUsed_Byte { get; set; }
        public Nullable<long> SharePointTotalFileCount { get; set; }
        public Nullable<long> SharePointSiteStorageUsed_Byte { get; set; }
        public string ReportPeriod { get; set; }

        // Teams Information
        public string TeamDisplayName { get; set; }
        public string TeamDescription { get; set; }
        public string TeamInternalId { get; set; }
        public string TeamClassification { get; set; }
        public string TeamSpecialization { get; set; }
        public string TeamVisibility { get; set; }
        public string TeamDiscoverySettings { get; set; }
        public string TeamResponseHeaders { get; set; }
        public string TeamStatusCode { get; set; }
        public Nullable<bool> TeamIsArchived { get; set; }
        public string TeamWebUrl { get; set; }

        public Nullable<bool> IsSiteProvisioned { get; set; }

        /// <summary>
        /// Foreign Key to the Site Collection
        /// </summary>
        public Nullable<bool> MailEnabled { get; set; }

        public Nullable<bool> AllowExternalSenders { get; set; }


        public Nullable<bool> AutoSubscribeNewMembers { get; set; }


        public string Site { get; set; }


        public Nullable<bool> EPAWide { get; set; }


        public string PublicPrivate { get; set; }


        public string Office { get; set; }


        public DateTime? CreatedDate { get; set; }

        public Nullable<int> CreatedDateMonth { get; set; }

        public Nullable<int> CreatedDateYear { get; set; }


        [Column("Storage_GB")]
        public Nullable<decimal> StorageUsedGB { get; set; }


        [Column("Storage_Used_Perct")]
        public Nullable<decimal> StorageUsedPercent { get; set; }


        [Column("Storage_Quota_GB")]
        public Nullable<decimal> StorageAllocatedGB { get; set; }



        public DateTime DTADDED { get; set; }

        /// <summary>
        /// The date this entity will be updated from a query
        /// </summary>
        public DateTime? DTUPD { get; set; }
    }
}
