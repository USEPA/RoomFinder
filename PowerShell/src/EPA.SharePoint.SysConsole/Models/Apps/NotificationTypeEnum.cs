using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    /// <summary>
    /// Identifies the type of notification that should be sent to the user
    /// </summary>
    public enum NotificationTypeEnum
    {
        /// <summary>
        /// Represents an notification that has gone beyond a reasonable timespan
        /// </summary>
        Archive_Alert,

        /// <summary>
        /// Represents an idle notification that is less than 14 days
        /// </summary>
        Idle_Approver_Alert
    }
}
