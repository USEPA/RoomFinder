using EPA.SharePoint.SysConsole.Models;
using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EzFormsListItemModel : SPListItemDefinition
    {
        public EzFormsListItemModel() : base()
        {
            this.ColumnValues = new List<SPListItemFieldDefinition>();
        }

        public DateTime RequestDate { get; set; }

    }
}
