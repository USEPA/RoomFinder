using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("AnalyticsExternalUsersSites", Schema = "dbo")]
    public class EntityAnalyticsExternalUsersSites : ModelBase
    {
        [Required]
        [Column("exID")]
        public int ExternalLookupID { get; set; }

        [ForeignKey("ExternalLookupID")]
        public EntityAnalyticsExternalUsers ExternalLookup { get; set; }

        public string Site { get; set; }

        public string Region { get; set; }

        public string SiteType { get; set; }

        public string ExternalUserID { get; set; }

        public string LoginName { get; set; }

        public string DisplayName { get; set; }

        public string EmailAddress { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }

    }
}
