using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models
{
    /// <summary>
    /// Represents the column metadata
    /// </summary>
    public class SPListItemFieldDefinition
    {
        /// <summary>
        /// Internal Field Name
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Field values
        /// </summary>
        public dynamic FieldValue { get; set; }
    }
}
