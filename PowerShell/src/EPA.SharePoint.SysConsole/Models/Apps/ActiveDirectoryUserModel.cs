using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    /// <summary>
    /// Represents and AD User object
    /// </summary>
    public class ActiveDirectoryUserModel : IComparable
    {
        public ActiveDirectoryUserModel()
        {
            SID = null;
        }

        public string SamAccountName { get; set; }

        public string SID { get; set; }

        public string Mail { get; set; }

        public string UserPrincipalName { get; set; }

        public string EmployeeID { get; set; }

        public string EmployeeNumber { get; set; }

        public string Distinguishedname { get; set; }

        public string Manager { get; set; }

        public string DisplayName { get; set; }

        public string GivenName { get; set; }

        public string SurName { get; set; }

        public string Office { get; set; }

        public string Organization { get; set; }

        public DateTime? WhenCreated { get; set; }

        /// <summary>
        /// Provides comparision property
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            return SamAccountName.CompareTo(obj);
        }
    }
}
