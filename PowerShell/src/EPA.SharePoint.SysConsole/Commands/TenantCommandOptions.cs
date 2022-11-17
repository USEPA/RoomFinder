using CommandLine;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    public class TenantCommandOptions : CommonOptions, ITenantCommandOptions
    {
        /// <summary>
        /// The DateTime of the current running process, this will override the current timestamp
        /// </summary>
        [Option("logdate", Required = false)]
        public DateTime? LogDateTime { get; set; }
    }
}
