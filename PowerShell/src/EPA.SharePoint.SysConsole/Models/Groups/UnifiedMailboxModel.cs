using System.Collections.ObjectModel;

namespace EPA.SharePoint.SysConsole.Models.Groups
{


    /// <summary>
    /// O365 Group Exchange Object Model
    /// </summary>
    public class UnifiedMailboxModel
    {
        public UnifiedMailboxModel()
        {
            this.EmailAddresses = new Collection<string>();
            this.Owners = new Collection<string>();
        }


        public Collection<string> EmailAddresses { get; set; }
        public string PrimarySmtpAddress { get; set; }
        public string SharePointNotebookUrl { get; set; }
        public string SharePointSiteUrl { get; set; }
        public string SharePointDocumentUrl { get; set; }
        public Collection<string> Owners { get; set; }
    }
}
