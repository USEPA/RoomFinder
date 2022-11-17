using CommandLine;

namespace EPA.SharePoint.SysConsole.Commands
{
    /// <summary>
    /// Common options for all commands
    /// </summary>
    public class CommonOptions : ICommonOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "increase logging if set.")]
        public bool Verbose { get; set; }

        [Option('w', "whatif", Required = false, HelpText = "provides for what if scenarios, present changes before asserting them.")]
        public bool? WhatIf { get; set; }

        [Option('l', "logfile", Required = false, HelpText = "Log filename, defaults to logs.txt.")]
        public string LogFileName { get; set; } = "poshlogs.txt";

        [Option("minimal-score", Required = false, SetName = "__AllParameterSets", HelpText = "Specifies a minimal server healthscore before any requests are executed.")]
        public int MinimalHealthScore { get; set; } = -1;

        [Option("retry-count", Required = false, SetName = "__AllParameterSets", HelpText = "Defines how often a retry should be executed if the server healthscore is not sufficient.")]
        public int RetryCount { get; set; } = -1;

        [Option("retry-wait", Required = false, SetName = "__AllParameterSets", HelpText = "Defines how many seconds to wait before each retry. Default is 5 seconds.")]
        public int RetryWait { get; set; } = 5;

        [Option("request-timeout", Required = false, SetName = "__AllParameterSets", HelpText = "The request timeout. Default is 180000")]
        public int RequestTimeout { get; set; } = 1800000;
    }
}
