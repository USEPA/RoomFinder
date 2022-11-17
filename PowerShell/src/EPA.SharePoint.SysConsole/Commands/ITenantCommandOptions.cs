using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    public interface ITenantCommandOptions : ICommonOptions
    {
        DateTime? LogDateTime { get; set; }
    }
}