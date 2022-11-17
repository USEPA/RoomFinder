using Newtonsoft.Json;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class GraphRecurrence
    {
        [JsonProperty("recurrenceProperties", NullValueHandling = NullValueHandling.Ignore)]
        public GraphRecurrenceProperties RecurrenceProperties { get; set; }

        [JsonProperty("recurrenceType", NullValueHandling = NullValueHandling.Ignore)]
        public Ical.Net.FrequencyType RecurrenceType { get; set; }

        [JsonProperty("recurrenceTimeZone", NullValueHandling = NullValueHandling.Ignore)]
        public GraphRecurrenceTimeZone RecurrenceTimeZone { get; set; }

        [JsonProperty("seriesTime", NullValueHandling = NullValueHandling.Ignore)]
        public GraphRecurrenceSeriesTime SeriesTime { get; set; }
    }
}
