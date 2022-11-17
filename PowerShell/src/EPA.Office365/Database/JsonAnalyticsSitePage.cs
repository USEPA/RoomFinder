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
    /// Represents a resource or page on the site
    /// </summary>
    public class JsonAnalyticsSitePage
    {
        public JsonAnalyticsSitePage()
        {
            this.IsWelcomePage = false;
            this.TotalHits = 0;
            this.TotalUniqueVisitors = 0;
        }

        /// <summary>
        /// Foreign Key to the Site Collection
        /// </summary>
        public Nullable<Guid> SiteGuid { get; set; }

        /// <summary>
        /// Foreign Key to the Web
        /// </summary>
        public Nullable<Guid> WebGuid { get; set; }

        /// <summary>
        /// The parent list to which this page belongs
        /// </summary>
        public Nullable<Guid> ListGuid { get; set; }

        /// <summary>
        /// Page Unique ID
        /// </summary>
        public Nullable<Guid> PageGuid { get; set; }

        public Nullable<int> PageId { get; set; }

        public string PageUrl { get; set; }


        public DateTime? EditedDate { get; set; }

        public string EditedUser { get; set; }

        /// <summary>
        /// The page is the default landing page
        /// </summary>
        public bool IsWelcomePage { get; set; }

        public Nullable<Int64> TotalHits { get; set; }

        public Nullable<Int64> TotalUniqueVisitors { get; set; }
    }
}
