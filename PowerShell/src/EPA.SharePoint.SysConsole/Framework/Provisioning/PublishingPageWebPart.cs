using OfficeDevPnP.Core.Framework.Provisioning.Model;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class PublishingPageWebPart : WebPart
    {
        public string DefaultViewDisplayName { get; set; }

        public bool IsListViewWebPart
        {
            get
            {
                return DefaultViewDisplayName != null;
            }
        }

        /// <summary>
        /// The source file based on the WatcherDirectory
        /// </summary>
        public string Src { get; set; }
    }
}
