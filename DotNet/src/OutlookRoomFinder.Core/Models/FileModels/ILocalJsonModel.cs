using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public interface ILocalJsonModel
    {
        ICollection<string> Equipment { get; set; }
        ICollection<string> EquipmentList { get; set; }
        ICollection<EquipmentJsonObject> Equipments { get; set; }
        ICollection<ResourceJsonObject> Locations { get; set; }
        ICollection<MailboxJsonObject> Mailboxes { get; set; }
    }
}