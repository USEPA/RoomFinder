using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    public abstract class EntityMSTeamsBase
    {
        public DateTime ReportRefreshDate { get; set; }


        public int ReportPeriod { get; set; }
    }

    public abstract class EntityMSTeamsCountBase : EntityMSTeamsBase
    {
        public DateTime ReportDate { get; set; }
    }

    /// <summary>
    /// Get the trends on Teams Activity Counts for the tenant.
    /// </summary>
    [Table("GraphTeamsActivityActivityCounts", Schema = "dbo")]
    public class EntityMSTeamsActivityActivityCounts : EntityMSTeamsCountBase
    {
        public Nullable<Int64> TeamChatMessages { get; set; }

        public Nullable<Int64> PrivateChatMessages { get; set; }

        public Nullable<Int64> Calls { get; set; }

        public Nullable<Int64> Meetings { get; set; }
    }

    /// <summary>
    /// Get the trends on how many unique users work within Teams (Calls, Meetings, and Chat Messages).
    /// </summary>
    [Table("GraphTeamsActivityUserCounts", Schema = "dbo")]
    public class EntityMSTeamsActivityUserCounts : EntityMSTeamsCountBase
    {
        public Nullable<Int64> TeamChatMessages { get; set; }

        public Nullable<Int64> PrivateChatMessages { get; set; }

        public Nullable<Int64> Calls { get; set; }

        public Nullable<Int64> Meetings { get; set; }

        public Nullable<Int64> OtherActions { get; set; }
    }

    /// <summary>
    /// Get details about Teams activity by user.
    /// </summary>
    [Table("GraphTeamsActivityUserDetail", Schema = "dbo")]
    public class EntityMSTeamsActivityUserDetail : EntityMSTeamsBase
    {
        public string UPN { get; set; }

        public string Deleted { get; set; }

        public DateTime? DeletedDate { get; set; }

        public DateTime LastActivityDate { get; set; }

        public Nullable<Int64> TeamChatMessageCount { get; set; }

        public Nullable<Int64> PrivateChatMessageCount { get; set; }

        public Nullable<Int64> CallCount { get; set; }

        public Nullable<Int64> MeetingCount { get; set; }

        public string HasOtherAction { get; set; }

        public string ProductsAssigned { get; set; }
    }

    /// <summary>
    /// Get the number of users using unique devices in your organization. The report will show you the number of users per device including Windows, Windows phone, Android phone, iPhone, and iPad.
    /// </summary>
    [Table("GraphTeamsDeviceUsageDistributionUserCounts", Schema = "dbo")]
    public class EntityMSTeamsDeviceUsageDistributionUserCounts : EntityMSTeamsBase
    {
        public Nullable<Int64> Web { get; set; }

        public Nullable<Int64> Windows { get; set; }

        public Nullable<Int64> WindowsPhone { get; set; }

        public Nullable<Int64> AndroidPhone { get; set; }

        public Nullable<Int64> iOS { get; set; }

        public Nullable<Int64> Mac { get; set; }
    }

    /// <summary>
    /// Get the usage trends on how many users in your organization have connected using the Teams app. You will also get a breakdown by the type of device (Web, Windows, Windows phone, Android phone, iOS, or Mac) on which the Skype for Business client app is installed and used across your organization.
    /// </summary>
    [Table("GraphTeamsDeviceUsageUserCounts", Schema = "dbo")]
    public class EntityMSTeamsDeviceUsageUserCounts : EntityMSTeamsCountBase
    {
        public Nullable<Int64> Web { get; set; }

        public Nullable<Int64> Windows { get; set; }

        public Nullable<Int64> WindowsPhone { get; set; }

        public Nullable<Int64> AndroidPhone { get; set; }

        public Nullable<Int64> iOS { get; set; }

        public Nullable<Int64> Mac { get; set; }
    }

    /// <summary>
    /// Get details about Teams device usage by user.
    /// </summary>
    [Table("GraphTeamsDeviceUsageUserDetail", Schema = "dbo")]
    public class EntityMSTeamsDeviceUsageUserDetail : EntityMSTeamsBase
    {
        public string UPN { get; set; }

        public DateTime? LastActivityDate { get; set; }

        public string Deleted { get; set; }

        public DateTime? DeletedDate { get; set; }

        public bool UsedWeb { get; set; }

        public DateTime? UsedWebLastDate { get; set; }

        public bool UsedWindows { get; set; }

        public DateTime? UsedWindowsLastDate { get; set; }

        public bool UsedWindowsPhone { get; set; }

        public DateTime? UsedWindowsPhoneLastDate { get; set; }

        public bool UsedAndroidPhone { get; set; }

        public DateTime? UsedAndroidPhoneLastDate { get; set; }

        public bool UsediOS { get; set; }

        public DateTime? UsediOSLastDate { get; set; }

        public bool UsedMac { get; set; }

        public DateTime? UsedMacLastDate { get; set; }
    }
}
