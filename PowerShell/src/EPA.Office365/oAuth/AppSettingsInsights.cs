using System;
using System.Collections.Generic;
using System.Text;

namespace EPA.Office365.oAuth
{
    public class AppSettingsInsights : AppSettingsInsightsChannel
    {
        public string InstrumentationKey { get; set; }

        public AppSettingsInsightsChannel TelemetryChannel { get; set; }
    }

    public class AppSettingsInsightsChannel
    {
        public string EndpointAddress { get; set; }
    }
}
