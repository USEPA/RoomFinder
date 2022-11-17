using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class FeatureSetModel
    {
        public FeatureSetModel()
        {
            items = new List<FeatureItemModel>();
        }

        public string dataTypes { get; set; }

        public string dataTypeIcon { get; set; }

        public string dataTypeBanner { get; set; }

        public string dataTypeCountId { get; set; }

        public int dataCount { get; set; }

        public int filteredCount { get; set; }

        public string dataDescription { get; set; }

        public string dataItems { get; set; }

        public IList<FeatureItemModel> items { get; set; }
    }
}
