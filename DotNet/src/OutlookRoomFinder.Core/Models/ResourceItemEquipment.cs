using Newtonsoft.Json;

namespace OutlookRoomFinder.Core.Models
{
    public class ResourceItemEquipment : ResourceItem, IResourceItem
    {
        public string EquipmentType { get; set; }

        [JsonIgnore]
        public ADEntry EquipmentList { get; set; }

        public override RestrictionType RestrictionType
        {
            get { return this.restrictionType; }
            set
            {
                this.restrictionType = value;
                switch (value)
                {
                    case RestrictionType.ApprovalRequired:
                        RestrictionImage = "EquipmentApprovalRequired.ico";
                        RestrictionTooltip = "Approval required";
                        break;
                    case RestrictionType.Restricted:
                        RestrictionImage = "EquipmentRestricted.ico";
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
                this.AvailabilityImage = "EquipmentAvailable.ico";
            }
            else
            {
                this.AvailabilityImage = "EquipmentBusy.ico";
            }
            return this;
        }

    }
}
