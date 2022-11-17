using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models
{
    public class ResourceListing<T> : ADEntry where T : IResourceItem
    {
        [JsonIgnore]
        public ICollection<T> Resources { get; set; } = Array.Empty<T>();
    }
}
