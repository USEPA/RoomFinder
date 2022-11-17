using System;

namespace OutlookRoomFinder.Core.Models
{
    public class AttendeeAvailabilityInfo
    {
        public string EmailAddress { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        /// <summary>
        /// Null = error, True = available, False = not available
        /// </summary>
        public bool? Status { get; set; }   
    }
}
