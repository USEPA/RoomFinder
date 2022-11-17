using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenDefinitions;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class ProvisioningSiteToken : TokenDefinition
    {
        public ProvisioningSiteToken(Web web)
            : base(web, "~fullsiteurl", "{fullsiteurl}")
        {
        }

        public override string GetReplaceValue()
        {
            if (string.IsNullOrEmpty(CacheValue))
            {

                if (!Web.IsPropertyAvailable(ctx => ctx.Url))
                {
                    Web.EnsureProperties(ctx => ctx.Url);
                }
                CacheValue = TokenHelper.EnsureTrailingSlash(Web.Url);
            }

            return CacheValue;
        }
    }
}
