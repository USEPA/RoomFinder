using System;

namespace EPA.Office365.Graph.Reporting
{
    public class CustomGraphReportProperties : ICustomGraphReportProperties
    {
        public ReportUsagePeriodEnum Period { get; set; }

        public DateTime? Date { get; set; }
    }
}
