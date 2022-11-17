using OutlookRoomFinder.Core.Models.Outlook;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.Filter
{
    public class GetAvailabilityResponseFilter
    {
        public bool IsLastSet { get; set; }

        public ICollection<AttendeeAvailabilityInfo> Data { get; set; } = Array.Empty<AttendeeAvailabilityInfo>();

        public GraphRecurrencePattern RecurrencePattern { get; set; }
    }
}
