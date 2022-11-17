using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    [Table("eBusinessAccounts", Schema = "etl")]
    public class EntityEBusinessAccounts
    {
        [Key]
        [Required()]
        [MaxLength(255)]
        public string SamAccountName { get; set; }


        [MaxLength(5)]
        public string Organization { get; set; }

        [MaxLength(10)]
        public string Acronym { get; set; }

        [MaxLength(15)]
        public string OfficeCode { get; set; }

        [MaxLength(255)]
        public string OfficeName { get; set; }

        [MaxLength(150)]
        [Column("PREFERRED_FIRSTNAME")]
        public string PreferredFirstName { get; set; }

        [MaxLength(150)]
        public string FirstName { get; set; }

        [MaxLength(25)]
        [Column("MIDDLE_INITIAL")]
        public string MiddleInitial { get; set; }

        [MaxLength(150)]
        public string LastName { get; set; }

        [MaxLength(10)]
        [Column("AFFLIATION_CODE")]
        public string AffiliationCode { get; set; }

        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(15)]
        public string WorkforceID { get; set; }

        [MaxLength(1000)]
        public string DistinguishedName { get; set; }

        public bool? Enabled { get; set; }

        [MaxLength(1000)]
        public string Building { get; set; }

        [MaxLength(250)]
        [Column("ADDRESS_LINE1")]
        public string AddressLine1 { get; set; }

        [MaxLength(75)]
        public string City { get; set; }

        [MaxLength(50)]
        public string State { get; set; }

        [MaxLength(12)]
        public string ZipCode { get; set; }

        [MaxLength(150)]
        [Column("ROOM_NUMBER")]
        public string RoomNumber { get; set; }

        [Column("DT_IMPORT", TypeName = "datetime2")] 
        public DateTime DateImport { get; set; }
    }
}
