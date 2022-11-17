using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EZFormsApprovers
    {
        public EZFormsApprovers()
        {
            this.Requests = new List<EZFormsArchive>();
        }

        public string EmailAddress { get; set; }

        public int TotalRequests
        {
            get
            {
                return this.Requests.Count();
            }
        }

        public List<EZFormsArchive> Requests { get; set; }

    }
}
