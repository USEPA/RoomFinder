using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * CSV mapping for v1.0 and beta endpoints
     * Report Refresh Date,Report Date,Report Period,Peer-to-peer,Organized,Participated
     */
    internal class MSTeamsActivityActivityCountsMap : ClassMap<MSTeamsActivityActivityCounts>
    {
        internal MSTeamsActivityActivityCountsMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.ReportDate).Name("Report Date").Index(1).Default(default(DateTime));
            Map(m => m.TeamChatMessages).Name("Team Chat Messages").Index(2).Default(0);
            Map(m => m.PrivateChatMessages).Name("Private Chat Messages").Index(3).Default(0);
            Map(m => m.Calls).Name("Calls").Index(4).Default(0);
            Map(m => m.Meetings).Name("Meetings").Index(5).Default(0);
            Map(m => m.ReportPeriod).Name("Report Period").Index(6).Default(0);
        }
    }

    /// <summary>
    /// Get the trends on how many users participate in Teams.
    /// </summary>
    public class MSTeamsActivityActivityCounts : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }

        [JsonProperty("teamChatMessages")]
        public Nullable<Int64> TeamChatMessages { get; set; }

        [JsonProperty("privateChatMessages")]
        public Nullable<Int64> PrivateChatMessages { get; set; }

        [JsonProperty("calls")]
        public Nullable<Int64> Calls { get; set; }

        [JsonProperty("meetings")]
        public Nullable<Int64> Meetings { get; set; }
    }
}
