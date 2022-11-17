using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * 
     * 
Report Refresh Date,
Visited Page Count,
Report Date,
Report Period
 */
    internal class SharePointActivityPagesMap : ClassMap<SharePointActivityPages>
    {
        internal SharePointActivityPagesMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.VisitedPageCount).Name("Visited Page Count").Index(1).Default(0);
            Map(m => m.ReportDate).Name("Report Date").Index(2).Default(default(DateTime));
            Map(m => m.ReportPeriod).Name("Report Period").Index(3).Default(0);
        }
    }


    public class SharePointActivityPages : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("visitedPageCount")]
        public Nullable<Int64> VisitedPageCount { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }
    }
}
