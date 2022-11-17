using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Scan
{
    public class ScanResultLogLines
    {
        public ScanResultLogLines()
        {
            // Scanned files
            this.Scanned = new List<ScanModels>();
        }

        /// <summary>
        /// Contains the scanned log lines of the files
        /// </summary>
        [JsonIgnore()]
        public List<ScanModels> Scanned { get; set; }

        /// <summary>
        /// The code has a web service call, we should evaluate this
        /// </summary>
        public IEnumerable<ScanModels> Evaluations
        {
            get
            {
                return this.Scanned.Where(s => s.Evaluation);
            }
        }

        /// <summary>
        /// The code has a violation of SP web services or Permissions
        /// </summary>
        public IEnumerable<ScanModels> Violations
        {
            get
            {
                return this.Scanned.Where(s => s.Violation);
            }
        }

        /// <summary>
        /// The code has a permission flag regarding add-in, workflow, or BCS
        /// </summary>
        public IEnumerable<ScanModels> Permissions
        {
            get
            {
                return this.Scanned.Where(s => s.Permission);
            }
        }

        /// <summary>
        /// The clean scans from the process
        /// </summary>
        public IEnumerable<ScanModels> Clean
        {
            get
            {
                return this.Scanned.Where(s => !s.Violation && !s.Evaluation && !s.Permission && !s.IsExcluded);
            }
        }
    }
}
