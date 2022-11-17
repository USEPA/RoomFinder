using System.Collections.Generic;
using System.ComponentModel;

namespace OutlookRoomFinder.Core.Models
{
    public class AnalyticsRoom
    {
        [DisplayName("Meeting Room")]
        public string RoomName { get; set; }

        [DisplayName("Number of Meetings")]
        public int NumberOfMeetings { get; set; }

        public ICollection<CalendarEventViewModel> Meetings { get; set; } = new List<CalendarEventViewModel>(); 

        public string SmtpAddress { get; set; }
    }
}