using Microsoft.AspNetCore.WebHooks.Metadata;

namespace EPA.Office365.API.Attributes
{

    /// <summary>
    /// An <see cref="IWebHookMetadata"/> service containing metadata about the Azure Alert receiver.
    /// </summary>
    public class AzureWebhookMetadata : WebHookMetadata, IWebHookEventFromBodyMetadata, IWebHookVerifyCodeMetadata
    {
        public AzureWebhookMetadata()
            : base(AzureWebHookConstants.ReceiverName) { }

        public override WebHookBodyType BodyType => WebHookBodyType.Json;

        public bool AllowMissing => true;

        public string BodyPropertyPath => AzureWebHookConstants.EventBodyPropertyPath;
    }
}
