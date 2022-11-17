using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPA.Office365.API.Attributes
{
    /// <summary>
    /// usepa receivers and handlers.
    /// </summary>
    public static class AzureWebHookConstants
    {
        /// <summary>
        /// Gets the name of the usepa WebHook receiver.
        /// </summary>
        public static string ReceiverName => "usepa";

        /// <summary>
        /// Gets the name of the header containing the usepa event name.
        /// </summary>
        public static string EventHeaderName => "X-Usepa-Event";

        /// <summary>
        /// Gets the JSON path of the property in an Azure Alert WebHook request body containing the Azure Alert event
        /// name. Matches the Azure Monitoring rule name.
        /// </summary>
        public static string EventBodyPropertyPath => "$.data.essentials.alertId";

    }
}
