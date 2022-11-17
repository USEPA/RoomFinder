using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class FeatureItemModel
    {
        public FeatureItemModel()
        {
            this.tags = new List<string>();
        }

        public int id { get; set; }

        public string title { get; set; }


        public string idlbl { get; set; }

        public string description { get; set; }

        public string assistance { get; set; }

        public string moreinfo { get; set; }

        public string howItAffectsMe { get; set; }

        public string howDoIPrepare { get; set; }

        public DateTime createdDate { get; set; }

        public bool recentlyUpdated { get; set; }

        public bool recentlyAdded { get; set; }

        /// <summary>
        /// used in search panel to render feature
        /// </summary>
        public bool displayFeature { get; set; }

        public string url { get; set; }

        public IList<string> tags { get; set; }
    }
}
