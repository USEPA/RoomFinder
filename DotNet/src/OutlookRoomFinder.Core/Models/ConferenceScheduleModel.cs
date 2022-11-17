using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OutlookRoomFinder.Core.Models
{
    public class ConferenceScheduleModel
    {
        public ConferenceScheduleModel()
        {
        }

        public ConferenceScheduleModel(string smtp)
            : this()
        {
            EmailAddress = smtp;
        }

        public string EmailAddress { get; set; }

        public ICollection<CalendarEventViewModel> Events { get; set; } = Array.Empty<CalendarEventViewModel>();

        public int? UsageByRoom
        {
            get
            {
                if (Events?.Any(wf => wf.BusyStatus == FreeBusyStatus.Busy) == true)
                {
                    return Events.Count(wf => wf.BusyStatus == FreeBusyStatus.Busy);
                }
                return 0;
            }
        }
    }
}
