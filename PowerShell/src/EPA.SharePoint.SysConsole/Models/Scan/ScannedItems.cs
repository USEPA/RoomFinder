using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Scan
{
    public class ScannedItems
    {
        public ScannedItems()
        {
            this.FoundEvaluations = 0;
            this.FoundViolations = 0;
        }

        public int Id { get; set; }
        public DateTime Created { get; set; }
        public Nullable<int> RequestID { get; set; }
        public string SiteUrl { get; set; }
        public string InstanceId { get; set; }
        public bool TestTenant { get; set; }
        public int FoundViolations { get; set; }
        public int FoundEvaluations { get; set; }
    }
}
