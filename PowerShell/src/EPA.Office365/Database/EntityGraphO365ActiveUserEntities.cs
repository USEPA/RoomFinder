using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    /// <summary>
    /// ACTIVE SERVICES
    /// </summary>
    [Table("GraphO365ActiveUserServices", Schema = "dbo")]
    public class EntityGraphO365ActiveUserService
    {
        public EntityGraphO365ActiveUserService()
        {
            LastActivityDate = DateTime.UtcNow.Date;
            ReportRefreshDate = DateTime.UtcNow.Date;
        }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        public DateTime? ReportRefreshDate { get; set; }

        public long? ExchangeActive { get; set; }

        public long? ExchangeInActive { get; set; }

        public long? OneDriveActive { get; set; }

        public long? OneDriveInActive { get; set; }

        public long? SharePointActive { get; set; }

        public long? SharePointInActive { get; set; }

        public long? SkypeActive { get; set; }

        public long? SkypeInActive { get; set; }

        public long? YammerActive { get; set; }

        public long? YammerInActive { get; set; }

        public long? TeamsActive { get; set; }

        public long? TeamsInActive { get; set; }

        public int ReportingPeriodDays { get; set; }
    }

    /// <summary>
    /// ACTIVE DETAILS
    /// </summary>
    [Table("GraphO365ActiveUserDetails", Schema = "dbo")]
    public class EntityGraphO365ActiveUserDetails
    {
        public EntityGraphO365ActiveUserDetails()
        {
            ReportRefreshDate = DateTime.UtcNow.Date;
        }


        [Key()]
        public string UPN { get; set; }

        public string DisplayName { get; set; }

        public Nullable<bool> Deleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Nullable<bool> LicenseForExchange { get; set; }
        public Nullable<bool> LicenseForOneDrive { get; set; }
        public Nullable<bool> LicenseForSharePoint { get; set; }
        public Nullable<bool> LicenseForSkypeForBusiness { get; set; }
        public Nullable<bool> LicenseForYammer { get; set; }
        public Nullable<bool> LicenseForTeams { get; set; }

        public DateTime? LastActivityDateForExchange { get; set; }
        public DateTime? LastActivityDateForOneDrive { get; set; }
        public DateTime? LastActivityDateForSharePoint { get; set; }
        public DateTime? LastActivityDateForSkypeForBusiness { get; set; }
        public DateTime? LastActivityDateForYammer { get; set; }
        public DateTime? LastActivityDateForTeams { get; set; }

        public DateTime? LicenseAssignedDateForExchange { get; set; }
        public DateTime? LicenseAssignedDateForOneDrive { get; set; }
        public DateTime? LicenseAssignedDateForSharePoint { get; set; }
        public DateTime? LicenseAssignedDateForSkypeForBusiness { get; set; }
        public DateTime? LicenseAssignedDateForYammer { get; set; }
        public DateTime? LicenseAssignedDateForTeams { get; set; }

        public string ProductsAssigned { get; set; }


        public DateTime ReportRefreshDate { get; set; }
    }

    /// <summary>
    /// ACTIVE USERS
    /// </summary>
    [Table("GraphO365ActiveUsers", Schema = "dbo")]
    public class EntityGraphO365ActiveUsers
    {
        public EntityGraphO365ActiveUsers()
        {
            LastActivityDate = DateTime.UtcNow.Date;
            ReportRefreshDate = DateTime.UtcNow.Date;
        }

        [Key()]
        public DateTime LastActivityDate { get; set; }

        public DateTime ReportRefreshDate { get; set; }

        public int ReportingPeriodDays { get; set; }

        public long? Office365 { get; set; }

        public long? Exchange { get; set; }

        public long? OneDrive { get; set; }

        public long? SharePoint { get; set; }

        public long? SkypeForBusiness { get; set; }

        public long? Yammer { get; set; }

        public long? Teams { get; set; }
    }
}
