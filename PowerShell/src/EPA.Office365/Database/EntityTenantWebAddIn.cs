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
    /// Represents an AddIn
    /// </summary>
    [Table("TenantWebAddIn", Schema = "dbo")]
    public class EntityTenantWebAddIn : ModelBase
    {
        public EntityTenantWebAddIn()
        {
            InError = false;
        }

        [Column("tenantSiteId")]
        public int? TenantSiteId { get; set; }

        [Column("tenantWebId")]
        public int? TenantWebId { get; set; }

        [Column("SiteGuid")]
        public Guid? SiteGuid { get; set; }

        [Column("WebGuid")]
        public Guid WebGuid { get; set; }

        public Guid? AppGuid { get; set; }

        public bool InError { get; set; }

        public string AppTitle { get; set; }

        public string AppDescription { get; set; }

        public string AppPermissions { get; set; }

        public string AppStatus { get; set; }

        public string HostedType { get; set; }

        public string HostWebUrl { get; set; }

        public string AppRedirectUrl { get; set; }

        public string AppPrincipalId { get; set; }

        public string AppWebFullUrl { get; set; }

        public string ImageFallbackUrl { get; set; }

        public string ImageUrl { get; set; }

        public string RemoteAppUrl { get; set; }

        public string SettingsPageUrl { get; set; }

        public string StartPage { get; set; }

        public string EulaUrl { get; set; }

        public string PrivacyUrl { get; set; }

        public string Publisher { get; set; }

        public string SupportUrl { get; set; }

        public Guid? ProductGuid { get; set; }

    }
}

