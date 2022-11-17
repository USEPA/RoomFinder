using Microsoft.Graph;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.Filter
{
    public class GraphScheduleValueResult : GraphOdata
    {
        [JsonProperty(PropertyName = "value")]
        public IEnumerable<ScheduleInformation> ScheduleValues { get; set; }
    }
}
