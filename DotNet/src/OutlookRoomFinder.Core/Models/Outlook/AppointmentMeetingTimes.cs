using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class AppointmentMeetingTimes
    {
        public ICollection<MeetingTimeWindow> Meetings { get; set; } = new List<MeetingTimeWindow>();

        public bool IsLastSet { get; set; }
    }
}
