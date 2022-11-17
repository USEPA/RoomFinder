using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebHooks;

namespace EPA.Office365.API.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AzureWebHookAttribute : WebHookAttribute
    {
        public AzureWebHookAttribute(string receiverName) : base(receiverName)
        {
        }

        public AzureWebHookAttribute()
            : base(AzureWebHookConstants.ReceiverName)
        {
        }
    }
}
