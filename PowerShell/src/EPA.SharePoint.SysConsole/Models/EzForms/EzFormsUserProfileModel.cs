using System;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EzFormsUserProfileModel : EzFormsUserModel
    {
        /// <summary>
        /// Initialize the user model
        /// </summary>
        public EzFormsUserProfileModel() : base()
        {
            this.ExternalUser = false;
            this.DisabledAccount = false;
        }

        public bool ExternalUser { get; set; }

        public bool DisabledAccount { get; set; }

        public string UserProfile_GUID { get; set; }

        public string SPS_DistinguishedName { get; set; }

        public string SID { get; set; }

        public string msOnline_ObjectId { get; set; }

        public string Office { get; set; }

        public string SamAccountName { get; set; }

        public string WorkforceID { get; set; }
        public string WorkforceNumber { get; set; }

        public string DottedAccountName { get; set; }

        public string Department { get; set; }

        public string SPS_Department { get; set; }

        public string SPS_JobTitle { get; set; }

        public string WorkPhone { get; set; }

        public string Manager { get; set; }

        public Nullable<DateTime> DisabledAccountDate { get; set; }
    }
}
