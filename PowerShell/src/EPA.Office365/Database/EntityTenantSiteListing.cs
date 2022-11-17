using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("TenantSiteListing", Schema = "dbo")]
    public class EntityTenantSiteListing : ModelBase
    {
        [Column("Url")]
        [MaxLength(512)]
        public string Url { get; set; }

        [MaxLength(50)]
        public string SiteType { get; set; }

        /// <summary>
        /// The date this site has a LastItemUpdated
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Last run date where this entity was updated in the database
        /// </summary>
        public DateTime DateModified { get; set; }
    }
}
