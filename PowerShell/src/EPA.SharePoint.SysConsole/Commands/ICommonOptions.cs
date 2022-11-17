namespace EPA.SharePoint.SysConsole.Commands
{
    public interface ICommonOptions
    {
        bool Verbose { get; set; }

        bool? WhatIf { get; set; }

        string LogFileName { get; set; }

        int MinimalHealthScore { get; set; }

        int RetryCount { get; set; }

        int RetryWait { get; set; }

        int RequestTimeout { get; set; }
    }
}