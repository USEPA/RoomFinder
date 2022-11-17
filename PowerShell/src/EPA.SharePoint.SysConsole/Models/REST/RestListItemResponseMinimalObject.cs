using Newtonsoft.Json;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.REST
{
    public class RestListItemResponseMinimalObject<T> where T : IRestListItemObj
    {
        [JsonProperty("odata.metadata")]
        public string Metadata { get; set; }

        [JsonProperty("odata.nextLink")]
        public string NextLink { get; set; }

        public List<T> value { get; set; }
    }
}
