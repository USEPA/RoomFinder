﻿using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     *
The CSV file has the following headers for columns.

Report Refresh Date
Exchange Emails Received
Yammer Messages Posted
Yammer Messages Read
Yammer Messages Liked
Report Date
Report Period
     */
    internal class Office365GroupsActivityCountsMap : ClassMap<Office365GroupsActivityCounts>
    {
        internal Office365GroupsActivityCountsMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.ExchangeReceivedEmailCount).Name("Exchange Emails Received").Index(1).Default(0);
            Map(m => m.YammerPostedMessageCount).Name("Yammer Messages Posted").Index(2).Default(0);
            Map(m => m.YammerReadMessageCount).Name("Yammer Messages Read").Index(3).Default(0);
            Map(m => m.YammerLikedMessageCount).Name("Yammer Messages Liked").Index(4).Default(0);
            Map(m => m.ReportDate).Name("Report Date").Index(5).Default(default(DateTime));
            Map(m => m.ReportPeriod).Name("Report Period").Index(6).Default(string.Empty);
        }
    }



    public class Office365GroupsActivityCounts : JSONODataBase
    {

        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("exchangeEmailsReceived")]
        public Nullable<Int64> ExchangeReceivedEmailCount { get; set; }

        [JsonProperty("yammerMessagesPosted")]
        public Nullable<Int64> YammerPostedMessageCount { get; set; }

        [JsonProperty("yammerMessagesRead")]
        public Nullable<Int64> YammerReadMessageCount { get; set; }

        [JsonProperty("yammerMessagesLiked")]
        public Nullable<Int64> YammerLikedMessageCount { get; set; }

        [JsonProperty("reportDate")]
        public DateTime ReportDate { get; set; }

        [JsonProperty("reportPeriod")]
        public string ReportPeriod { get; set; }
    }
}
