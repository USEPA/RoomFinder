using Microsoft.Graph;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.Filter
{
    public class GraphOutlookItemValueResult : GraphOdata
    {
        [JsonProperty(PropertyName = "value")]
        public IEnumerable<Event> CalendarEvents { get; set; }
    }
}
