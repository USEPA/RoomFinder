using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Reporting
{
    public class O365ReportServiceStatistic
    {
        public O365ReportServiceStatistic() { }

        public O365ReportServiceStatistic(string typename, DateTime typeDate, string typeline)
        {
            this.ReportType = typename;
            this.Line = typeline;
        }

        public O365ReportServiceStatistic(string typename, long typeID, DateTime typeDate, string typeline) : this(typename, typeDate, typeline)
        {
            this.ID = typeID;
            this.Date = typeDate;
        }

        public string ReportType { get; set; }

        public string Line { get; set; }

        public Nullable<long> ID { get; set; }

        public DateTime Date { get; set; }

        /// <summary>
        /// Pesent the type prepended by the line
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.ID.HasValue)
            {
                return string.Format("{0},{1},{2},{3},{4},{5}", ReportType, ID, Date.ToShortDateString(), Date.Month, Date.Year, Line);
            }

            return string.Format("{0},{1},{2},{3},{4}", ReportType, Date.ToShortDateString(), Date.Month, Date.Year, Line);
        }
    }
}
