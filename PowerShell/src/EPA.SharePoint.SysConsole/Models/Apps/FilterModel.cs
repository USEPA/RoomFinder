using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class FilterModel
    {
        public FilterModel()
        {
            this.tags = new List<FilterTagModel>();
        }

        public FilterModel(string Display, string ClassId) : this()
        {
            this.display = Display;
            this.classid = ClassId;
        }

        public string display { get; set; }

        public string name { get { return this.display.ToLower(); } }

        public string classid { get; set; }

        public string classname { get { return classid.ToLowerInvariant(); } }

        public IList<FilterTagModel> tags { get; set; }
    }
}
