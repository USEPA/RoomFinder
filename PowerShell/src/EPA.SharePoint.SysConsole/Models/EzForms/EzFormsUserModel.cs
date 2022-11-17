using System;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EzFormsUserModel : IEzFormsSPModel, IComparable
    {
        /// <summary>
        /// Initialize the user mdoel base
        /// </summary>
        public EzFormsUserModel()
        {

        }
        
        /// <summary>
        /// SharePoint Unique ID
        /// </summary>
        public int ID { get; set; }

        public string DisplayName { get; set; }

        public string Email { get; set; }

        public string UPN { get; set; }

        public string UserName { get; set; }

        public string SPS_ClaimID { get; set; }

        public string AD_GUID { get; set; }

        public bool Employee { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int CompareTo(object obj)
        {
            return Email.CompareTo(obj);
        }
    }
}
