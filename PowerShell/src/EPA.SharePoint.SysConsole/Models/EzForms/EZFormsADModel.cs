using System;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    /// <summary>
    /// Represents a row in Active Directory
    /// </summary>
    public class EZFormsADModel : IComparable
    {
        public EZFormsADModel() { }

        /// <summary>
        /// represents the uniqueid of the record
        /// </summary>
        public int? UniqueID { get; set; }

        /// <summary>
        /// Represents the Active Directory Unique ID
        /// </summary>
        public string SID { get; set; }

        /// <summary>
        /// LAN ID
        /// </summary>
        public string SamAccountName { get; set; }

        /// <summary>
        /// First Name
        /// </summary>
        public string GivenName { get; set; }

        /// <summary>
        /// Last Name
        /// </summary>
        public string SurName { get; set; }

        /// <summary>
        /// Full Name
        /// </summary>
        public string DisplayName { get; set; }


        public string Mail { get; set; }

        public string EmployeeID { get; set; }

        public string EmployeeNumber { get; set; }

        public string Office { get; set; }

        /// <summary>
        /// the users organization from AD
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// The full qualified name
        /// </summary>
        public string UserPrincipalName { get; set; }

        /// <summary>
        /// the OU location
        /// </summary>
        public string Path { get; set; }

        public DateTime? AccountExpirationDate { get; set; }

        /// <summary>
        /// the users manager
        /// </summary>
        public string ADmanager { get; set; }

        public int CompareTo(object obj)
        {
            return SID.CompareTo(obj);
        }
    }
}
