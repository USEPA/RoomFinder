using Newtonsoft.Json;
using System.Collections.Generic;

namespace EPA.Office365.Graph
{
    public class JSONAuditCollection<T> : GraphODataResult where T : class
    {
        /// <summary>
        /// initilize collections
        /// </summary>
        public JSONAuditCollection()
        {
            this.Results = new List<T>();
        }

        /// <summary>
        /// Serializable collection of auditiable events
        /// </summary>
        [JsonProperty("value")]
        public List<T> Results { get; set; }
    }
}
