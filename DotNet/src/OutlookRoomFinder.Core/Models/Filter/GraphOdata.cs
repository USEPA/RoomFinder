using Newtonsoft.Json;

namespace OutlookRoomFinder.Core.Models.Filter
{
    public class GraphOdata
    {
        [JsonProperty(PropertyName = "@odata.context")]
        public string OdataContext { get; set; }

        [JsonProperty(PropertyName = "@odata.nextLink")]
        public string OdataNextLink { get; set; }
    }
}
