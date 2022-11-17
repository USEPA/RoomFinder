﻿using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * CSV Mapping
     * Report Refresh Date,Report Date,Report Period,,,,,
     * */
    internal class SkypeForBusinessPeerToPeerActivityCountsMap : ClassMap<SkypeForBusinessPeerToPeerActivityCounts>
    {
        internal SkypeForBusinessPeerToPeerActivityCountsMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.ReportDate).Name("Report Date").Index(1).Default(default(DateTime));
            Map(m => m.ReportPeriod).Name("Report Period").Index(2).Default(0);
            Map(m => m.IM).Name("IM").Index(3).Default(0);
            Map(m => m.Audio).Name("Audio").Index(4).Default(0);
            Map(m => m.Video).Name("Video").Index(5).Default(0);
            Map(m => m.AppSharing).Name("App Sharing").Index(6).Default(0);
            Map(m => m.FileTransfer).Name("File Transfer").Index(7).Default(0);
        }
    }

    /// <summary>
    /// Get usage trends on the number and type of sessions held in your organization. Types of sessions include IM, audio, video, application sharing, and file transfer.
    /// </summary>
    public class SkypeForBusinessPeerToPeerActivityCounts : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }

        [JsonProperty("im")]
        public Nullable<Int64> IM { get; set; }

        [JsonProperty("audio")]
        public Nullable<Int64> Audio { get; set; }

        [JsonProperty("video")]
        public Nullable<Int64> Video { get; set; }

        [JsonProperty("appSharing")]
        public Nullable<Int64> AppSharing { get; set; }

        [JsonProperty("fileTransfer")]
        public Nullable<Int64> FileTransfer { get; set; }


    }
}
