using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models
{
    public class ResourceListRoom : ADEntry
    {
        [JsonIgnore]
        public ICollection<IResourceItem> Rooms { get; set; } = Array.Empty<IResourceItem>();
    }
}