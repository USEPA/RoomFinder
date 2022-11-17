using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public class EquipmentJsonObject : ResourceJsonObject, IEquipmentJsonObject
    {
        [JsonProperty(PropertyName = "equipmentType")]
        public string EquipmentType { get; set; }

        public Collection<RestrictedDelegatesModel> RestrictedDelegates { get; set; } = new Collection<RestrictedDelegatesModel>();
    }
}