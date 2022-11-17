using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class GraphRecurrenceProperties
    {
        /// <summary>
        /// Represents the period between instances of the same recurring series.
        /// </summary>
        [JsonProperty("interval", NullValueHandling = NullValueHandling.Ignore)]
        public int Interval { get; set; }

        /// <summary>
        /// Represents the day of the month.
        /// </summary>
        [JsonProperty("dayOfMonth", NullValueHandling = NullValueHandling.Ignore)]
        public int? DayOfMonth { get; set; }

        /// <summary>
        /// Represents the day of the week or type of day, for example, weekend day vs weekday.
        /// </summary>
        [JsonProperty("dayOfWeek", NullValueHandling = NullValueHandling.Ignore)]
        public string DayOfWeek { get; set; }

        /// <summary>
        /// Represents the number of the week in the selected month e.g. 'first' for first week of the month.
        /// </summary>
        [JsonProperty("weekNumber", NullValueHandling = NullValueHandling.Ignore)]
        public Ical.Net.FrequencyOccurrence? WeekNumber { get; set; }

        /// <summary>
        /// Represents the month.
        /// </summary>
        [JsonProperty("month", NullValueHandling = NullValueHandling.Ignore)]
        public string Month { get; set; }

        /// <summary>
        /// Represents the set of days for this recurrence. Valid values are: 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', and 'Sun'.
        /// </summary>
        [JsonProperty("days", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<string> Days { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Represents your chosen first day of the week otherwise the default is the value in the current user's settings. Valid values are: 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', and 'Sun'.
        /// </summary>
        [JsonProperty("firstDayOfWeek", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstDayOfWeek { get; set; }
    }

}
