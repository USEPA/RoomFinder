using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Reporting
{
    public class PageFileStatistic
    {
        public Guid SiteGuid { get; set; }

        public Guid WebGuid { get; set; }

        public Guid ListGuid { get; set; }

        public Guid PageGuid { get; set; }

        public int PageId { get; set; }

        public string Url { get; set; }

        public int TotalHits { get; set; }

        public int TotalUniqueUsers { get; set; }

        public bool IsWelcomePage { get; set; }

        public DateTime Edited { get; set; }

        public string EditedUser { get; set; }
    }
}
