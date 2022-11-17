using Newtonsoft.Json;

namespace EPA.Office365.Graph
{
    public class GraphODataResult
    {
        /// <summary>
        /// Represents the metadata regarding the ODAta API service
        /// </summary>
        [JsonProperty("@odata.context")]
        public string Metadata { get; set; }

        /// <summary>
        /// Provides the next ODATA Paging link
        /// </summary>
        [JsonProperty("@odata.nextLink")]
        public string NextLink { get; set; }
    }
}
