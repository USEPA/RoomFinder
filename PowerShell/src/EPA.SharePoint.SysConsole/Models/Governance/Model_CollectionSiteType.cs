using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Governance
{
    public class Model_CollectionSiteType : SPListItemDefinition
    {

        public Model_CollectionSiteType() : base()
        {
            this.ColumnValues = new List<SPListItemFieldDefinition>();
        }

        public string TemplateName { get; set; }

        public string Name1 { get; set; }

        public bool ShowInDropDown { get; set; }

        public string CollectionURL { get; set; }
        public string CollectionURLTrailingSlash { get; set; }

        public string SiteCollection1 { get; set; }

        public int SiteCollection1Id { get; set; }

        public string SiteType { get; set; }

        public int SiteTypeId { get; set; }


        public override string ToString()
        {
            return string.Format("ID {0} URL=>{1}", Id, CollectionURL);
        }
    }
}
