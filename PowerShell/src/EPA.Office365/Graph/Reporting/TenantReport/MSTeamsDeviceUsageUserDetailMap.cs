using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * CSV Mapping for User activity
     * Report Refresh Date,,,,,,,,Report Period
     * */
    internal class MSTeamsDeviceUsageUserDetailMap : ClassMap<MSTeamsDeviceUsageUserDetail>
    {
        internal MSTeamsDeviceUsageUserDetailMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.UPN).Name("User Principal Name").Index(1).Default(string.Empty);
            Map(m => m.LastActivityDate).Name("Last Activity Date").Index(2).Default(default(DateTime));
            Map(m => m.Deleted).Name("Is Deleted").Index(3).Default("false");
            Map(m => m.DeletedDate).Name("Deleted Date").Index(4).Default(default(DateTime?));
            Map(m => m.UsedWeb).Name("Used Web").Index(3).Default("No");
            Map(m => m.UsedWindowsPhone).Name("Used Windows Phone").Index(4).Default("No");
            Map(m => m.UsediOS).Name("Used iOS").Index(5).Default("No");
            Map(m => m.UsedMac).Name("Used Mac").Index(6).Default("No");
            Map(m => m.UsedAndroidPhone).Name("Used Android Phone").Index(7).Default("No");
            Map(m => m.UsedWindows).Name("Used Windows").Index(8).Default("No");
            Map(m => m.ReportPeriod).Name("Report Period").Index(9).Default(0);
        }
    }


    /// <summary>
    /// Get details about Teams device usage by user.
    /// </summary>
    public class MSTeamsDeviceUsageUserDetail : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("userPrincipalName")]
        public string UPN { get; set; }

        [JsonProperty("lastActivityDate")]
        public DateTime? LastActivityDate { get; set; }

        [JsonProperty("isDeleted")]
        public string Deleted { get; set; }

        [JsonProperty("deletedDate")]
        public DateTime? DeletedDate { get; set; }

        [JsonProperty("usedWeb")]
        public string UsedWeb { get; set; }

        [JsonProperty("usedWindowsPhone")]
        public string UsedWindowsPhone { get; set; }

        [JsonProperty("usediOS")]
        public string UsediOS { get; set; }

        [JsonProperty("usedMac")]
        public string UsedMac { get; set; }

        [JsonProperty("usedAndroidPhone")]
        public string UsedAndroidPhone { get; set; }

        [JsonProperty("usedWindows")]
        public string UsedWindows { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }

    }
}
