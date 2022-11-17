using Newtonsoft.Json;
using OutlookRoomFinder.Core.Models.FileModels;
using OutlookRoomFinder.Core.Models.Outlook;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models
{
    public class ResourceItemMailbox : ResourceItem, IResourceItem
    {
        public int Capacity { get; set; }
        
        public override MeetingAttendeeType EntryType
        {
            get
            {
                return MeetingAttendeeType.Room;
            }
        }

        [JsonIgnore]
        public ADEntry RoomList { get; set; }

        [JsonIgnore]
        public ICollection<string> Equipment { get; set; } = new List<string>();
        public ICollection<EquipmentModel> EquipmentDependencies { get; set; } = Array.Empty<EquipmentModel>();


        public override RestrictionType RestrictionType
        {
            get { return this.restrictionType; }
            set
            {
                this.restrictionType = value;
                switch (value)
                {
                    case RestrictionType.ApprovalRequired:
                        RestrictionImage = "RoomApprovalRequired.ico";
                        RestrictionTooltip = "Approval required";
                        break;
                    case RestrictionType.Restricted:
                        RestrictionImage = "RoomRestricted.ico";
                        RestrictionTooltip = "Restricted";
                        break;
                    default:
                        RestrictionImage = "blank.ico";
                        RestrictionTooltip = "Unrestricted";
                        break;
                }
            }
        }

        public override IResourceItem UpdateAvailabilityStatus(bool? roomIsAvailable)
        {
            this.Status = roomIsAvailable;
            if (!roomIsAvailable.HasValue)
            {
                this.AvailabilityImage = "ItemError.ico";
            }
            else if (roomIsAvailable == true)
            {
                this.AvailabilityImage = "RoomAvailable.ico";
            }
            else
            {
                this.AvailabilityImage = "RoomBusy.ico";
            }

            return this;
        }

    }
}