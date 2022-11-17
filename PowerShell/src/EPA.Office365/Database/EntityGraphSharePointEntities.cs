using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EPA.Office365.Database
{
    /// <summary>
    /// Get the number of unique, licensed users who interacted with files stored on SharePoint sites.
    /// </summary>
    [Table("GraphSharePointActivityFileCounts", Schema = "dbo")]
    public class EntityGraphSharePointActivityFileCounts
    {
        public DateTime ReportRefreshDate { get; set; }

        public long? ViewedOrEdited { get; set; }

        public long? Synced { get; set; }

        public long? SharedInternally { get; set; }

        public long? SharedExternally { get; set; }

        public DateTime ReportDate { get; set; }

        public int ReportPeriod { get; set; }
    }

    /// <summary>
    /// Get the trend in the number of active users. A user is considered active if he or she has executed a file activity (save, sync, modify, or share) or visited a page within the specified time period.
    /// </summary>
    [Table("GraphSharePointActivityUserCounts", Schema = "dbo")]
    public class EntityGraphSharePointActivityUserCounts
    {
        public DateTime ReportRefreshDate { get; set; }

        public long? VisitedPage { get; set; }

        public long? ViewedOrEdited { get; set; }

        public long? Synced { get; set; }

        public long? SharedInternally { get; set; }

        public long? SharedExternally { get; set; }

        public DateTime ReportDate { get; set; }

        public int ReportPeriod { get; set; }
    }

    /// <summary>
    /// Get the number of unique pages visited by users.
    /// </summary>
    [Table("GraphSharePointActivityPagesCounts", Schema = "dbo")]
    public class EntityGraphSharePointActivityPagesCounts
    {
        public DateTime ReportRefreshDate { get; set; }

        public long? VisitedPageCount { get; set; }

        public DateTime ReportDate { get; set; }

        public int ReportPeriod { get; set; }
    }

    /// <summary>
    /// Get details about SharePoint activity by user.
    /// </summary>
    [Table("GraphSharePointActivityUserDetail", Schema = "dbo")]
    public class EntityGraphSharePointActivityUserDetail
    {
        public EntityGraphSharePointActivityUserDetail()
        {
            IsDeleted = false;
            ViewedOrEditedFileCount = 0;
            SyncedFileCount = 0;
            SharedInternallyFileCount = 0;
            SharedExternallyFileCount = 0;
            VisitedPageCount = 0;
            ReportPeriod = 0;
        }

        public DateTime? ReportRefreshDate { get; set; }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        [Key()]
        public string UserPrincipalName { get; set; }

        public string ProductsAssigned { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedDate { get; set; }

        public long? ViewedOrEditedFileCount { get; set; }

        public long? SyncedFileCount { get; set; }

        public long? SharedInternallyFileCount { get; set; }

        public long? SharedExternallyFileCount { get; set; }

        public long? VisitedPageCount { get; set; }

        public int ReportPeriod { get; set; }
    }

    /// <summary>
    /// SharePoint Site Usage Details by SiteCollection
    /// </summary>
    [Table("GraphSharePointSiteUsageDetail", Schema = "dbo")]
    public class EntityGraphSharePointSiteUsageDetail
    {
        public EntityGraphSharePointSiteUsageDetail()
        {
            IsDeleted = false;
            FileCount = 0;
            ActiveFileCount = 0;
            PageViewCount = 0;
            VisitedPageCount = 0;
            StorageUsed_Byte = 0;
            StorageAllocated_Byte = 0;
            ReportPeriod = 0;
        }

        public DateTime? ReportRefreshDate { get; set; }

        [Key()]
        public string SiteURL { get; set; }

        public string OwnerDisplayName { get; set; }

        public bool IsDeleted { get; set; }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        public long? FileCount { get; set; }

        public long? ActiveFileCount { get; set; }

        public long? PageViewCount { get; set; }

        public long? VisitedPageCount { get; set; }

        public long? StorageUsed_Byte { get; set; }

        public long? StorageAllocated_Byte { get; set; }

        public string RootWebTemplate { get; set; }

        public int ReportPeriod { get; set; }
    }

    /// <summary>
    /// SharePoint Site Usage Site Count
    /// </summary>
    [Table("GraphSharePointSiteUsageSiteCounts", Schema = "dbo")]
    public class EntityGraphSharePointSiteUsageSiteCounts
    {
        public DateTime ReportRefreshDate { get; set; }

        public string SiteType { get; set; }

        public long? Total { get; set; }

        public long? Active { get; set; }

        public DateTime ReportDate { get; set; }

        public int ReportPeriod { get; set; }
    }


    [Table("GraphSharePointSiteUsageFileCounts", Schema = "dbo")]
    public class EntityGraphSharePointSiteUsageFileCounts
    {
        public DateTime ReportRefreshDate { get; set; }

        public string SiteType { get; set; }

        public long? Total { get; set; }

        public long? Active { get; set; }

        public DateTime ReportDate { get; set; }

        public int ReportPeriod { get; set; }
    }


    [Table("GraphSharePointSiteUsagePages", Schema = "dbo")]
    public class EntityGraphSharePointSiteUsagePages
    {
        public DateTime ReportRefreshDate { get; set; }

        public string SiteType { get; set; }

        public long? PageViewCount { get; set; }

        public DateTime ReportDate { get; set; }

        public int ReportPeriod { get; set; }
    }

    /// <summary>
    /// SharePoint Site Usage Storage
    /// </summary>
    [Table("GraphSharePointSiteUsageStorage", Schema = "dbo")]
    public class EntityGraphSharePointSiteUsageStorage
    {
        public DateTime ReportRefreshDate { get; set; }

        public string SiteType { get; set; }

        public long? StorageUsed_Byte { get; set; }

        public DateTime ReportDate { get; set; }

        public int ReportPeriod { get; set; }
    }
}
