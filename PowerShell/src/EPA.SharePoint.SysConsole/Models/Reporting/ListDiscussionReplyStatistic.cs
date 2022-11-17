using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Reporting
{
    public class ListDiscussionReplyStatistic
    {
        public ListDiscussionReplyStatistic()
        {
            this.ItemCount = 0;
        }

        public string RelativeUrl { get; set; }

        public int ItemCount { get; set; }

        public string AuthorName { get; set; }

        public DateTime LastItemUserModifiedDate { get; set; }
    }

}
