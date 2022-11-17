using Newtonsoft.Json;
using System.Collections.Generic;

namespace OutlookRoomFinder.Web.Models
{
    public class ReportRoomModel : ReportCustomModel
    {
        [JsonProperty(PropertyName = "emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty(PropertyName = "resources")]
        public List<string> Resources { get; set; }
    }
}
