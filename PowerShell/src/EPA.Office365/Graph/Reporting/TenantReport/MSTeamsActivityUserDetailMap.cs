using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
     * CSV Mapping for User activity
     * Report Refresh Date,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,Report Period
     * */
    internal class MSTeamsActivityUserDetailMap : ClassMap<MSTeamsActivityUserDetail>
    {
        internal MSTeamsActivityUserDetailMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.UPN).Name("User Principal Name").Index(1).Default(string.Empty);
            Map(m => m.LastActivityDate).Name("Last Activity Date").Index(2).Default(default(DateTime));
            Map(m => m.Deleted).Name("Is Deleted").Index(3).Default("false");
            Map(m => m.DeletedDate).Name("Deleted Date").Index(4).Default(default(DateTime?));
            Map(m => m.ProductsAssignedCSV).Name("Assigned Products").Index(5).Default(string.Empty);
            Map(m => m.TeamChatMessageCount).Name("Team Chat Message Count").Index(6).Default(0);
            Map(m => m.PrivateChatMessageCount).Name("Private Chat Message Count").Index(7).Default(0);
            Map(m => m.CallCount).Name("Call Count").Index(8).Default(0);
            Map(m => m.MeetingCount).Name("Meeting Count").Index(9).Default(0);
            Map(m => m.HasOtherAction).Name("Has Other Action").Index(10).Default(string.Empty);
            Map(m => m.ReportPeriod).Name("Report Period").Index(34).Default(0);
        }
    }


    /// <summary>
    /// Get details about Teams activity by user.
    /// </summary>
    public class MSTeamsActivityUserDetail : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("reportPeriod")]
        public int ReportPeriod { get; set; }

        [JsonProperty("userPrincipalName")]
        public string UPN { get; set; }

        [JsonProperty("isDeleted")]
        public string Deleted { get; set; }

        [JsonProperty("deletedDate")]
        public DateTime? DeletedDate { get; set; }

        [JsonProperty("lastActivityDate")]
        public DateTime LastActivityDate { get; set; }

        [JsonProperty("teamChatMessageCount")]
        public Nullable<Int64> TeamChatMessageCount { get; set; }

        [JsonProperty("privateChatMessageCount")]
        public Nullable<Int64> PrivateChatMessageCount { get; set; }

        [JsonProperty("callCount")]
        public Nullable<Int64> CallCount { get; set; }

        [JsonProperty("meetingCount")]
        public Nullable<Int64> MeetingCount { get; set; }

        [JsonProperty("hasOtherAction")]
        public string HasOtherAction { get; set; }

        [JsonProperty("assignedProducts")]
        public IEnumerable<string> ProductsAssigned { get; set; }

        [JsonIgnore()]
        public string ProductsAssignedCSV { get; set; }

        /// <summary>
        /// Process the CSV or Array into a Delimited string
        /// </summary>
        [JsonIgnore()]
        public string RealizedProductsAssigned
        {
            get
            {
                var _productsAssigned = string.Empty;
                if (ProductsAssigned != null)
                {
                    _productsAssigned = string.Join(",", ProductsAssigned);
                }
                else if (!string.IsNullOrEmpty(ProductsAssignedCSV))
                {
                    _productsAssigned = ProductsAssignedCSV.Replace("+", ",");
                }

                return _productsAssigned;
            }
        }

    }
}