using CommandLine;

namespace EPA.SharePoint.SysConsole.Commands
{
    public class BaseApplicationScanningOptions : TenantCommandOptions, IBaseApplicationScanningOptions
    {
        /// <summary>
        /// Scan all subsites along with the top level site
        /// (OPTIONAL) for a sub site scanning if specified
        /// </summary>
        [Option("deep-scan", Required = false, SetName = "Main", HelpText = "If you want to scan all sites and subsites")]
        public bool Deepscan { get; set; }

        /// <summary>
        /// Will write log file to SharePoint List unless this is specified
        /// </summary>
        [Option("skip-log", Required = false, SetName = "Main", HelpText = "If specified do not write log to sharepoint site")]
        public bool SkipLog { get; set; }

        /// <summary>
        /// Will evaluate file/libraries
        /// </summary>
        [Option("evaluate-libraries", Required = false, SetName = "Main", HelpText = "If we want to flag known libraries that pose concern")]
        public bool EvaluateLibraries { get; set; }

        /// <summary>
        /// Will write files found in pages libraries to disk
        /// </summary>
        [Option("write-files", Required = false, SetName = "Main", HelpText = "Will write files found in pages libraries to disk")]
        public bool WriteFilesToDisk { get; set; }
    }
}
