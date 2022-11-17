using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EPA.Office365.oAuth
{
    public class AppSettingsExchange
    {
        public int MaxLogFileSize { get; set; }

        public int CacheLifetime { get; set; }

        public int DefaultRoomCapacity { get; set; }

        public int GetUserAvailabilityBatchSize { get; set; }

        [JsonProperty(PropertyName = "EWSUrl")]
        public string EwsUrl { get; set; }

        [JsonProperty(PropertyName = "EWSLogin")]
        public string EwsLogin { get; set; }

        [JsonProperty(PropertyName = "EWSPassword")]
        public string EwsPassword { get; set; }

        public string JsonFilename { get; set; }
    }
}
