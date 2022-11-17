using System.Collections.ObjectModel;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public interface IEquipmentJsonObject
    {
        string EquipmentType { get; set; }
        Collection<RestrictedDelegatesModel> RestrictedDelegates { get; set; }
    }
}