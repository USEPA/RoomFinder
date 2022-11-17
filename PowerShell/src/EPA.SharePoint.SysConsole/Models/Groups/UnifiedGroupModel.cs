using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.Groups
{
    public class UnifiedGroupModel
    {
        public UnifiedGroupModel()
        {
            this.EmailAddresses = new List<string>();
            this.Owners = new List<string>();
        }


        public string DisplayName { get; set; }

        public string WhenCreated { get; set; }

        public string WhenChanged { get; set; }

        public string PrimarySmtpAddress { get; set; }

        public string Alias { get; set; }

        public string AccessType { get; set; }

        public string Notes { get; set; }

        public string SharePointNotebookUrl { get; set; }

        public string SharePointSiteUrl { get; set; }

        public string SharePointDocumentsUrl { get; set; }

        /// <summary>
        /// Semi-Colon delimited set of Owners
        /// </summary>
        public ICollection<string> Owners { get; set; }

        public ICollection<O365GroupMemberModel> Members { get; set; }

        public ICollection<O365GroupMemberModel> MemberOwners { get; set; }

        public ICollection<string> EmailAddresses { get; set; }
    }
}
