using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class PublishingPage
    {
        #region Properties

        public string FileName { get; set; }

        public string TargetFileName { get; set; }

        public string Title { get; set; }

        public string Layout { get; set; }

        public string ListUrl { get; set; }

        public bool Overwrite { get; set; }

        public bool Publish { get; set; }

        public bool IsPublishingPage { get; set; }

        public bool WelcomePage { get; set; }

        public string PublishingPageContent { get; set; }

        public List<PublishingPageWebPart> WebParts { get; private set; } = new List<PublishingPageWebPart>();

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        #endregion Properties

        #region Constructors

        public PublishingPage()
        {
        }

        #endregion Constructors
    }
}