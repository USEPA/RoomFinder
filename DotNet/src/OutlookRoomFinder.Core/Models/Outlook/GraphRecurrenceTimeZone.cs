using Newtonsoft.Json;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class GraphRecurrenceTimeZone
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("offset", NullValueHandling = NullValueHandling.Ignore)]
        public long Offset { get; set; }
    }
}
