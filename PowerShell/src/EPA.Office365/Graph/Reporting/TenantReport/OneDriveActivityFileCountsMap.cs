﻿using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
The CSV file has the following headers for columns.
Report Refresh Date
Viewed Or Edited
Synced
Shared Internally
Shared Externally
Report Date
Report Period
 */
    internal class OneDriveActivityFileCountsMap : ClassMap<OneDriveActivityFileCounts>
    {
        internal OneDriveActivityFileCountsMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.FilesViewedModified).Name("Viewed Or Edited").Index(1).Default(0);
            Map(m => m.FilesSynced).Name("Synced").Index(2).Default(0);
            Map(m => m.FilesSharedINT).Name("Shared Internally").Index(3).Default(0);
            Map(m => m.FilesSharedEXT).Name("Shared Externally").Index(4).Default(0);
            Map(m => m.ReportDate).Name("Report Date").Index(5).Default(default(DateTime));
            Map(m => m.ReportPeriod).Name("Report Period").Index(6).Default(0);
        }
    }


    public class OneDriveActivityFileCounts : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("viewedOrEdited")]
        public Nullable<Int64> FilesViewedModified { get; set; }

        [JsonProperty("synced")]
        public Nullable<Int64> FilesSynced { get; set; }

        [JsonProperty("sharedInternally")]
        public Nullable<Int64> FilesSharedINT { get; set; }

        [JsonProperty("sharedExternally")]
        public Nullable<Int64> FilesSharedEXT { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }
    }
}
