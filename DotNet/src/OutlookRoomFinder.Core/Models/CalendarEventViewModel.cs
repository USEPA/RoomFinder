using Microsoft.Graph;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OutlookRoomFinder.Core.Models
{
    public class CalendarEventViewModel
    {
        public string MailboxId { get; set; }
        public string Location { get; set; }
        public string LocationEmail { get; set; }

        [DisplayName("Organizer")]
        public string OrganizerName { get; set; }
        public string OrganizerEmail { get; set; }

        public string Subject { get; set; }

        public DateTimeOffset? OriginalStart { get; internal set; }
        [DisplayName("Start Time")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy hh:mm tt}")]
        public DateTime StartTime { get; set; }

        [DisplayName("End Time")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy hh:mm tt}")]
        public DateTime EndTime { get; set; }

        [DisplayName("Start Time")]
        public string StartTimeString => string.Format("{0:MM/dd/yyyy hh:mm tt}", StartTime);

        [DisplayName("End Time")]
        public string EndTimeString => string.Format("{0:MM/dd/yyyy hh:mm tt}", EndTime);

        public bool? IsMeeting { get; set; }
        public bool? IsOnlineMeeting { get; set; }
        public bool? IsAllDayEvent { get; set; }
        public bool? IsRecurring { get; set; }
        public bool? IsCancelled { get; set; }
        public string TimeZone { get; set; }
        public FreeBusyStatus? ShowAs { get; internal set; }
        public FreeBusyStatus BusyStatus { get; internal set; }

        /// <summary>
        /// Null = error, True = available, False = not available
        /// </summary>
        public bool? Status { get; set; }
    }
}