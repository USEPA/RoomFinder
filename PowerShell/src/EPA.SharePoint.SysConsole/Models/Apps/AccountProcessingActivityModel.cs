using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class AccountProcessingActivityModel
    {
        /// <summary>
        /// stores the request ID for post parsing
        /// </summary>
        public int ID { get; set; }

        public string Message { get; set; }

        public DateTime EventDate { get; set; }

        public Nullable<DateTime> ExpirationDate { get; set; }
    }   

}
