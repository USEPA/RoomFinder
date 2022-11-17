using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class AccountProcessingModel
    {
        public AccountProcessingModel()
        {
            ItemID = 0;
            Activity = new List<AccountProcessingActivityModel>();
        }

        public string SID { get; set; }
        public string EmployeeID { get; set; }
        public string EmployeeNumber { get; set; }
        public int ItemID { get; set; }
        public string DottedAccount { get; set; }
        public DateTime? AccountExpirationDate { get; set; }
        public string Manager { get; set; }

        public List<AccountProcessingActivityModel> Activity { get; set; }
    }
}
