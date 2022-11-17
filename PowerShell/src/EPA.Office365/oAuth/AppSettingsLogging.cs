using Serilog.Events;

namespace EPA.Office365.oAuth
{
    public class AppSettingsLogging
    {
        public bool IncludeScopes { get; set; }

        public LogEventLevel Level { get; set; }

        public AppSettingsLoggingLevel LogLevel { get; set; }
    }

    public class AppSettingsLoggingLevel
    {
        public LogEventLevel Default { get; set; }

        public LogEventLevel System { get; set; }

        public LogEventLevel Microsoft { get; set; }
    }
}