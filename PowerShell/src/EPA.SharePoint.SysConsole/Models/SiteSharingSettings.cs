using System;

namespace EPA.SharePoint.SysConsole.Models
{

    public class SiteSharingSettings
    {
        public Guid WebId { get; set; }

        public string Title { get; set; }

        public string TemplateId { get; set; }

        public string Region { get; set; }

        public string SiteType { get; set; }

        public string SiteUrl { get; set; }


        public bool CanMemberShare { get; set; }


        public override string ToString()
        {
            return $"{Region}|{SiteType}|{SiteUrl}|{CanMemberShare.ToString()}";
        }
    }
}
