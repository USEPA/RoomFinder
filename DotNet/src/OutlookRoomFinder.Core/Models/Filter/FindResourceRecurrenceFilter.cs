using OutlookRoomFinder.Core.Models.Outlook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.Filter
{
    public class FindResourceRecurrenceFilter
    {
        [JsonProperty(Required = Required.Always, PropertyName = "setIndex")]
        public int SetIndex { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "setSize")]
        public int SetSize { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "attendees")]
        public ICollection<AttendeeInfo> Attendees { get; set; } = Array.Empty<AttendeeInfo>();

        public GraphRecurrencePattern RecurrencePattern { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "recurrence")]
        public GraphRecurrence Recurrence { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "roomFilter")]
        public FindResourceFilter RoomFilter { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
