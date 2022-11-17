using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    /// <summary>
    /// [Description]:<Drive>:[^]:[Group]:<UNC_Path>
    /// </summary>
    public class MappingNetworkModel
    {
        public MappingNetworkModel() { }

        public string Description { get; set; }

        public string Drive { get; set; }

        public string Group { get; set; }

        public string UNC { get; set; }

        public bool IsChecked { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}{2}:{3}:{4}",
                string.IsNullOrEmpty(Description) ? string.Empty : Description,
                string.IsNullOrEmpty(Drive) ? string.Empty : Drive,
                IsChecked ? "^" : string.Empty,
                string.IsNullOrEmpty(Group) ? string.Empty : Group,
                string.IsNullOrEmpty(UNC) ? string.Empty : UNC);
        }
    }
}
