using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public class LocalJsonModel : ILocalJsonModel
    {
        /// <summary>
        /// Equipment Attributes
        /// </summary>
        public ICollection<string> Equipment { get; set; }

        /// <summary>
        /// Equipment Categories
        /// </summary>
        public ICollection<string> EquipmentList { get; set; }

        /// <summary>
        /// Locations for Rooms
        /// </summary>
        public ICollection<ResourceJsonObject> Locations { get; set; }

        /// <summary>
        /// Collection of Mailbox to provision
        /// </summary>
        public ICollection<MailboxJsonObject> Mailboxes { get; set; }

        /// <summary>
        /// Collection of Equipment to provision
        /// </summary>
        public ICollection<EquipmentJsonObject> Equipments { get; set; }
    }
}
