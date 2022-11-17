using System;

namespace EPA.Office365.Graph.Reporting
{
    public interface ICustomGraphReportProperties
    {
        ReportUsagePeriodEnum Period { get; set; }

        DateTime? Date { get; set; }
    }
}
