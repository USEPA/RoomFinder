using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Governance
{
    public class EPASCASyncModels
    {
        public List<SCAModel> SourceSCAList { get; set; }

        public List<DestinationSCAModel> AdminLK { get; set; }
    }

    /// <summary>
    /// SCA Listing Columns
    /// </summary>
    public class SCAModel
    {
        public string Email { get; set; }

        public string OfficeTitle { get; set; }

        public string SCA_x0020_Role { get; set; }

        public string Region_x0020_or_x0020_Program_x0 { get; set; }


        public override string ToString()
        {
            return string.Format("Email {0} Office {1}", Email, OfficeTitle);
        }
    }

    /// <summary>
    /// AdminLK Columns
    /// </summary>
    public class DestinationSCAModel
    {
        public string Email { get; set; }

        public string OfficeTitle { get; set; }

        public int ListItemId { get; set; }

        public int CollectionSiteTypeId { get; set; }

        public string CollectionSiteType { get; set; }

        public override string ToString()
        {
            return string.Format("Email {0} Office {1}", Email, OfficeTitle);
        }
    }

    public class CollectionSiteLookup
    {
        public int ListItemId { get; set; }

        public string SiteTitle { get; set; }

        public string TemplateName { get; set; }

        public string TypeTitle { get; set; }

        public string ShowInDropDown { get; set; }

        public string SiteUrl { get; set; }


        public int SiteCollectionId { get; set; }

        public string SiteCollection { get; set; }


        public int SiteTypeId { get; set; }

        public string SiteType { get; set; }
    }
}
