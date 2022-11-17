using System;
using System.Collections.Generic;
using System.Text;

namespace EPA.Office365.Graph
{
    /// <summary>
    /// Defines a Unified Group user
    /// </summary>
    public class UnifiedGroupUser
    {
        /// <summary>
        /// Unified group user's user principal name
        /// </summary>
        public String UserPrincipalName { get; set; }
        /// <summary>
        /// Unified group user's display name
        /// </summary>
        public String DisplayName { get; set; }

    }
}
