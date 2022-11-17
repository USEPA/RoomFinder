using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenDefinitions;
using System;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    public class ProvisioningGroupToken : TokenDefinition
    {
        private static bool CacheSearched;
        private static string localKey;

        public ProvisioningGroupToken(Web web, string key)
            : base(web, string.Format("~Groupname:{0}", key), string.Format("{{Groupname:{0}}}", key))
        {
            localKey = key;
        }

        public override string GetReplaceValue()
        {
            if (string.IsNullOrEmpty(CacheValue) && !CacheSearched)
            {
                var groups = Web.Context.LoadQuery(Web.SiteGroups.Where(w => w.Title == localKey).Include(sg => sg.Id));
                Web.Context.ExecuteQueryRetry();

                if (groups != null && groups.Any())
                {
                    CacheValue = "" + groups.FirstOrDefault().Id;
                }

                CacheSearched = true;
            }

            return CacheValue;
        }
    }
}
