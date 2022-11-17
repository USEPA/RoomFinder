﻿using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * 
Report Refresh Date,
Site Type,
Storage Used (Byte),
Report Date,
Report Period
2017-10-29,All,6488883224233,2017-10-29,7
 */
    internal class SharePointSiteUsageStorageMap : ClassMap<SharePointSiteUsageStorage>
    {
        internal SharePointSiteUsageStorageMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.SiteType).Name("Site Type").Index(1).Default(string.Empty);
            Map(m => m.StorageUsed_Byte).Name("Storage Used (Byte)").Index(2).Default(0);
            Map(m => m.ReportDate).Name("Report Date").Index(3).Default(default(DateTime));
            Map(m => m.ReportPeriod).Name("Report Period").Index(4).Default(0);
        }
    }


    public class SharePointSiteUsageStorage : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("siteType")]
        public string SiteType { get; set; }

        [JsonProperty("storageUsedInBytes")]
        public Nullable<Int64> StorageUsed_Byte { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }
    }
}