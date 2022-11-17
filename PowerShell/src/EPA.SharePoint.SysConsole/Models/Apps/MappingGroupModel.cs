using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class MappingGroupModel
    {
        public MappingGroupModel()
        {
            this.Drives = new List<string>();
            this.Networks = new List<MappingNetworkModel>();
        }

        public string Name { get; set; }

        public List<string> Drives { get; set; }

        public List<MappingNetworkModel> Networks { get; set; }
    }
}
