using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("AnalyticsUserProfilesSubsites", Schema = "dbo")]
    public class EntityAnalyticsUserProfilesSubsites : ModelBase
    {
        public EntityAnalyticsUserProfilesSubsites()
        {
            this.ListCount = 0;
            this.FlaggedCount = 0;
            this.DateCreated = DateTime.UtcNow;
        }

        [MaxLength(255)]
        public string Title { get; set; }

        [Required()]
        [MaxLength(255)]
        public string SiteOwner { get; set; }


        [MaxLength(100)]
        public string SiteTemplate { get; set; }


        public int? ListCount { get; set; }

        [MaxLength(255)]
        public string Url { get; set; }

        public DateTime DateCreated { get; set; }

        [Column("Exception")]
        public bool ExceptionFlag { get; set; }


        [MaxLength(1024)]
        public string SiteOwnerComment { get; set; }


        [MaxLength(1024)]
        public string AdminComment { get; set; }


        public DateTime? LastModified { get; set; }


        public string Status { get; set; }


        public int? FlaggedCount { get; set; }


        public DateTime? FirstNotification { get; set; }

        public DateTime? SecondNotification { get; set; }

        public DateTime? LastNotification { get; set; }

        public int? NotificationCount { get; set; }


        public DateTime? LastExpiredNotification { get; set; }

    }
}
