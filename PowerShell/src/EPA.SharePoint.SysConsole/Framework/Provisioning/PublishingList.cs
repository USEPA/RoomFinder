using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class PublishingList
    {
        public PublishingList()
        {
            this.ContentTypeBindings = new List<PublishingContentTypeBinding>();
        }

        public string ListUrl { get; set; }


        public List<PublishingContentTypeBinding> ContentTypeBindings { get; set; }


        public class PublishingContentTypeBinding
        {
            public PublishingContentTypeBinding()
            {
                this.FieldLinks = new List<PublishingFieldLink>();
            }

            public string Name { get; set; }

            public List<PublishingFieldLink> FieldLinks { get; set; }
        }

        public class PublishingFieldLink
        {
            public string InternalName { get; set; }
        }
    }
}
