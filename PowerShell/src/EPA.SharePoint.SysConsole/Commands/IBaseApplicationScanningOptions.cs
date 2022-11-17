using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Commands
{
    public interface IBaseApplicationScanningOptions : ITenantCommandOptions
    {
        bool Deepscan { get; set; }

        /// <summary>
        /// Will write log file to SharePoint List unless this is specified
        /// </summary>
        bool SkipLog { get; set; }

        /// <summary>
        /// Will evaluate file/libraries
        /// </summary>
        bool EvaluateLibraries { get; set; }

        /// <summary>
        /// Will write files found in pages libraries to disk
        /// </summary>
        bool WriteFilesToDisk { get; set; }
    }
}
