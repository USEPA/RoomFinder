using System.Collections.ObjectModel;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public class MailboxJsonObject : ResourceJsonObject, IMailboxJsonObject
    {
        public Collection<RestrictedDelegatesModel> RestrictedDelegates { get; set; }

        public Collection<string> Dependencies { get; set; } = new Collection<string>();

        public Collection<string> Equipment { get; set; } = new Collection<string>();

        public Collection<string> EquipmentDependencies { get; set; } = new Collection<string>();
    }
}
