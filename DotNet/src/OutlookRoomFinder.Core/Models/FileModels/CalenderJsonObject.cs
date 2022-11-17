using Microsoft.Graph;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public class CalenderJsonObject
    {
        public ICollection<string> Schedules { get; set; }
        public DateTimeTimeZone StartTime { get; set; }
        public DateTimeTimeZone Endtime { get; set; }
        public string AvailabilityViewInterval { get; set; }

    }
}
