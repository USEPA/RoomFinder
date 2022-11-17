using Newtonsoft.Json;
using OutlookRoomFinder.Core.Models.Outlook;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models
{
    public abstract class ResourceItem : ADEntry, IResourceItem
    {
        [JsonIgnore]
        public LocationAddress Location { get; set; } = new LocationAddress();

        public string AvailabilityImage { get; set; }
        public string RestrictionImage { get; set; }
        public string RestrictionTooltip { get; set; }

        internal RestrictionType restrictionType = RestrictionType.None;
        public virtual RestrictionType RestrictionType
        {
            get { return this.restrictionType; }
            set
            {
                this.restrictionType = value;
                switch (value)
                {
                    case RestrictionType.ApprovalRequired:
                        RestrictionImage = "ApprovalRequired.ico";
                        RestrictionTooltip = "Approval required";
                        break;
                    case RestrictionType.Restricted:
                        RestrictionImage = "Restricted.ico";
                        RestrictionTooltip = "Restricted";
                        break;
                    default:
                        RestrictionImage = "blank.ico";
                        RestrictionTooltip = "Unrestricted";
                        break;
                }
            }
        }

        public virtual MeetingAttendeeType EntryType
        {
            get
            {
                return MeetingAttendeeType.Resource;
            }
        }

        public bool? Status { get; set; }

        public abstract IResourceItem UpdateAvailabilityStatus(bool? roomIsAvailable);

        public ICollection<string> Dependencies { get; set; } = Array.Empty<string>();
    }
}
