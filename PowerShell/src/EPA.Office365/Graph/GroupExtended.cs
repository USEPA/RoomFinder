using Microsoft.Graph;
using Newtonsoft.Json;

namespace EPA.Office365.Graph
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>")]
    public class GroupExtended : Group
    {
        [JsonProperty("owners@odata.bind", NullValueHandling = NullValueHandling.Ignore)]
        public string[] OwnersODataBind { get; set; }

        [JsonProperty("members@odata.bind", NullValueHandling = NullValueHandling.Ignore)]
        public string[] MembersODataBind { get; set; }
    }
}
