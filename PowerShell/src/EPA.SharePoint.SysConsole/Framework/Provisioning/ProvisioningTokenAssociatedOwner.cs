using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenDefinitions;


namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class ProvisioningTokenAssociatedOwner : TokenDefinition
    {
        private static bool CacheSearched;

        public ProvisioningTokenAssociatedOwner(Web web)
            : base(web, "~membershipGroupname:AssociatedOwnerGroup", "{membershipGroupname:AssociatedOwnerGroup}")
        {
        }

        public override string GetReplaceValue()
        {
            if (string.IsNullOrEmpty(CacheValue) && !CacheSearched)
            {
                Web.Context.Load(Web, ctx => ctx.AssociatedOwnerGroup);
                Web.Context.ExecuteQueryRetry();

                if (!Web.AssociatedOwnerGroup.ServerObjectIsNull())
                {
                    CacheValue = "" + Web.AssociatedOwnerGroup.Id;
                }

                CacheSearched = true;
            }

            return CacheValue;
        }
    }
}
