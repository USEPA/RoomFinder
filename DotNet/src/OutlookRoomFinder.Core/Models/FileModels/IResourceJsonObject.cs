using System.Collections.ObjectModel;

namespace OutlookRoomFinder.Core.Models.FileModels
{
    public interface IResourceJsonObject
    {
        string BookingProcessing { get; set; }
        string City { get; set; }
        string Company { get; set; }
        string CountryOrRegion { get; set; }
        string Department { get; set; }
        string DisplayName { get; set; }
        string EmailAddress { get; set; }
        string RestrictionType { get; set; }
        string Name { get; set; }
        string Office { get; set; }
        string Floor { get; }
        string Phone { get; set; }
        string PostalCode { get; set; }
        string PrimarySmtpAddress { get; set; }
        int? ResourceCapacity { get; set; }
        string SamAccountName { get; set; }
        Collection<string> SmtpAddresses { get; set; }
        string StateOrProvince { get; set; }
        string UserPrincipalName { get; set; }
    }
}