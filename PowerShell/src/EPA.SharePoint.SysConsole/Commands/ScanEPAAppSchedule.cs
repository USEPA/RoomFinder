using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Apps;
using EPA.SharePoint.SysConsole.Models.Scan;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEpaAppSchedule", HelpText = "scans active app requests based on schedule.")]
    public class ScanEPAAppScheduleOptions : BaseApplicationScanningOptions
    {
    }

    public static class ScanEPAAppScheduleOptionsExtensions
    {
        public static int RunGenerateAndReturnExitCode(this ScanEPAAppScheduleOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPAAppSchedule(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Description: This tool scans a SharePoint site(s) for apps and solutions that may require full application scan";
    ///     Usage: Scan-EPAAppSchedule [option]. See options below:";
    ///          ScanEPAAppSchedule --Deepscan # This will scan the specified URL and it's respective subsites";
    ///          ScanEPAAppSchedule # This will scan the site only";
    ///          ScanEPAAppSchedule --SkipLog https://testusepa-94fe92bbee9918.sharepoint.com/AddinName ";
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CSOM Exceptions unknown")]
    public class ScanEPAAppSchedule : BaseSpoApplicationScanning<ScanEPAAppScheduleOptions>
    {
        public ScanEPAAppSchedule(ScanEPAAppScheduleOptions opts, IAppSettings settings)
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
                // the application must be registered in the Tenant Admin area and have access to the app catalog
                var sharpAuthManager = new OfficeDevPnP.Core.AuthenticationManager();
                using var applicationSiteContext = sharpAuthManager.GetAppOnlyAuthenticatedContext(_appSiteUrl, Settings.AzureAd.SPClientID, Settings.AzureAd.SPClientSecret);
                var applicationWeb = applicationSiteContext.Web;
                var applicationScanList = applicationWeb.Lists.GetByTitle(ApplicationScan_Constants.FlaggedSiteListName);

                applicationSiteContext.Load(applicationWeb);
                applicationSiteContext.Load(applicationScanList);
                applicationSiteContext.ExecuteQueryRetry();


                try
                {
                    var sharpTeamEmails = new List<string>();
                    var groupQuery = applicationSiteContext.LoadQuery(applicationSiteContext.Web.SiteGroups.Include(sgi => sgi.Id, sgi => sgi.Users).Where(sg => sg.LoginName == "SHARP Tiger Team"));
                    applicationSiteContext.ExecuteQueryRetry();

                    var groupDetails = groupQuery.FirstOrDefault();
                    if (groupDetails != null)
                    {
                        sharpTeamEmails.AddRange(groupDetails.Users.Where(w => !string.IsNullOrEmpty(w.Email)).Select(s => s.Email));
                    }

                    var viewFields = new string[] { "ID", "Created", "Request1", "Title", "InstanceID0" };
                    ListItemCollectionPosition itemPosition = null;
                    var viewFieldRefs = viewFields.Select(vf => CAML.FieldRef(vf)).ToArray();

                    var camlQuery = new CamlQuery
                    {
                        ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll,
                        CAML.Where(CAML.Eq(CAML.FieldValue("RowProcessed", FieldType.Boolean.ToString("f"), 0.ToString()))),
                        "",
                        CAML.ViewFields(viewFieldRefs),
                        50)
                    };

                    var scanningItems = new List<ScannedItems>();

                    while (true)
                    {
                        camlQuery.ListItemCollectionPosition = itemPosition;
                        var scanlistItems = applicationScanList.GetItems(camlQuery);
                        applicationSiteContext.Load(scanlistItems);
                        applicationSiteContext.ExecuteQueryRetry();

                        itemPosition = scanlistItems.ListItemCollectionPosition;

                        foreach (var scanlistItem in scanlistItems)
                        {
                            var requestId = default(Nullable<int>);
                            var scannedId = scanlistItem.Id;
                            var created = scanlistItem.RetrieveListItemValue("Created").ToDateTime();
                            var request = scanlistItem.RetrieveListItemValueAsLookup("Request1");
                            var _siteUrl = scanlistItem.RetrieveListItemValue("Title");
                            var _appInstanceId = scanlistItem.RetrieveListItemValue("InstanceID0");

                            if (request != null)
                            {
                                requestId = request.LookupId;
                            }

                            LogVerbose("ID:{0}", scannedId);

                            scanningItems.Add(new ScannedItems()
                            {
                                Id = scannedId,
                                Created = created,
                                RequestID = requestId,
                                SiteUrl = _siteUrl,
                                InstanceId = _appInstanceId,
                                TestTenant = !(IsProductionTenant(_siteUrl))
                            });
                        }

                        if (itemPosition == null)
                        {
                            break;
                        }
                    }

                    if (scanningItems.Any())
                    {
                        LogVerbose("Processing app scans for {0} requests", scanningItems.Count());

                        var sharpScanSuccess = false;
                        var sharpEmail = new StringBuilder();
                        sharpEmail.Append("<div>SHARP Tiger Team members,");
                        sharpEmail.Append("<p>The following requests were scanned:</p>");

                        foreach (var scanningItem in scanningItems.OrderBy(ob => ob.TestTenant).ThenBy(tb => tb.SiteUrl))
                        {
                            var scannedId = scanningItem.Id;
                            var scannedRequestId = scanningItem.RequestID;
                            var _siteUrl = scanningItem.SiteUrl;

                            // we are scanning the tenant for the specified URL
                            ScanResults scanResult = null;
                            if (string.IsNullOrEmpty(scanningItem.InstanceId))
                            {
                                scanResult = ProcessSiteCollection(_siteUrl);
                            }
                            else
                            {
                                scanResult = ProcessAddinsRequest(_siteUrl, scanningItem.InstanceId);
                            }

                            if (scanResult != null)
                            {
                                var requestUrl = string.Format("{0}Lists/Application%20Requests/DispForm.aspx?ID={1}", _appSiteUrl, scannedRequestId);

                                sharpEmail.AppendFormat("<div>A scan request for item <a href=\"{0}\">{0}</a> has completed.</div>", requestUrl);
                                if (scanResult.SPAddIn)
                                {
                                    if (scanResult.AppType == AddInEnum.ProviderHosted)
                                    {
                                        sharpEmail.AppendFormat("<div>Evaluated URL <a href=\"{0}\">{0}</a>.</div>", scanResult.Url);
                                        sharpEmail.Append("<div><strong>Please note:</strong> this is a Provider Hosted Add-In. You <strong>must</strong> review the permissions and html file found in the attachments.</div>");
                                    }
                                    else
                                    {
                                        sharpEmail.AppendFormat("<div>Scanned URL <a href=\"{0}\">{0}</a>.</div>", scanResult.Url);
                                    }
                                }
                                else
                                {
                                    sharpEmail.AppendFormat("<div>Scanned URL <a href=\"{0}\">{0}</a>.</div>", _siteUrl);
                                }
                                sharpEmail.Append("<p>The scan revealed: <ul>");
                                sharpEmail.AppendFormat("<li>{0} violations.</li>", scanResult.Violations.Count());
                                sharpEmail.AppendFormat("<li>{0} items requiring additional information.</li>", scanResult.Evaluations.Count());
                                sharpEmail.AppendFormat("<li>{0} permissions to evaluate.</li>", scanResult.Permissions.Count());
                                sharpEmail.Append("</ul></p>");
                                sharpEmail.Append("<div>Please review the scan results and process the request.</div>");
                                sharpEmail.Append("<div>-----------------------</div>");
                                sharpScanSuccess = true;
                            }

                            AddFlaggedSitetoList(applicationSiteContext, applicationScanList, scanResult, _siteUrl, scannedId);
                        }


                        sharpEmail.Append("<div>Regards,");
                        sharpEmail.Append("<div>Scheduled app scanner");
                        sharpEmail.Append("<div><small>This is an unmonitored email inbox.  Please do not reply.</small></div>");
                        sharpEmail.Append("</div>");

                        if (sharpScanSuccess)
                        {
                            var properties = new Microsoft.SharePoint.Client.Utilities.EmailProperties
                            {
                                To = sharpTeamEmails,
                                Subject = "SHARP Application Code Scanning Completed, Take Action",
                                Body = sharpEmail.ToString()
                            };
                            Microsoft.SharePoint.Client.Utilities.Utility.SendEmail(applicationSiteContext, properties);
                            applicationSiteContext.ExecuteQueryRetry();
                        }
                    }

                    return 1;
                }
                catch (Exception e)
                {
                    LogWarning("Failed to process scheduled scans {0}", e);
                }
            }
            catch (Exception e)
            {
                LogWarning("Failed to process application URL {0} with message {1}", _appSiteUrl, e.Message);
            }
            return -1;
        }
    }
}