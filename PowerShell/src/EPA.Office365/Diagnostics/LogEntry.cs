using Serilog.Events;
using System;
using System.Collections.Generic;

namespace EPA.Office365.Diagnostics
{
    public class LogEntry
    {
        public LogEventLevel LogLevel { get; set; }    // Error, Warning, Info, Perf
        public string Message { get; set; }
        public string Source { get; set; }
        public Exception Exception { get; set; }
        public LogEntryType LogType { get; set; }
        public ICollection<string> OperationProperties { get; set; } = Array.Empty<string>();
    }
}