using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.Filter
{
    public class FindResourceFilter
    {
        [JsonProperty(PropertyName = "includeUnavailable")]
        public bool IncludeUnavailable { get; set; }

        [JsonProperty(PropertyName = "includeRestricted")]
        public bool IncludeRestricted { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "start")]
        public string Start { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "end")]
        public string End { get; set; }

        [JsonProperty(PropertyName = "capacity")]
        public int? Capacity { get; set; } = 0;

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [JsonProperty(PropertyName = "office")]
        public string Office { get; set; }

        [JsonProperty(PropertyName = "floor")]
        public string Floor { get; set; }

        [JsonProperty(PropertyName = "listPath")]
        public string ListPath { get; set; }

        [JsonProperty(PropertyName = "requiredEquipment")]
        public ICollection<string> RequiredEquipment { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
