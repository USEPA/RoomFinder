using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Scan
{
    public class ScanLogLine
    {
        public ScanLogLine() { }

        public ScanLogLine(int line, string result) : this()
        {
            this.LineNumber = line;
            this.Result = result;
        }

        public int LineNumber { get; set; }

        public string Result { get; set; }

        /// <summary>
        /// Prefix output string with line number
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Line:{0} = {1}", LineNumber, Result);
        }
    }
}
