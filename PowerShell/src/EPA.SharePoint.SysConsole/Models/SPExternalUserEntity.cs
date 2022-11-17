using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeDevPnP.Core.Entities;

namespace EPA.SharePoint.SysConsole.Models
{
    public class SPExternalUserEntity : ExternalUserEntity
    {
        public Nullable<int> UserId { get; set; }

        public bool FoundInSiteUsers { get; set; }
    }
}
