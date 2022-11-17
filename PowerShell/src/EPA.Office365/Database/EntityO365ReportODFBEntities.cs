using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    /// <summary>
    /// OneDrive for Business deployed monthly
    /// </summary>
    [Table("O365ReportODFBDeployedMonthly", Schema = "dbo")]
    public class EntityO365ReportODFBDeployedMonthly : ModelBase
    {
        public EntityO365ReportODFBDeployedMonthly()
        {
            this.Active = 0;
            this.Inactive = 0;
            this.DTADDED = DateTime.UtcNow;
        }

        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        public Int64 Active { get; set; }


        public Int64 Inactive { get; set; }


        public Nullable<Int64> ReportID { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// OneDrive for Business deployed weekly
    /// </summary>
    [Table("O365ReportODFBDeployedWeekly", Schema = "dbo")]
    public class EntityO365ReportODFBDeployedWeekly : ModelBase
    {
        public EntityO365ReportODFBDeployedWeekly()
        {
            this.Active = 0;
            this.Inactive = 0;
            this.DTADDED = DateTime.UtcNow;
        }


        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        public Int64 Active { get; set; }


        public Int64 Inactive { get; set; }


        public Nullable<Int64> ReportID { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// OneDrive for Business Storage monthly
    /// </summary>
    [Table("O365ReportODFBStorageMonthly", Schema = "dbo")]
    public class EntityO365ReportODFBStorageMonthly : ModelBase
    {
        public EntityO365ReportODFBStorageMonthly()
        {
            this.StorageUsedMB = 0;
            this.StorageUsedGB = 0;
            this.StorageUsedTB = 0;
            this.StorageAllocatedMB = 0;
            this.StorageAllocatedGB = 0;
            this.StorageAllocatedTB = 0;
            this.DTADDED = DateTime.UtcNow;
        }

        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        [Column("Storage_Used_MB")]
        public decimal StorageUsedMB { get; set; }

        [Column("Storage_Used_GB")]
        public decimal StorageUsedGB { get; set; }

        [Column("Storage_Used_TB")]
        public decimal StorageUsedTB { get; set; }

        [Column("Storage_Allocated_MB")]
        public decimal StorageAllocatedMB { get; set; }

        [Column("Storage_Allocated_GB")]
        public decimal StorageAllocatedGB { get; set; }

        [Column("Storage_Allocated_TB")]
        public decimal StorageAllocatedTB { get; set; }


        public Nullable<Int64> ReportID { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// OneDrive for Business Storage weekly
    /// </summary>
    [Table("O365ReportODFBStorageWeekly", Schema = "dbo")]
    public class EntityO365ReportODFBStorageWeekly : ModelBase
    {
        public EntityO365ReportODFBStorageWeekly()
        {
            this.StorageUsedMB = 0;
            this.StorageUsedGB = 0;
            this.StorageUsedTB = 0;
            this.StorageAllocatedMB = 0;
            this.StorageAllocatedGB = 0;
            this.StorageAllocatedTB = 0;
            this.DTADDED = DateTime.UtcNow;
        }

        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        [Column("Storage_Used_MB")]
        public decimal StorageUsedMB { get; set; }

        [Column("Storage_Used_GB")]
        public decimal StorageUsedGB { get; set; }

        [Column("Storage_Used_TB")]
        public decimal StorageUsedTB { get; set; }

        [Column("Storage_Allocated_MB")]
        public decimal StorageAllocatedMB { get; set; }

        [Column("Storage_Allocated_GB")]
        public decimal StorageAllocatedGB { get; set; }

        [Column("Storage_Allocated_TB")]
        public decimal StorageAllocatedTB { get; set; }


        public Nullable<System.Int64> ReportID { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }
}
