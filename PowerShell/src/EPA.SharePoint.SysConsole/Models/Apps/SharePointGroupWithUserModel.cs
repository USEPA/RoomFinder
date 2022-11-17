using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    /// <summary>
    /// Represents the AD group and its members
    /// </summary>
    public class SharePointGroupWithUserModel
    {
        public SharePointGroupWithUserModel()
        {
            Members = new List<ActiveDirectoryUserModel>();
        }

        public string ADGroup { get; set; }

        public string SPGroup { get; set; }

        public ICollection<ActiveDirectoryUserModel> Members { get; set; }
    }
}
