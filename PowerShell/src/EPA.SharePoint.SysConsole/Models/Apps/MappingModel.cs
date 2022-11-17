using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class MappingModel
    {
        public MappingModel()
        {
            this.SubGroups = new List<MappingGroupModel>();
        }

        public string Name { get; set; }

        public List<MappingGroupModel> SubGroups { get; set; }
    }

}
