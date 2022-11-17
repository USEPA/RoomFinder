using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("eBusinessOfficeListing", Schema = "dbo")]
    public class EntityOffice : ModelBase
    {
        public string Title { get; set; }

        public string OrgName { get; set; }

        public string OrgCode { get; set; }

        /// <summary>
        /// What is the alpha code for the organization
        /// </summary>
        public string AlphaCode { get; set; }

        /// <summary>
        /// To which organization does the office belong
        /// </summary>
        public string ReportsTo { get; set; }

        /// <summary>
        /// is the current row in the system
        /// </summary>
        public bool CurrentRow { get; set; }

        /// <summary>
        /// Which ID did this row spawn from
        /// </summary>
        public Nullable<int> ChangeFromRow { get; set; }
    }
}
