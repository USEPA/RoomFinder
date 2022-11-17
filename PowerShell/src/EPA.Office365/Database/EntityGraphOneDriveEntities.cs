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
    /// Contains dates for recalculating the OneDrive Metrics
    /// </summary>
    [Table("O365PreviewOneDriveDates", Schema = "dbo")]
    public class EntityO365OneDriveDates : ModelBase
    {
        public EntityO365OneDriveDates()
        {
            RequiresCalculation = true;
        }

        [MaxLength(50)]
        public string FormattedDate { get; set; }


        public DateTime? DTSTART { get; set; }

        public DateTime? DTEND { get; set; }


        public int? TotalMetricDays { get; set; }


        public bool RequiresCalculation { get; set; }

        /// <summary>
        /// Last date we pulled Activity Detail data
        /// </summary>
        public DateTime? LastDateActivity { get; set; }

        /// <summary>
        /// Last date we pulled Usage Detail data
        /// </summary>
        public DateTime? LastDateUsage { get; set; }
    }


    /// <summary>
    /// OneDrive Activity Detail
    /// </summary>
    [Table("GraphOneDriveActivityDetail", Schema = "dbo")]
    public class EntityGraphOneDriveActivityDetail
    {
        public EntityGraphOneDriveActivityDetail()
        {
            ODB_TotalFileViewedModified = 0;
            ODB_TotalFileSynched = 0;
            ODB_TotalFileSharedEXT = 0;
            ODB_TotalFileSharedINT = 0;
            ODB_TotalofAllActivities = 0;
        }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        [Key()]
        public string UPN { get; set; }

        public int ReportingPeriodInDays { get; set; }

        public string ProductsAssigned { get; set; }

        public string Deleted { get; set; }

        public DateTime? DeletedDate { get; set; }

        [Column("ODB_TotalofAllActivities")]
        public Int64 ODB_TotalofAllActivities { get; set; }

        [Column("ODB_CollaboratedByOthers")]
        public Int64 ODB_CollaboratedByOthers { get; set; }

        [Column("ODB_CollaboratedByOwner")]
        public Int64 ODB_CollaboratedByOwner { get; set; }

        [Column("ODB_TotalFileViewedModified")]
        public Int64 ODB_TotalFileViewedModified { get; set; }

        [Column("ODB_TotalFileSynched")]
        public Int64 ODB_TotalFileSynched { get; set; }

        [Column("ODB_TotalFileSharedEXT")]
        public Int64 ODB_TotalFileSharedEXT { get; set; }

        [Column("ODB_TotalFileSharedINT")]
        public Int64 ODB_TotalFileSharedINT { get; set; }
    }

    /// <summary>
    /// OneDrive Activity Files
    /// </summary>
    [Table("GraphOneDriveActivityFiles", Schema = "dbo")]
    public class EntityGraphOneDriveActivityFiles
    {
        public EntityGraphOneDriveActivityFiles()
        {
            ODB_TotalFileViewedModified = 0;
            ODB_TotalFileSynched = 0;
            ODB_TotalFileSharedEXT = 0;
            ODB_TotalFileSharedINT = 0;
            ReportingPeriodInDays = 0;
        }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        public int ReportingPeriodInDays { get; set; }

        [Column("ODB_TotalFileViewedModified")]
        public Int64 ODB_TotalFileViewedModified { get; set; }

        [Column("ODB_TotalFileSynched")]
        public Int64 ODB_TotalFileSynched { get; set; }

        [Column("ODB_TotalFileSharedEXT")]
        public Int64 ODB_TotalFileSharedEXT { get; set; }

        [Column("ODB_TotalFileSharedINT")]
        public Int64 ODB_TotalFileSharedINT { get; set; }
    }

    /// <summary>
    /// OneDrive Activity Users
    /// </summary>
    [Table("GraphOneDriveActivityUsers", Schema = "dbo")]
    public class EntityGraphOneDriveActivityUsers
    {
        public EntityGraphOneDriveActivityUsers()
        {
            ODB_TotalFileViewedModified = 0;
            ODB_TotalFileSynched = 0;
            ODB_TotalFileSharedEXT = 0;
            ODB_TotalFileSharedINT = 0;
            ReportingPeriodInDays = 0;
        }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        public int ReportingPeriodInDays { get; set; }

        [Column("ODB_TotalFileViewedModified")]
        public Int64 ODB_TotalFileViewedModified { get; set; }

        [Column("ODB_TotalFileSynched")]
        public Int64 ODB_TotalFileSynched { get; set; }

        [Column("ODB_TotalFileSharedEXT")]
        public Int64 ODB_TotalFileSharedEXT { get; set; }

        [Column("ODB_TotalFileSharedINT")]
        public Int64 ODB_TotalFileSharedINT { get; set; }
    }

    /// <summary>
    /// OneDrive Usage Account
    /// </summary>
    [Table("GraphOneDriveUsageAccount", Schema = "dbo")]
    public class EntityGraphOneDriveUsageAccount
    {
        public EntityGraphOneDriveUsageAccount()
        {
            TotalAccounts = 0;
            TotalActiveAccounts = 0;
        }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        [Key()]
        public string SiteType { get; set; }

        [Column("Total_Accounts")]
        public long? TotalAccounts { get; set; }

        [Column("Total_ActiveAccounts")]
        public long? TotalActiveAccounts { get; set; }

        public int ReportingPeriodInDays { get; set; }
    }

    /// <summary>
    /// OneDrive Usage Detail
    /// </summary>
    [Table("GraphOneDriveUsageDetail", Schema = "dbo")]
    public class EntityGraphOneDriveUsageDetail
    {
        public EntityGraphOneDriveUsageDetail()
        {
            TotalFiles = 0;
            TotalFilesViewedModified = 0;
            Storage_Allocated_B = 0;
            StorageUsedByte = 0;
            StorageUsedMB = 0;
            StorageUsedGB = 0;
            StorageUsedTB = 0;
            ReportingPeriodInDays = 0;
        }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        [Key()]
        public string SiteURL { get; set; }

        public string SiteOwner { get; set; }

        public string Deleted { get; set; }

        public Int64 TotalFiles { get; set; }

        public Int64 TotalFilesViewedModified { get; set; }

        [Column("Storage_Allocated_B")]
        public Int64 Storage_Allocated_B { get; set; }

        [Column("Storage_Used_B")]
        public Int64 StorageUsedByte { get; set; }

        [Column("Storage_Used_MB")]
        public decimal StorageUsedMB { get; set; }

        [Column("Storage_Used_GB")]
        public decimal StorageUsedGB { get; set; }

        [Column("Storage_Used_TB")]
        public decimal StorageUsedTB { get; set; }

        public int ReportingPeriodInDays { get; set; }


    }

    /// <summary>
    /// OneDrive Usage Storage
    /// </summary>
    [Table("GraphOneDriveUsageStorage", Schema = "dbo")]
    public class EntityGraphOneDriveUsageStorage
    {
        public EntityGraphOneDriveUsageStorage()
        {
            StorageUsedByte = 0;
            StorageUsedMB = 0;
            StorageUsedGB = 0;
            StorageUsedTB = 0;
            ReportingPeriodInDays = 0;
        }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        [Key()]
        public string SiteType { get; set; }

        [Column("Storage_Used_B")]
        public long? StorageUsedByte { get; set; }

        [Column("Storage_Used_MB")]
        public decimal StorageUsedMB { get; set; }

        [Column("Storage_Used_GB")]
        public decimal StorageUsedGB { get; set; }

        [Column("Storage_Used_TB")]
        public decimal StorageUsedTB { get; set; }

        public int ReportingPeriodInDays { get; set; }
    }

    /// <summary>
    /// OneDrive Usage File Counts
    /// </summary>
    [Table("GraphOneDriveUsageFileCounts", Schema = "dbo")]
    public class EntityGraphOneDriveUsageFileCounts
    {
        public EntityGraphOneDriveUsageFileCounts()
        {
            Total = 0;
            Active = 0;
        }

        [Key()]
        public DateTime ReportDate { get; set; }

        [Key()]
        public string SiteType { get; set; }

        [Column("Total")]
        public long? Total { get; set; }

        [Column("Active")]
        public long? Active { get; set; }

        public int ReportPeriod { get; set; }

        public DateTime? ReportRefreshDate { get; set; }
    }
}
