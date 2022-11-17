using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Reporting
{
    public class ClientTypeStatistic
    {
        public ClientTypeStatistic()
        {
            POP3 = 0;
            MAPI = 0;
            OWA = 0;
            EAS = 0;
            EWS = 0;
            IMAP = 0;
        }

        public DateTime _weeklyDate { get; set; }

        public long POP3 { get; set; }
        public long MAPI { get; set; }
        public long OWA { get; set; }
        public long EAS { get; set; }
        public long EWS { get; set; }
        public long IMAP { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", _weeklyDate.ToShortDateString(), _weeklyDate.Month, _weeklyDate.Year, POP3, MAPI, OWA, EAS, EWS, IMAP);
        }
    }
}
