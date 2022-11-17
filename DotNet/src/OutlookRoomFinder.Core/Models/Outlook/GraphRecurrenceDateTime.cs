using Newtonsoft.Json;
using System;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    /// <summary>
    /// Represents a holistic datetime perspective for UTC to Local/Running system Timestamp
    /// </summary>
    public class GraphRecurrenceDateTime
    {
        /// <summary>
        /// Converts the date/time to this computer's local date/time.
        /// </summary>
        public DateTimeOffset AsSystemLocal { get; set; }
        /// <summary>
        /// Gets/sets the underlying DateTime value stored. This should always use DateTimeKind.Utc,
        ///     regardless of its actual representation. Use IsUtc along with the TZID to control
        ///     how this date/time is handled.
        /// </summary>
        [JsonProperty(PropertyName = "Value")]
        public DateTimeOffset AsActualDateTime { get; set; }
        /// <summary>
        /// Returns a DateTimeOffset representation of the Value. If a TzId is specified,
        ///     it will use that time zone's UTC offset, otherwise it will use the system-local
        ///     time zone.
        /// </summary>
        public DateTimeOffset AsDateTimeOffset { get; set; }
        /// <summary>
        /// Converts the date/time to UTC (Coordinated Universal Time)
        /// </summary>
        public DateTime AsUtc { get; set; }
        /// <summary>
        /// Gets the DayOfWeek for this date/time value.
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }
        /// <summary>
        /// Gets/sets whether or not this date/time value contains a 'date' part.
        /// </summary>
        public bool HasDate { get; set; }
        /// <summary>
        /// Gets/sets whether or not this date/time value contains a 'time' part.
        /// </summary>
        public bool HasTime { get; set; }
        /// <summary>
        /// Gets/sets the time zone ID for this date/time value.
        /// </summary>
        public string TzId { get; set; }
        /// <summary>
        /// Gets the year for this date/time value.
        /// </summary>
        public int Year { get; set; }
        /// <summary>
        /// Gets the ticks for this date/time value.
        /// </summary>
        public long Ticks { get; set; }
        /// <summary>
        /// Gets the Day of the Year 1-365
        /// </summary>
        public int DayOfYear { get; set; }
        /// <summary>
        /// Gets/sets whether the Value of this date/time represents a universal time.
        /// </summary>
        public bool IsUtc { get; set; }
        /// <summary>
        /// Gets the time zone name this time is in, if it references a time zone.
        /// </summary>
        public string TimeZoneName { get; set; }
    }

}
