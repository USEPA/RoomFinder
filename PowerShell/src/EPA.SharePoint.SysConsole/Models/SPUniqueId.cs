using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models
{
    /// <summary>
    /// Represents the core component of any sharepoint list item
    /// </summary>
    public class SPUniqueId
    {
        /// <summary>
        /// Unique Integer for a SP Item
        /// </summary>
        public Nullable<Guid> Id { get; set; }

        /// <summary>
        /// Indicates if the item has unique permissions
        /// </summary>
        public bool HasUniquePermission { get; set; }
    }
}
