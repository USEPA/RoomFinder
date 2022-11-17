﻿using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * CSV Mapping
     * Report Refresh Date,Report Date,Report Period,,,,,
     * */
    internal class SkypeForBusinessParticipantActivityMinuteCountsMap : ClassMap<SkypeForBusinessParticipantActivityMinuteCounts>
    {
        internal SkypeForBusinessParticipantActivityMinuteCountsMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.ReportDate).Name("Report Date").Index(1).Default(default(DateTime));
            Map(m => m.ReportPeriod).Name("Report Period").Index(2).Default(0);
            Map(m => m.AudioVideo).Name("Audio/Video").Index(3).Default(0);
        }
    }

    /// <summary>
    /// Get usage trends on the length in minutes and type of conference sessions that users from your organization participated in. Types of conference sessions include audio/video.
    /// </summary>
    public class SkypeForBusinessParticipantActivityMinuteCounts : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }

        [JsonProperty("audioVideo")]
        public Nullable<Int64> AudioVideo { get; set; }
    }
}
