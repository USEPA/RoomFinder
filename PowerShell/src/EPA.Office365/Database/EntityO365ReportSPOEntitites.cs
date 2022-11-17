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
    /// Monthly active users monthly
    /// </summary>
    [Table("O365ReportSPOActiveUsersMonthly", Schema = "dbo")]
    public class EntityO365ReportSPOActiveUsersMonthly : ModelBase
    {
        public EntityO365ReportSPOActiveUsersMonthly()
        {
            this.UniqueUsers = 0;
            this.LicenseAssigned = 0;
            this.LicensesAcquired = 0;
            this.TotalUsers = 0;
            this.DTADDED = DateTime.UtcNow;
        }

        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        public Nullable<Int64> UniqueUsers { get; set; }


        public Nullable<Int64> LicenseAssigned { get; set; }


        public Nullable<Int64> LicensesAcquired { get; set; }


        public Nullable<Int64> TotalUsers { get; set; }


        public Nullable<Int64> ReportID { get; set; }


        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// Weekly active users weekly
    /// </summary>
    [Table("O365ReportSPOActiveUsersWeekly", Schema = "dbo")]
    public class EntityO365ReportSPOActiveUsersWeekly : ModelBase
    {
        public EntityO365ReportSPOActiveUsersWeekly()
        {
            this.UniqueUsers = 0;
            this.LicenseAssigned = 0;
            this.LicensesAcquired = 0;
            this.TotalUsers = 0;
            this.DTADDED = DateTime.UtcNow;
        }

        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        public Nullable<Int64> UniqueUsers { get; set; }


        public Nullable<Int64> LicenseAssigned { get; set; }


        public Nullable<Int64> LicensesAcquired { get; set; }


        public Nullable<Int64> TotalUsers { get; set; }


        public Nullable<Int64> ReportID { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// Client Software by Users
    /// </summary>
    [Table("O365ReportSPOClientSoftwareBrowser", Schema = "dbo")]
    public class EntityO365ReportSPOClientSoftwareBrowser : ModelBase
    {
        public EntityO365ReportSPOClientSoftwareBrowser()
        {
            this.TotalCount = 0;
            this.DTADDED = DateTime.UtcNow;
        }

        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        public Int64 TotalCount { get; set; }


        public string DisplayName { get; set; }


        public DateTime? LastAccessTime { get; set; }


        public string ObjectId { get; set; }


        public string Username { get; set; }


        public string ClientName { get; set; }


        public string ClientVersion { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// Weekly conference analytics
    /// </summary>
    [Table("O365ReportSPOConferencesWeekly", Schema = "dbo")]
    public class EntityO365ReportSPOConferenceWeekly : ModelBase
    {
        public EntityO365ReportSPOConferenceWeekly()
        {
            this.ApplicationSharing = 0;
            this.AudioVisual = 0;
            this.InstantMessaging = 0;
            this.Telephony = 0;
            this.TotalConferences = 0;
            this.WebConferences = 0;
            this.DTADDED = DateTime.UtcNow;
        }

        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        [Column("ApplicationSharing")]
        public Int64 ApplicationSharing { get; set; }

        [Column("AudioVisual")]
        public Int64 AudioVisual { get; set; }

        [Column("InstantMessaging")]
        public Int64 InstantMessaging { get; set; }

        [Column("Telephony")]
        public Int64 Telephony { get; set; }

        [Column("TotalConferences")]
        public Int64 TotalConferences { get; set; }

        [Column("WebConferences")]
        public Int64 WebConferences { get; set; }


        public Nullable<Int64> ReportID { get; set; }


        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// connections by client type analytics
    /// </summary>
    [Table("O365ReportSPOConnectionsWeekly", Schema = "dbo")]
    public class EntityO365ReportSPOConnectionClientWeekly : ModelBase
    {
        public EntityO365ReportSPOConnectionClientWeekly()
        {
            this.POP3 = 0;
            this.MAPI = 0;
            this.OWA = 0;
            this.EAS = 0;
            this.EWS = 0;
            this.IMAP = 0;
            this.DTADDED = DateTime.UtcNow;
        }


        public DateTime? tblDate { get; set; }


        public Nullable<int> tblDateMonth { get; set; }


        public string Month { get; set; }


        public int Year { get; set; }


        public Int64 POP3 { get; set; }


        public Int64 MAPI { get; set; }


        public Int64 OWA { get; set; }


        public Int64 EAS { get; set; }


        public Int64 EWS { get; set; }


        public Int64 IMAP { get; set; }


        public Nullable<System.Int64> ReportID { get; set; }

        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// Tenant Storage monthly
    /// </summary>
    [Table("O365ReportSPOTenantStorageMonthly", Schema = "dbo")]
    public class EntityO365ReportSPOTenantStorageMonthly : ModelBase
    {
        public EntityO365ReportSPOTenantStorageMonthly()
        {
            this.StorageUsedMB = 0;
            this.StorageUsedGB = 0;
            this.StorageUsedTB = 0;
            this.StorageAllocatedMB = 0;
            this.StorageAllocatedGB = 0;
            this.StorageAllocatedTB = 0;
            this.StorageTotal = 0;
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

        [Column("Storage_Total")]
        public decimal StorageTotal { get; set; }


        public Nullable<System.Int64> ReportID { get; set; }


        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

    /// <summary>
    /// Tenant Storage weekly
    /// </summary>
    [Table("O365ReportSPOTenantStorageWeekly", Schema = "dbo")]
    public class EntityO365ReportSPOTenantStorageWeekly : ModelBase
    {
        public EntityO365ReportSPOTenantStorageWeekly()
        {
            this.StorageUsedMB = 0;
            this.StorageUsedGB = 0;
            this.StorageUsedTB = 0;
            this.StorageAllocatedMB = 0;
            this.StorageAllocatedGB = 0;
            this.StorageAllocatedTB = 0;
            this.StorageTotal = 0;
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

        [Column("Storage_Total")]
        public decimal StorageTotal { get; set; }


        public Nullable<Int64> ReportID { get; set; }


        public DateTime DTADDED { get; set; }

        public DateTime? DTUPD { get; set; }
    }

}
