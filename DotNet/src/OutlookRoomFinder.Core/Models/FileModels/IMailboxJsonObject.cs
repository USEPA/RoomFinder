using System.Collections.ObjectModel;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public interface IMailboxJsonObject
    {
        Collection<string> Equipment { get; set; }
        Collection<RestrictedDelegatesModel> RestrictedDelegates { get; set; }
    }
}