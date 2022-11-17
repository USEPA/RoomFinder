using Newtonsoft.Json;
using System;

namespace OutlookRoomFinder.Web.Models
{
    public class ReportCustomModel
    {
        [JsonProperty(PropertyName = "startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty(PropertyName = "endDate")]
        public DateTime EndDate { get; set; }
    }
}