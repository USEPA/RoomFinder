﻿using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * 
Report Refresh Date,
Visited Page,
Viewed Or Edited,
Synced,
Shared Internally,
Shared Externally,
Report Date,
Report Period
2017-10-28,372,361,25,9,,2017-10-28,7
 */
    internal class SharePointActivityUserCountsMap : ClassMap<SharePointActivityUserCounts>
    {
        internal SharePointActivityUserCountsMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.VisitedPage).Name("Visited Page").Index(1).Default(0);
            Map(m => m.ViewedOrEdited).Name("Viewed Or Edited").Index(2).Default(0);
            Map(m => m.Synced).Name("Synced").Index(3).Default(0);
            Map(m => m.SharedInternally).Name("Shared Internally").Index(4).Default(0);
            Map(m => m.SharedExternally).Name("Shared Externally").Index(5).Default(0);
            Map(m => m.ReportDate).Name("Report Date").Index(6).Default(default(DateTime));
            Map(m => m.ReportPeriod).Name("Report Period").Index(7).Default(0);
        }
    }


    public class SharePointActivityUserCounts : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("visitedPage")]
        public Nullable<Int64> VisitedPage { get; set; }

        [JsonProperty("viewedOrEdited")]
        public Nullable<Int64> ViewedOrEdited { get; set; }

        [JsonProperty("synced")]
        public Nullable<Int64> Synced { get; set; }

        [JsonProperty("sharedInternally")]
        public Nullable<Int64> SharedInternally { get; set; }

        [JsonProperty("sharedExternally")]
        public Nullable<Int64> SharedExternally { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }
    }
}
