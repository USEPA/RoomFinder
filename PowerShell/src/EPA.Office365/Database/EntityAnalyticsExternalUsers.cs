using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("AnalyticsExternalUsers", Schema = "dbo")]
    public class EntityAnalyticsExternalUsers : ModelBase
    {
        public EntityAnalyticsExternalUsers()
        {
            this.UserSites = new List<EntityAnalyticsExternalUsersSites>();
        }

        [Required()]
        [MaxLength(100)]
        public string ExternalUserID { get; set; }

        public string InvitedAs { get; set; }

        public string AcceptedAs { get; set; }

        public string DisplayName { get; set; }

        public string EmailAddress { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }

        public List<EntityAnalyticsExternalUsersSites> UserSites { get; set; }
    }
}
