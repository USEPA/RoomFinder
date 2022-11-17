using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Scan
{
    public class ScanAddInModel
    {
        public ScanAddInModel() { }

        public ScanAddInModel(AppInstance instance)
        {
            this.SiteId = instance.SiteId;
            this.WebId = instance.WebId;
            this.AppPrincipalId = instance.AppPrincipalId;
            this.AppWebFullUrl = instance.AppWebFullUrl;
            this.Id = instance.Id;
            this.ImageFallbackUrl = instance.ImageFallbackUrl;
            this.ImageUrl = instance.ImageUrl;
            this.InError = instance.InError;
            this.ProductId = instance.ProductId;
            this.RemoteAppUrl = instance.RemoteAppUrl;
            this.SettingsPageUrl = instance.SettingsPageUrl;
            this.StartPage = instance.StartPage;
            this.Status = instance.Status;
            this.Title = instance.Title;
        }

        /// <summary>
        /// Creates an app redirect and initializes the instance
        /// </summary>
        /// <param name="parentUrl">a string containing the host web URL; Ensure it has a trailing slash</param>
        /// <param name="instance">an app instance initialized via CSOM</param>
        public ScanAddInModel(string parentUrl, AppInstance instance) : this(instance)
        {
            this.HostWebUrl = parentUrl;
            this.AppRedirectUrl = string.Format("{0}_layouts/15/appredirect.aspx?instance_id={1}", parentUrl, instance.Id.ToString("B"));
        }

        public string HostWebUrl { get; }

        public string AppRedirectUrl { get; }

        public string AppPrincipalId { get; }

        public string AppWebFullUrl { get; }

        public Guid Id { get; }

        public string ImageFallbackUrl { get; }

        public string ImageUrl { get; }

        public bool InError { get; }

        public Guid ProductId { get; }

        public string RemoteAppUrl { get; }

        public string SettingsPageUrl { get; }

        public Guid SiteId { get; }

        public string StartPage { get; }

        public AppInstanceStatus Status { get; }

        public string Title { get; }

        public Guid WebId { get; }



        public AddInEnum HostedType { get; set; }


        public string EulaUrl { get; private set; }

        public string PrivacyUrl { get; private set; }

        public string Publisher { get; private set; }

        public string ShortDescription { get; private set; }

        public string SupportUrl { get; private set; }

        public void LoadAppDetails(Microsoft.SharePoint.Packaging.AppDetails appInstanceDetails)
        {
            this.SupportUrl = appInstanceDetails.SupportUrl;
            this.EulaUrl = appInstanceDetails.EulaUrl;
            this.PrivacyUrl = appInstanceDetails.PrivacyUrl;
            this.Publisher = appInstanceDetails.Publisher;
            this.ShortDescription = appInstanceDetails.ShortDescription;
        }

        /// <summary>
        /// Does the App Permissions collection contain elements
        /// </summary>
        [JsonIgnore()]
        public bool HasPermissions
        {
            get
            {
                if (AppPermissions != null)
                    return AppPermissions.Any();
                return false;
            }
        }

        /// <summary>
        /// Collection of permissions
        /// </summary>
        public List<string> AppPermissions { get; set; }

        public void LoadAppPermissions(string[] appInstancePermissions)
        {
            this.AppPermissions = new List<string>();
            this.AppPermissions.AddRange(appInstancePermissions);
        }

        /// <summary>
        /// Print the add-in details including the title, app web, and remote web
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            try
            {
                var _msg = String.Format("ID:{0} Title {1} AppWeb:{2} RemoteWeb:{3}", this.Id, this.Title, this.AppWebFullUrl, this.RemoteAppUrl);
                return _msg;
            }
            catch { }
            return base.ToString();
        }
    }
}
