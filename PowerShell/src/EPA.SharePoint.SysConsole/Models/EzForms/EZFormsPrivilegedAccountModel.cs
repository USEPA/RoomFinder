using System;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    /// <summary>
    /// EZ Forms Request Model
    /// </summary>
    public class EZFormsPrivilegedAccountModel : IEzFormsSPModel
    {
        /// <summary>
        /// Initialize entity
        /// </summary>
        public EZFormsPrivilegedAccountModel()
        {
        }

        /// <summary>
        /// SharePoint Unique ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Will default to 0 if this is not set in the list
        /// </summary>
        public int RequestId { get; set; }


        public int EmployeeId { get; set; }

        /// <summary>
        /// Email address extracted from Employee Object
        /// </summary>
        public string LANMail { get; set; }

        public string LANSamAccountName { get; set; }


        public string LANEmployeeID { get; set; }


        public string DottedEmployeeNumber { get; set; }


        public string DottedSamAccountName { get; set; }


        public bool RowProcessed { get; set; }


        public string RowMessage { get; set; }


        public int EmployeeManagerId { get; set; }

        public string EmployeeManagerMail { get; set; }


        public string Title { get; set; }


        public string Action { get; set; }


        public bool ActionTaken { get; set; }


        public bool ActionErrored { get; set; }

        public bool EmailSent { get; set; }

        public DateTime? DottedAccountCreated { get; set; }

        public string URL { get; set; }
    }
}
