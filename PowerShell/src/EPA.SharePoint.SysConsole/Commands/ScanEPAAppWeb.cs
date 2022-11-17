using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Apps;
using Microsoft.SharePoint.Client;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEpaAppWeb", HelpText = "scans app or host web for governance intrusion detection.")]
    public class ScanEPAAppWebOptions : BaseApplicationScanningOptions
    {
        /// <summary>
        /// The FQDN of the SharePoint site to scan
        /// </summary>
        [Option('s', "siteurl", Required = true, SetName = "Main", HelpText = "The url to be scanned")]
        public string SiteUrl { get; set; }
    }

    public static class ScanEPAAppWebExtensions
    {
        public static int RunGenerateAndReturnExitCode(this ScanEPAAppWebOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPAAppWeb(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Description: This tool scans a SharePoint site(s) for apps and solutions that may require full application scan";
    ///     Usage: Scan-EPAAppWeb [option]. See options below:";
    ///          ScanEPAAppWeb -All - This will scan all site collections and their respective subsites";
    ///          ScanEPAAppWeb -Site https://usepa.sharepoint.com/sites/Test - This will scan the site only";
    ///          ScanEPAAppWeb -SiteSites https://usepa.sharepoint.com/sites/Test - This will scan the site and its respective subsies";
    ///     If you're scanning a SharePoint Addin, use the Site option with the addin's app web url ex: 
    ///          ScanEPAAppWeb -Site https://testusepa-94fe92bbee9918.sharepoint.com/AddinName ";
    /// </summary>
    /// <remarks>
    /// 
    /// # Site Collection app scan
    /// #   ScanEPAAppWeb --Site-Url "https://usepa.sharepoint.com/sites/oei_ats/" --SkipLog --EvaluateLibraries --Deepscan --Verbose
    /// # 
    /// # Subsite app scan
    /// #   ScanEPAAppWeb --Site-Url "https://usepa.sharepoint.com/sites/oei_community/ittraining" --SkipLog --Deepscan --Verbose
    /// # 
    /// #  Provider Hosted Add-In [production]
    /// #   ScanEPAAppWeb --Site-Url "https://usepa-605a41dd4e822d.sharepoint.com/sites/OEI_Development/EPASharePointWebHelpDeskPHA/" --SkipLog --Deepscan --Verbose
    /// # 
    /// #  SharePoint Hosted Add-In [test tenant]
    /// #   ScanEPAAppWeb --Site-Url "https://testusepa-9f825bde6e24be.sharepoint.com/sites/TestAppScan/Employee%20Spotlight" --SkipLog --Deepscan --Verbose
    /// # 
    /// # Provider Hosted Add-In
    /// #   ScanEPAAppWeb --Site-Url "https://testusepa-9f825bde6e24c1.sharepoint.com/sites/TestAppScan/VirtoFormsApp" --SkipLog --Deepscan --Verbose
    /// 
    /// </remarks>
    public class ScanEPAAppWeb : BaseSpoApplicationScanning<ScanEPAAppWebOptions>
    {
        public ScanEPAAppWeb(ScanEPAAppWebOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var Url = Settings.Commands.TenantAdminUrl;
            var ClientId = Settings.SpoAddInMakeEPASite.ClientId;
            var ClientSecret = Settings.SpoAddInMakeEPASite.ClientSecret;

            Settings.AzureAd.SPClientID = ClientId;
            Settings.AzureAd.SPClientSecret = ClientSecret;

            LogVerbose($"AppCredential connecting and setting Add-In keys");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Url), null, ClientId, ClientSecret, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            try
            {
                // Scan the site collection / web and process results
                var scanResult = ProcessSiteCollection(Opts.SiteUrl);

                // ensure the current app is registered in the app catalog and authorized in the tenant
                var sharpAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
                using (var applicationSiteContext = sharpAuthManager.GetAppOnlyAuthenticatedContext(_appSiteUrl, Settings.AzureAd.SPClientID, Settings.AzureAd.SPClientSecret))
                {

                    var applicationWeb = applicationSiteContext.Web;
                    var applicationScanList = applicationWeb.Lists.GetByTitle(ApplicationScan_Constants.FlaggedSiteListName);

                    applicationSiteContext.Load(applicationWeb);
                    applicationSiteContext.Load(applicationScanList);
                    applicationSiteContext.ExecuteQueryRetry();
                    AddFlaggedSitetoList(applicationSiteContext, applicationScanList, scanResult, Opts.SiteUrl);
                }
                return 1;
            }
            catch (Exception e)
            {
                LogWarning("Failed to process application URL {0} with message {1}", Opts.SiteUrl, e.Message);
            }
            return -1;
        }
    }
}