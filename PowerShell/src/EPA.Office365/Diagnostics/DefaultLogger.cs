using EPA.Office365;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365
{
    public class DefaultLogger : ITraceLogger
    {
        public void LogError(Exception ex, String message, params object[] args)
        {
            System.Diagnostics.Trace.TraceInformation(message, args);
        }

        public void LogWarning(String message, params object[] args)
        {
            System.Diagnostics.Trace.TraceWarning(message, args);
        }

        public void LogInformation(String message, params object[] args)
        {
            System.Diagnostics.Trace.TraceInformation(message, args);
        }
    }
}