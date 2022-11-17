using System;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    /// <summary>
    /// Represents an event in a calendar.
    /// </summary>
    public sealed class CalendarEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEvent"/> class.
        /// </summary>
        internal CalendarEvent()
        {
        }

        public CalendarEvent(DateTime startTime, DateTime endTime)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        /// <summary>
        /// Gets the start date and time of the event.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets the end date and time of the event.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the free/busy status associated with the event.
        /// </summary>
        public LegacyFreeBusyStatus FreeBusyStatus { get; set; }

        /// <summary>
        /// Gets the details of the calendar event. Details is null if the user
        /// requsting them does no have the appropriate rights.
        /// </summary>
        public CalendarEventDetails Details { get; set; }
    }
}