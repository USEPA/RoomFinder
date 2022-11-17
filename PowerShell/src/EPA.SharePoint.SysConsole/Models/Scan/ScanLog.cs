using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Scan
{
    public class ScanLog
    {

        public ScanLog()
        {
            this.Timestamp = DateTimeOffset.Now;
        }

        public ScanLog(string message) : this()
        {
            this.Message = message;
        }


        public ScanLog(string message, bool error) : this(message)
        {
            this.Message = message;
            this.ErrorLog = error;
        }

        public string Message { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public bool ErrorLog { get; set; }

        /// <summary>
        /// Print out timestamp and message
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (ErrorLog)
                return string.Format("{0} - Error=>{1}", Timestamp.ToString("G"), Message);
            else
                return string.Format("{0} - {1}", Timestamp.ToString("G"), Message);
        }
    }
}
