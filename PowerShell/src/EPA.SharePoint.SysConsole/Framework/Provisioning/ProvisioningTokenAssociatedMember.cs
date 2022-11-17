using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenDefinitions;


namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class ProvisioningTokenAssociatedMember : TokenDefinition
    {
        private static bool CacheSearched;

        public ProvisioningTokenAssociatedMember(Web web)
            : base(web, "~membershipGroupname:AssociatedMemberGroup", "{membershipGroupname:AssociatedMemberGroup}")
        {
        }

        public override string GetReplaceValue()
        {
            if (string.IsNullOrEmpty(CacheValue) && !CacheSearched)
            {
                Web.Context.Load(Web, ctx => ctx.AssociatedMemberGroup);
                Web.Context.ExecuteQueryRetry();

                if (!Web.AssociatedMemberGroup.ServerObjectIsNull())
                {
                    CacheValue = "" + Web.AssociatedMemberGroup.Id;
                }

                CacheSearched = true;
            }

            return CacheValue;
        }
    }
}
