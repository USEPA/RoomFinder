using Newtonsoft.Json;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class GraphRecurrencePatternParameters
    {
        [JsonProperty(PropertyName = "Value")]
        public string TimeZone { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string TimeZoneId { get; set; }
    }
}
