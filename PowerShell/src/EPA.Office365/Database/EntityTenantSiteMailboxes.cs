using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("TenantSiteMailboxes", Schema = "dbo")]
    public class EntityTenantSiteMailboxes : ModelBase
    {
        [Column("ParentSiteUrl")]
        [MaxLength(512)]
        public string Url { get; set; }

        [MaxLength(1024)]
        public string SiteOwnerEmail { get; set; }

        [MaxLength(512)]
        public string MailboxAddresses { get; set; }

        [MaxLength(255)]
        public string UserName { get; set; }

        /// <summary>
        /// The date this site was removed
        /// </summary>
        public DateTime DateRemoved { get; set; }
    }
}
