using System.ComponentModel;

namespace OutlookRoomFinder.Core.Models
{
    public class AnalyticsEquipment
    {
        [DisplayName("Equipment")]
        public string EquipmentName { get; set; }

        [DisplayName("Number of Meetings")]
        public int NumberOfMeetings { get; set; }
    }
}