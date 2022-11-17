using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Scan
{
    public class ScanResults : ScanResultLogLines
    {
        public ScanResults() : base()
        {
            this.Messages = new List<ScanLog>();
            this.SubSites = new List<ScanResults>();
            this.AddIns = new List<ScanAddInModel>();
            this.CustomActions = new List<ScanCustomActionModel>();
            this.SiteOwners = new List<string>();
        }

        /// <summary>
        /// Initialize the scan results with the tenant information based on the URL
        /// </summary>
        /// <param name="_siteUrl"></param>
        public ScanResults(string _siteUrl) : this()
        {
            this.Url = _siteUrl.ToLower();
            if (this.Url.IndexOf("https://usepa") > -1)
            {
                SiteTenant = AddInTenantTypeEnum.Production;
            }
            else
            {
                SiteTenant = AddInTenantTypeEnum.Test;
            }
        }

        public string Url { get; set; }

        public string ServerRelativeUrl { get; set; }

        public string Title { get; set; }

        public bool HasClientCode { get; set; }

        /// <summary>
        /// Indicates if this is a production or test tenant
        /// </summary>
        public AddInTenantTypeEnum SiteTenant { get; set; }

        public List<string> SiteOwners { get; set; }

        public bool SPAddIn { get; set; }

        /// <summary>
        /// The web guid uniquely identifying the web
        /// </summary>
        public Guid WebId { get; set; }

        /// <summary>
        /// The site guid uniquely identifying the web
        /// </summary>
        public Guid SiteId { get; set; }

        /// <summary>
        /// Concatenates the component/technologies into semi-colon delimited collection
        /// </summary>
        public List<string> AppComponents
        {
            get
            {
                var components = new List<string>();
                if (HasClientCode)
                {
                    components.Add("Client Only Code Solution");
                }

                if (AddIns.Any(a => a.HostedType == AddInEnum.SharePointHosted))
                {
                    components.Add("SharePoint Hosted App");
                }

                if (AddIns.Any(a => a.HostedType == AddInEnum.ProviderHosted))
                {
                    components.Add("Provider Hosted App");
                }

                return components;
            }
        }

        /// <summary>
        /// Single option for how this application is hosted
        /// </summary>
        public AddInEnum AppType
        {
            get
            {
                var apptypes = AddInEnum.SPFramework;
                if (AddIns.Any(a => a.HostedType == AddInEnum.ProviderHosted))
                {
                    apptypes = AddInEnum.ProviderHosted;
                }
                else if (AddIns.Any(a => a.HostedType == AddInEnum.SharePointHosted))
                {
                    apptypes = AddInEnum.SharePointHosted;
                }

                return apptypes;
            }
        }

        /// <summary>
        /// Contains messages for the scanner
        /// </summary>
        public List<ScanLog> Messages { get; set; }


        public List<ScanResults> SubSites { get; set; }


        public List<ScanAddInModel> AddIns { get; set; }

        public List<ScanCustomActionModel> CustomActions { get; set; }
    }
}
