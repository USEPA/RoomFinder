namespace OutlookRoomFinder.Core.Models.Outlook
{
    /// <summary>
    /// Represents the details of a calendar event as returned by the GetUserAvailability operation.
    /// </summary>
    public sealed class CalendarEventDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEventDetails"/> class.
        /// </summary>
        internal CalendarEventDetails()
        {
        }

        /// <summary>
        /// Gets the store Id of the calendar event.
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets the subject of the calendar event.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Gets the location of the calendar event.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets a value indicating whether the calendar event is a meeting.
        /// </summary>
        public bool IsMeeting { get; set; }

        /// <summary>
        /// Gets a value indicating whether the calendar event is recurring.
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Gets a value indicating whether the calendar event is an exception in a recurring series.
        /// </summary>
        public bool IsException { get; set; }

        /// <summary>
        /// Gets a value indicating whether the calendar event has a reminder set.
        /// </summary>
        public bool IsReminderSet { get; set; }

        /// <summary>
        /// Gets a value indicating whether the calendar event is private.
        /// </summary>
        public bool IsPrivate { get; set; }
    }
}