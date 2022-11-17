using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Reporting
{
    public class ListStatistic
    {
        public ListStatistic()
        {
            this.ItemCount = 0;
        }

        public string ListName { get; set; }

        public int ItemCount { get; set; }

        public BaseType ListType { get; set; }

        public DateTime FirstItemCreated { get; set; }

        public DateTime LastItemUserModifiedDate { get; set; }

        public IList<ListDiscussionReplyStatistic> Replies { get; set; }
        public ListTemplateType ListTemplateType { get; set; }
        public string RelativeUrl { get; set; }
    }

}
