using System.Xml.Linq;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    /// <summary>
    /// Contains constants for the provisioning agent
    /// </summary>
    public class ProvisioningConstants
    {
        /// <summary>
        /// http://schemas.usepa.com/ProvisioningExtensibilityHandlerConfiguration
        /// </summary>
        static public XNamespace configns = "http://schemas.usepa.com/ProvisioningExtensibilityHandlerConfiguration";

        /// <summary>
        /// i:0#.f|membership|
        /// </summary>
        static public string ClaimPrefix = "i:0#.f|membership|";

        /// <summary>
        /// PropertyBag: __NavigationShowSiblings
        /// </summary>
        static public string NavigationShowSiblings = "__NavigationShowSiblings";

        /// <summary>
        /// Default logging source
        /// </summary>
        static public string LOGGING_SOURCE = "OfficeDevPnP.Core";
    }
}
