using Newtonsoft.Json;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class GraphRecurrenceSeriesTime
    {
        [JsonProperty("startYear", NullValueHandling = NullValueHandling.Ignore)]
        public int StartYear { get; set; }

        [JsonProperty("startMonth", NullValueHandling = NullValueHandling.Ignore)]
        public int StartMonth { get; set; }

        [JsonProperty("startDay", NullValueHandling = NullValueHandling.Ignore)]
        public int StartDay { get; set; }

        [JsonProperty("endYear", NullValueHandling = NullValueHandling.Ignore)]
        public int EndYear { get; set; }

        [JsonProperty("endMonth", NullValueHandling = NullValueHandling.Ignore)]
        public int EndMonth { get; set; }

        [JsonProperty("endDay", NullValueHandling = NullValueHandling.Ignore)]
        public int EndDay { get; set; }

        [JsonProperty("startTimeMinutes", NullValueHandling = NullValueHandling.Ignore)]
        public int StartTimeMinutes { get; set; }

        [JsonProperty("durationMinutes", NullValueHandling = NullValueHandling.Ignore)]
        public int DurationMinutes { get; set; }
    }
}
