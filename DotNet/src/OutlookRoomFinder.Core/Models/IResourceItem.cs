using OutlookRoomFinder.Core.Models.Outlook;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models
{
    public interface IResourceItem : IADEntry
    {
        string AvailabilityImage { get; set; }
        string RestrictionImage { get; set; }
        string RestrictionTooltip { get; set; }
        RestrictionType RestrictionType { get; set; }
        bool? Status { get; set; }
        LocationAddress Location { get; set; }

        /// <summary>
        /// Override the ICO presented to the enduser based on the status
        /// </summary>
        /// <param name="roomIsAvailable"></param>
        /// <returns></returns>
        IResourceItem UpdateAvailabilityStatus(bool? roomIsAvailable);

        ICollection<string> Dependencies { get; set; }

        MeetingAttendeeType EntryType { get; }
    }
}