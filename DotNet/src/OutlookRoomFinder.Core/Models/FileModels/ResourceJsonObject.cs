using System.Collections.ObjectModel;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public class ResourceJsonObject : IResourceJsonObject
    {
        public string SamAccountName { get; set; }

        public string UserPrincipalName { get; set; }

        public string PrimarySmtpAddress { get; set; }

        public Collection<string> SmtpAddresses { get; set; } = new Collection<string>();

        public string Name { get; set; }

        public string EmailAddress { get; set; }

        public string DisplayName { get; set; }

        public int? ResourceCapacity { get; set; }

        public string RestrictionType { get; set; }

        public string BookingProcessing { get; set; }

        public string City { get; set; }

        public string Company { get; set; }

        public string CountryOrRegion { get; set; }

        public string Department { get; set; }

        public string Office { get; set; }

        public string Floor { get; set; }

        public string Phone { get; set; }

        public string PostalCode { get; set; }

        public string StateOrProvince { get; set; }
    }
}