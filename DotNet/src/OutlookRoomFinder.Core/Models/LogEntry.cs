using Serilog.Events;
using System;
using System.Collections.Generic;

namespace OutlookRoomFinder.Core.Models
{
    public class LogEntry
    {
        public string User { get; set; }
        public string Url { get; set; }
        public LogEventLevel LogLevel { get; set; }    // Error, Warning, Info, Perf
        public LogEntryType LogType { get; set; }
        public string Operation { get; set; }
        public ICollection<string> OperationProperties { get; set; } = Array.Empty<string>();
    }
}
