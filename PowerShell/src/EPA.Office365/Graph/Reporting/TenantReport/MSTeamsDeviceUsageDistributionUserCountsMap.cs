using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * CSV Mapping for Device usage
     * Report Refresh Date,,,,,,,,Report Period
     * */
    internal class MSTeamsDeviceUsageDistributionUserCountsMap : ClassMap<MSTeamsDeviceUsageDistributionUserCounts>
    {
        internal MSTeamsDeviceUsageDistributionUserCountsMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.Web).Name("Web").Index(1).Default(0);
            Map(m => m.WindowsPhone).Name("Windows Phone").Index(2).Default(0);
            Map(m => m.AndroidPhone).Name("Android Phone").Index(3).Default(0);
            Map(m => m.iOS).Name("iOS").Index(4).Default(0);
            Map(m => m.Mac).Name("Mac").Index(5).Default(0);
            Map(m => m.Windows).Name("Windows").Index(6).Default(0);
            Map(m => m.ReportPeriod).Name("Report Period").Index(7).Default(0);
        }
    }

    /// <summary>
    /// Get the number of users using unique devices in your organization. The report will show you the number of users per device including Windows, Windows phone, Android phone, iPhone, and iPad.
    /// </summary>
    public class MSTeamsDeviceUsageDistributionUserCounts : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

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
