using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    /// <summary>
    /// Represents the datastruture for the feature/governance app
    /// </summary>
    public class MockModel
    {
        public MockModel()
        {
            this.filters = new List<FilterModel>();
            this.features = new List<FeatureSetModel>();
        }

        public IList<FilterModel> filters { get; set; }

        public IList<FeatureSetModel> features { get; set; }

    }
}
