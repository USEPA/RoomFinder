using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * CSV Mapping for Teams app
     * Report Refresh Date,,,,,,,,Report Period
     * */
    internal class MSTeamsDeviceUsageUserCountsMap : ClassMap<MSTeamsDeviceUsageUserCounts>
    {
        internal MSTeamsDeviceUsageUserCountsMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.Web).Name("Web").Index(1).Default(0);
            Map(m => m.WindowsPhone).Name("Windows Phone").Index(2).Default(0);
            Map(m => m.AndroidPhone).Name("Android Phone").Index(3).Default(0);
            Map(m => m.iOS).Name("iOS").Index(4).Default(0);
            Map(m => m.Mac).Name("Mac").Index(5).Default(0);
            Map(m => m.Windows).Name("Windows").Index(6).Default(0);
            Map(m => m.ReportDate).Name("Report Date").Index(7).Default(default(DateTime));
            Map(m => m.ReportPeriod).Name("Report Period").Index(8).Default(0);
        }
    }

    /// <summary>
    /// Get the usage trends on how many users in your organization have connected using the Teams app. You will also get a breakdown by the type of device (Web, Windows, Windows phone, Android phone, iOS, or Mac) on which the Teams client app is installed and used across your organization.
    /// </summary>
    public class MSTeamsDeviceUsageUserCounts : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }

        [JsonProperty("windows")]
        public Nullable<Int64> Windows { get; set; }

        [JsonProperty("windowsPhone")]
        public Nullable<Int64> WindowsPhone { get; set; }

        [JsonProperty("androidPhone")]
        public Nullable<Int64> AndroidPhone { get; set; }

        [JsonProperty("iOS")]
        public Nullable<Int64> iOS { get; set; }

        [JsonProperty("mac")]
        public Nullable<Int64> Mac { get; set; }

        [JsonProperty("web")]
        public Nullable<Int64> Web { get; set; }
    }
}
