using Newtonsoft.Json;

namespace OutlookRoomFinder.Web.Models
{
    public class ReportUsageModel
    {
        public ReportUsageModel() { }

        [JsonProperty(PropertyName = "reportDurationType")]
        public string ReportDurationType { get; set; }
    }
}
