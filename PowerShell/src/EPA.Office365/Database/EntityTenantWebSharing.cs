using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("TenantWebSharing", Schema = "dbo")]
    public class EntityTenantWebSharing : ModelBase
    {
        public EntityTenantWebSharing()
        {
            CanMemberShare = false;
        }

        /// <summary>
        /// The unique identifier for sites
        /// </summary>
        [Column("WebGuid")]
        public Guid WebGuid { get; set; }

        [Required()]
        [MaxLength(255)]
        public string WebUrl { get; set; }

        [MaxLength(255)]
        public string WebTitle { get; set; }

        /// <summary>
        /// The Web Template
        /// </summary>
        public string SiteTemplateId { get; set; }


        [MaxLength(100)]
        public string Region { get; set; }


        [MaxLength(100)]
        public string SiteType { get; set; }


        public bool CanMemberShare { get; set; }
    }
}
