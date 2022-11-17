using Newtonsoft.Json;
using System;

namespace EPA.SharePoint.SysConsole.Models.REST
{
    public class RestMetaDataObject
    {
        public Guid id { get; set; }

        public string uri { get; set; }

        public string etag { get; set; }

        [JsonProperty("type")]
        public string resttype { get; set; }
    }
}
