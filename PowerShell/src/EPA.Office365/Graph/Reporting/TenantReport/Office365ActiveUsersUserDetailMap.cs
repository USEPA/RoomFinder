using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    /*
    The CSV file has the following headers for columns.
    Report Refresh Date
    User Principal Name
Display Name
Is Deleted
Deleted Date
Has Exchange License
Has OneDrive License
Has SharePoint License
Has Skype For Business License
Has Yammer License
Has Teams License

Exchange Last Activity Date
OneDrive Last Activity Date
SharePoint Last Activity Date
Skype For Business Last Activity Date
Yammer Last Activity Date
Teams Last Activity Date

Exchange License Assign Date
OneDrive License Assign Date
SharePoint License Assign Date
Skype For Business License Assign Date
Yammer License Assign Date
Teams License Assign Date

Assigned Products
    */
    internal class Office365ActiveUsersUserDetailMap : ClassMap<Office365ActiveUsersUserDetail>
    {
        internal Office365ActiveUsersUserDetailMap()
        {
            Map(m => m.ReportRefreshDate).Name("Report Refresh Date").Index(0).Default(default(DateTime));
            Map(m => m.UPN).Name("User Principal Name").Index(1).Default(string.Empty);
            Map(m => m.DisplayName).Name("Display Name").Index(2).Default(string.Empty);
            Map(m => m.Deleted).Name("Is Deleted").Index(3).Default(false);
            Map(m => m.DeletedDate).Name("Deleted Date").Index(4).Default(default(DateTime?));

            Map(m => m.LicenseForExchange).Name("Has Exchange License").Index(5).Default(false);
            Map(m => m.LicenseForOneDrive).Name("Has OneDrive License").Index(6).Default(false);
            Map(m => m.LicenseForSharePoint).Name("Has SharePoint License").Index(7).Default(false);
            Map(m => m.LicenseForSkypeForBusiness).Name("Has Skype For Business License").Index(8).Default(false);
            Map(m => m.LicenseForYammer).Name("Has Yammer License").Index(9).Default(false);
            Map(m => m.LicenseForMSTeams).Name("Has Teams License").Index(10).Default(false);

            Map(m => m.LastActivityDateForExchange).Name("Exchange Last Activity Date").Index(11).Default(default(DateTime?));
            Map(m => m.LastActivityDateForOneDrive).Name("OneDrive Last Activity Date").Index(12).Default(default(DateTime?));
            Map(m => m.LastActivityDateForSharePoint).Name("SharePoint Last Activity Date").Index(13).Default(default(DateTime?));
            Map(m => m.LastActivityDateForSkypeForBusiness).Name("Skype For Business Last Activity Date").Index(14).Default(default(DateTime?));
            Map(m => m.LastActivityDateForYammer).Name("Yammer Last Activity Date").Index(15).Default(default(DateTime?));
            Map(m => m.LastActivityDateForMSTeams).Name("Teams Last Activity Date").Index(16).Default(default(DateTime?));

            Map(m => m.LicenseAssignedDateForExchange).Name("Exchange License Assign Date").Index(17).Default(default(DateTime?));
            Map(m => m.LicenseAssignedDateForOneDrive).Name("OneDrive License Assign Date").Index(18).Default(default(DateTime?));
            Map(m => m.LicenseAssignedDateForSharePoint).Name("SharePoint License Assign Date").Index(19).Default(default(DateTime?));
            Map(m => m.LicenseAssignedDateForSkypeForBusiness).Name("Skype For Business License Assign Date").Index(20).Default(default(DateTime?));
            Map(m => m.LicenseAssignedDateForYammer).Name("Yammer License Assign Date").Index(21).Default(default(DateTime?));
            Map(m => m.LicenseAssignedDateForMSTeams).Name("Teams License Assign Date").Index(22).Default(default(DateTime?));

            Map(m => m.ProductsAssignedCSV).Name("Assigned Products").Index(23).Default(string.Empty);
        }
    }



    public class Office365ActiveUsersUserDetail : JSONODataBase
    {
        [JsonProperty("reportRefreshDate")]
        public DateTime ReportRefreshDate { get; set; }

        [JsonProperty("userPrincipalName")]
        public string UPN { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("isDeleted")]
        public bool Deleted { get; set; }

        [JsonProperty("deletedDate")]
        public DateTime? DeletedDate { get; set; }

        [JsonProperty("hasExchangeLicense")]
        public bool LicenseForExchange { get; set; }

        [JsonProperty("hasOneDriveLicense")]
        public bool LicenseForOneDrive { get; set; }

        [JsonProperty("hasSharePointLicense")]
        public bool LicenseForSharePoint { get; set; }

        [JsonProperty("hasSkypeForBusinessLicense")]
        public bool LicenseForSkypeForBusiness { get; set; }

        [JsonProperty("hasYammerLicense")]
        public bool LicenseForYammer { get; set; }

        [JsonProperty("hasTeamsLicense")]
        public bool LicenseForMSTeams { get; set; }

        [JsonProperty("exchangeLastActivityDate")]
        public DateTime? LastActivityDateForExchange { get; set; }

        [JsonProperty("oneDriveLastActivityDate")]
        public DateTime? LastActivityDateForOneDrive { get; set; }

        [JsonProperty("sharePointLastActivityDate")]
        public DateTime? LastActivityDateForSharePoint { get; set; }

        [JsonProperty("skypeForBusinessLastActivityDate")]
        public DateTime? LastActivityDateForSkypeForBusiness { get; set; }

        [JsonProperty("yammerLastActivityDate")]
        public DateTime? LastActivityDateForYammer { get; set; }

        [JsonProperty("teamsLastActivityDate")]
        public DateTime? LastActivityDateForMSTeams { get; set; }

        [JsonProperty("exchangeLicenseAssignDate")]
        public DateTime? LicenseAssignedDateForExchange { get; set; }

        [JsonProperty("oneDriveLicenseAssignDate")]
        public DateTime? LicenseAssignedDateForOneDrive { get; set; }

        [JsonProperty("sharePointLicenseAssignDate")]
        public DateTime? LicenseAssignedDateForSharePoint { get; set; }

        [JsonProperty("skypeForBusinessLicenseAssignDate")]
        public DateTime? LicenseAssignedDateForSkypeForBusiness { get; set; }

        [JsonProperty("yammerLicenseAssignDate")]
        public DateTime? LicenseAssignedDateForYammer { get; set; }

        [JsonProperty("teamsLicenseAssignDate")]
        public DateTime? LicenseAssignedDateForMSTeams { get; set; }

        [JsonProperty("assignedProducts")]
        public IEnumerable<string> ProductsAssigned { get; set; }

        [JsonIgnore()]
        public string ProductsAssignedCSV { get; set; }
    }

}
