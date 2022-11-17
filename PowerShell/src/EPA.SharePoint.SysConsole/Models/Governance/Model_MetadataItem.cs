using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Governance
{
    public class Model_MetadataItem : SPListItemDefinition
    {

        public string SiteUrl { get; set; }
        public string SiteUrlTrailingSlash { get; set; }

        public string Region { get; set; }

        public string SiteType { get; set; }

        public bool HasMetadata { get; set; }

        public Nullable<int> MetadataItems { get; set; }

        public string RequestCompletedFlag { get; set; }

        public string SiteRequestId { get; set; }

        public string CollectionSiteType { get; set; }
        public Nullable<int> CollectionSiteTypeId { get; set; }

        public bool SiteExists { get; set; }

        public string WebGuid { get; set; }

        /// <summary>
        /// Email Sent
        /// </summary>
        public bool EmailSentFlag { get; set; }

        public bool CanBeRemoved { get; set; }

        #region Used to process list items

        public bool PrcInserted { get; set; }

        public bool PrcUpdated { get; set; }

        public bool PrcRemoved { get; set; }

        #endregion

        public override string ToString()
        {
            return string.Format("ID {0} Title {1}", Id, SiteUrl);
        }
    }
}
