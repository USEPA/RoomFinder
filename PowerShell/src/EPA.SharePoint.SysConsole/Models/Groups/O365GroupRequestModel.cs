using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.Groups
{
    /// <summary>
    /// Represents a Group Request in the application
    /// </summary>
    public class O365GroupRequestModel
    {
        public int ListItemId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PublicFlag { get; set; }
        public string GroupName { get; set; }
        public string GroupOwner { get; set; }
        public string CleanAlias { get; set; }
        public string RequestCompletedFlag { get; set; }
        public string EmailAddress { get; set; }
        public string WhenCreated { get; set; }
        public string WhenChanged { get; set; }
        public string PrimarySmtpAddress { get; set; }
        public string Alias { get; set; }
        public string AccessType { get; set; }
        public string Notes { get; set; }
        public bool Success { get; set; }
        public string SharePointSiteUrl { get; set; }
        public string SharePointDocumentsUrl { get; set; }

        /// <summary>
        /// Does the user want a Team?
        /// </summary>
        public bool? CreateTeam { get; set; }

        /// <summary>
        /// Semi-Colon delimited set of Owners
        /// </summary>
        public string Owners { get; set; }

        public ICollection<string> EmailAddresses { get; set; } = new List<string>();
    }
}
