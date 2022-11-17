using System;

namespace OutlookRoomFinder.Core.Models
{
    public class TeleconferenceModel
    {
        public string Name { get; set; }

        public string PhoneNumber { get; set; }

        public string EmailAddress { get; set; }

        public DateTime ConferenceDate { get; set; }

        public DateTime ConferenceEnd { get; set; }

        public int? Attendees { get; set; }

        public string Purpose { get; set; }

        public string ConferenceType { get; set; }

        public string AudioInfo { get; set; }

        public string LocationOfHost { get; set; }

        public string Locations { get; set; }

        public string Comment { get; set; }
    }
}
