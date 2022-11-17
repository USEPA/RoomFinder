using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Governance
{
    public class SiteMailboxes
    {
        public string SharePointUrl { get; set; }

        public string[] EmailAddresses { get; set; }

        public string DistinguishedName { get; set; }


        public string UPN { get; set; }
    }
}
