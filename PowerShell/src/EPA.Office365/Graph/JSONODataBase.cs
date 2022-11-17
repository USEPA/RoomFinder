using Newtonsoft.Json;

namespace EPA.Office365.Graph
{
    public class JSONODataBase
    {
        /// <summary>
        /// Represents the OData API data type
        /// </summary>
        [JsonProperty("@odata.type")]
        public string ODataType { get; set; }
    }
}
