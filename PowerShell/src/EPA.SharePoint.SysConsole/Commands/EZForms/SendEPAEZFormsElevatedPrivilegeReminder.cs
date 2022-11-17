using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using OfficeDevPnP.Core.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("sendEPAEZFormsElevatedPrivilegeReminder", HelpText = "will send email reminders for ezform requests to renew elevated privileges.")]
    public class SendEPAEZFormsElevatedPrivilegeReminderOptions : CommonOptions
    {
        /// <summary>
        /// The directory where the notification schedule should be written/read
        /// </summary>
        [Option("log-directory", Required = true)]
        public string LogDirectory { get; set; }

        /// <summary>
        /// Used as a first run option to ensure all EPASS forms have valid data
        /// </summary>
        [Option("initialize", Required = false)]
        public bool Initialize { get; set; }
    }

    public static class SendEPAEZFormsElevatedPrivilegeReminderOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this SendEPAEZFormsElevatedPrivilegeReminderOptions opts, IAppSettings appSettings)
        {
            var cmd = new SendEPAEZFormsElevatedPrivilegeReminder(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will send EPASS notifications
    ///     1 Will query the list for any EPASS requests that are Permanent
    ///     2 Will set listitems to ensure user can recert in browser
    ///     3 Will email user on the appropriate timeline [30/7/1]
    /// Query for all Accounts that are Denied or have Past Expiration
    ///     Set a Recertification Workflow Row for Denial
    /// </summary>
    public class SendEPAEZFormsElevatedPrivilegeReminder : BaseEZFormsRecertification<SendEPAEZFormsElevatedPrivilegeReminderOptions>
    {
        public SendEPAEZFormsElevatedPrivilegeReminder(SendEPAEZFormsElevatedPrivilegeReminderOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        internal string FormatUrl
        {
            get
            {
                string mFormatUrl = "{0}/SitePages/Edit%20Request{2}.aspx?requestID={1}&Source={0}/Lists/Access%20Requests/My%20Requests.aspx";
                return mFormatUrl;
            }
        }

        internal string NotificationJsonPath { get; private set; }

        /// <summary>
        /// Contains the JSON representation of all notifications sent by the system
        /// </summary>
        internal protected EzFormsNotifications NotificationJson { get; private set; }

        /// <summary>
        /// Permanent (Reviewed annually)
        /// </summary>
        internal const string timespanconstant = "Permanent (Reviewed annually)";

        #endregion

        /// <summary>
        /// Check path for existence
        /// </summary>
        public override void OnBeforeRun()
        {
            base.OnBeforeRun();

            // check if the dump directory exists
            if (!System.IO.Directory.Exists(Opts.LogDirectory))
            {
                throw new System.IO.DirectoryNotFoundException(string.Format("The directory {0} could not be found.", Opts.LogDirectory));
            }
            
            NotificationJson = new EzFormsNotifications();

            // Read in the JSON file for existing notifications
            NotificationJsonPath = System.IO.Path.Combine(Opts.LogDirectory, "ElevatedPrivilegesNotificationSchedule.json");
            LogVerbose("Reading in JSON notification file from the following Path {0}", NotificationJsonPath);
            if (System.IO.File.Exists(NotificationJsonPath))
            {
                NotificationJson = Newtonsoft.Json.JsonConvert.DeserializeObject<EzFormsNotifications>(System.IO.File.ReadAllText(NotificationJsonPath));
            }

            // Relay email reminders to appropriate groups
            //EmailBCC.Add("EZTech_AD_Admins@epa.gov");
        }

        public override int OnRun()
        {

            var emailNotificationOutput = new List<EPASSReminderModel>();


            // Save when it occurred
            NotificationJson.RunOccurrences.Add(UtcIsoDate);


            var siteUrl = this.ClientContext.Url;
            
            var utcIso30DaysFromToday = UtcIsoDate.AddHours(ThirtyDaysSpan.TotalHours);

            var camlFieldRefs = AccessRequestCamlFieldRefs;
            camlFieldRefs.Add(EzForms_AccessRequest.Field_Reason_x0020_for_x0020_Exclusion);
            camlFieldRefs.Add(EzForms_AccessRequest.Field_Permanent_x0020_Access_x0020_Req);
            camlFieldRefs.Add(EzForms_AccessRequest.Field_Previous_x0020_Routing_x0020_Pha);
            camlFieldRefs.Add(EzForms_AccessRequest.Field_Modified);

            // The View XML for the requisite fields
            var camlViewClause = CAML.ViewFields(camlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());

            // setup Access list object
            var accessRequestList = this.ClientContext.Web.Lists.GetByTitle(EzForms_AccessRequest.ListName);
            this.ClientContext.Load(accessRequestList);
            this.ClientContext.ExecuteQueryRetry();



            #region Update any request without a proper recertification date

            // this should be a one time run to ensure the next date is populated
            var noRecertDateOutput = new List<EPASSReminderModel>();

            // CAML Queries for all Approved requests that are within 30 days
            var camlNoRecertificationDateWhereClause = CAML.And(
                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, "Text", EzForms_RequestType_Constants.Elevated_Privileges)),
                CAML.And(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Routing_x0020_Phase, "Text", EzForms_Constants.Approved)),
                    CAML.And(
                        CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Permanent_x0020_Access_x0020_Req, "Text", timespanconstant)),
                        CAML.IsNull(CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate))
                    )
                )
            );


            var camlNoRecertQuery = new CamlQuery()
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, CAML.Where(camlNoRecertificationDateWhereClause), "", camlViewClause, 100),
                ListItemCollectionPosition = null
            };

            while (true)
            {
                LogDebugging("Query: {0} with item position: {1}", camlNoRecertQuery.ViewXml, (camlNoRecertQuery?.ListItemCollectionPosition?.PagingInfo ?? "initial"));
                var spListItems = accessRequestList.GetItems(camlNoRecertQuery);
                this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                this.ClientContext.ExecuteQueryRetry();
                camlNoRecertQuery.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    var foundTimestamp = false;
                    var requestId = requestItem.Id;
                    var employee = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Employee);
                    var requestDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Date).ToNullableDatetime();
                    var lastModified = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Modified).ToDateTime();
                    var nextCertificationDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezformsNextCertifyDate).ToNullableDatetime();
                    var activityLog = RetrieveActivityLog(requestItem, requestId);
                    LogVerbose("Activity {0}", activityLog.Count());

                    if (activityLog.Any(al => al.RoutingPhase == EzForms_Constants.Business_Relationship_Manager))
                    {
                        // Find the BRM approval date and convert this to the last action taken
                        foundTimestamp = true;
                        var activity = activityLog.LastOrDefault(fd => fd.RoutingPhase == EzForms_Constants.Business_Relationship_Manager);
                        lastModified = activity.ActivityDate.ToDateTime();
                        LogWarning("Date {0} and converted {1}", activity.ActivityDate, lastModified);
                    }
                    else if (requestDate.HasValue)
                    {
                        // if no BRM date exists [which shouldn't be the case] then use the original request date
                        lastModified = requestDate.Value;
                    }

                    noRecertDateOutput.Add(new EPASSReminderModel()
                    {
                        ID = requestId,
                        LastAction = lastModified,
                        RequestDate = requestDate,
                        ActivityStamp = foundTimestamp,
                        ExistingCertificationDate = nextCertificationDate,
                        DisplayName = employee.ToUserValue(),
                        EmployeeEmail = employee.ToUserEmailValue()
                    });
                }

                if (camlNoRecertQuery.ListItemCollectionPosition == null)
                {
                    break;
                }
            }

            // Process items without a Recertification date
            if (this.ShouldProcess(string.Format("Updating requests [{0}] without a recertification date", noRecertDateOutput.Count())))
            {
                // Any invalid dates in SP, lets update
                foreach (var item in noRecertDateOutput)
                {
                    var requestId = item.ID;
                    var lastAction = item.LastAction;
                    ListItem requestItem = accessRequestList.GetItemById(requestId);

                    var day365span = new TimeSpan(365, 0, 0, 0); // lets add 365 days to whatever date the form was approved
                    var nextCertifyDate = lastAction.Add(day365span);
                    item.ExistingCertificationDate = nextCertifyDate;

                    // Lets update the record with the next certify date if its blank
                    requestItem[EzForms_AccessRequest.Field_ezformsNextCertifyDate] = nextCertifyDate;
                    requestItem.SystemUpdate();
                    requestItem.Context.ExecuteQueryRetry();
                }
            }

            #endregion



            #region Set all [approved] requests within 30 days to Recertification

            // initialize collection of items to be updated
            var thirtyApprovedOutput = new List<ElevatedRecertifyItem>();

            // CAML Queries for all Approved requests that are within 30 days
            var caml30approvedWhereClauseNeedToRecertify = CAML.And(
                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, "Text", EzForms_RequestType_Constants.Elevated_Privileges)),
                CAML.And(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Routing_x0020_Phase, "Text", EzForms_Constants.Approved)),
                    CAML.And(
                        CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Permanent_x0020_Access_x0020_Req, "Text", timespanconstant)),
                        CAML.And(
                            // UTC+30 Days <= Recertification
                            CAML.Geq(string.Format("{0}<Value Type='DateTime' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{1}</Value>",
                                CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), UtcIsoDate.ToString(base.ZeroHourFormatString))),
                            // Today >= Recertification
                            CAML.Leq(string.Format("{0}<Value Type='DateTime' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{1}</Value>",
                                CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), utcIso30DaysFromToday.ToString(base.ZeroHourFormatString)))
                        )
                    )
                )
            );

            // get ezforms site and query the list for approved requests
            ListItemCollectionPosition itemCollectionPosition = null;
            var caml30approvedQuery = new CamlQuery()
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, CAML.Where(caml30approvedWhereClauseNeedToRecertify), "", camlViewClause, 100)
            };

            while (true)
            {
                LogDebugging("Query: {0} with item position: {1}", caml30approvedQuery.ViewXml, (itemCollectionPosition == null ? "initial" : itemCollectionPosition.PagingInfo));
                caml30approvedQuery.ListItemCollectionPosition = itemCollectionPosition;
                var spListItems = accessRequestList.GetItems(caml30approvedQuery);
                this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                this.ClientContext.ExecuteQueryRetry();
                itemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    var requestId = requestItem.Id;
                    thirtyApprovedOutput.Add(new ElevatedRecertifyItem()
                    {
                        ID = requestId,
                        dottedAccount = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezFormsADAccount),
                        routing = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Routing_x0020_Phase),
                        recertDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezformsNextCertifyDate)
                    });
                }

                if (itemCollectionPosition == null)
                {
                    break;
                }
            }

            // Process items into a Recertification state
            if (this.ShouldProcess(string.Format("Updating requests [{0}] with recertification status", thirtyApprovedOutput.Count())))
            {
                foreach (var spItem in thirtyApprovedOutput)
                {
                    LogWarning("ListItem [{0}] User:{1} Recertification:{2} will be updated.", spItem.ID, spItem.dottedAccount, spItem.recertDate);

                    try
                    {
                        var requestItem = accessRequestList.GetItemById(spItem.ID);
                        requestItem[EzForms_AccessRequest.Field_Request_x0020_Status] = EzForms_Constants.Recertification;
                        requestItem[EzForms_AccessRequest.Field_Routing_x0020_Phase] = EzForms_Constants.Recertification;
                        requestItem[EzForms_AccessRequest.Field_Previous_x0020_Routing_x0020_Pha] = spItem.routing;
                        requestItem.SystemUpdate();
                        this.ClientContext.ExecuteQueryRetry();
                        spItem.updated = true;
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Migrate failed for list items MSG:{0}", ex.Message);
                    }
                }
            }


            #endregion



            #region Process Email notifications to the users


            // CAML Queries for all Approved requests that are within 30 days
            var camlWhereClauseNeedToRecertify = CAML.And(
                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, "Text", EzForms_RequestType_Constants.Elevated_Privileges)),
                CAML.And(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Routing_x0020_Phase, "Text", EzForms_Constants.Recertification)),
                    CAML.And(
                        CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Permanent_x0020_Access_x0020_Req, "Text", timespanconstant)),
                        CAML.And(
                            // UTC+30 Days <= Recertification
                            CAML.Geq(string.Format("{0}<Value Type='DateTime' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{1}</Value>",
                                CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), UtcIsoDate.ToString(base.ZeroHourFormatString))),
                            // Today >= Recertification
                            CAML.Leq(string.Format("{0}<Value Type='DateTime' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{1}</Value>",
                                CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), utcIso30DaysFromToday.ToString(base.ZeroHourFormatString)))
                        )
                    )
                )
            );

            // get ezforms site and query the list for approved requests
            var camlThirtyOrLessQuery = new CamlQuery()
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, CAML.Where(camlWhereClauseNeedToRecertify), "", camlViewClause, 100),
                ListItemCollectionPosition = null
            };

            while (true)
            {
                LogDebugging("Query: {0} with item position: {1}", camlThirtyOrLessQuery.ViewXml, (camlThirtyOrLessQuery?.ListItemCollectionPosition?.PagingInfo ?? "initial"));
                var spListItems = accessRequestList.GetItems(camlThirtyOrLessQuery);
                this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                this.ClientContext.ExecuteQueryRetry();
                camlThirtyOrLessQuery.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    var foundTimestamp = false;
                    var requestId = requestItem.Id;
                    var employee = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Employee);
                    var requestDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Date).ToNullableDatetime();
                    var lastModified = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Modified).ToDateTime();
                    var previousRouting = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Previous_x0020_Routing_x0020_Pha);
                    var nextCertificationDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezformsNextCertifyDate).ToNullableDatetime();
                    var requestType = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Type);
                    var activityLog = RetrieveActivityLog(requestItem, requestId);


                    emailNotificationOutput.Add(new EPASSReminderModel()
                    {
                        ID = requestId,
                        LastAction = lastModified,
                        RequestDate = requestDate,
                        ActivityStamp = foundTimestamp,
                        ExistingCertificationDate = nextCertificationDate,
                        DisplayName = employee.LookupValue,
                        EmployeeEmail = employee.Email,
                        RequestType = requestType
                    });
                }

                if (camlThirtyOrLessQuery.ListItemCollectionPosition == null)
                {
                    break;
                }
            }


            foreach (var item in emailNotificationOutput)
            {
                var requestId = item.ID;
                var notificationType = NotificationType.NONE;
                var emailSentTo = new List<string>(); // reset the recepient email array
                var emailHeader = string.Empty;
                var emailSubject = string.Empty;
                var emailMessageSentFlag = false; // indicates an email has been sent

                // perform some lookup parsing to see if we should send an email
                var requestNotification = NotificationJson.Notifications.FirstOrDefault(ab => ab.SPId == requestId);

                // At this point in the logic a Recertification Date should be populated
                DateTime lastAction = item.ExistingCertificationDate.Value;


                var spanDifference = ((TimeSpan)lastAction.Subtract(UtcIsoDate));
                if (spanDifference.TotalHours >= 0 && spanDifference < OneDaySpan)
                {
                    // email 1 day reminder
                    notificationType = NotificationType.OneDay;
                    emailSubject = string.Format("{0}: Final reminder.", item.RequestType);
                    emailHeader = string.Format("{0} reminder.  This is the last reminder.  Your exemption will be disabled tomorrow.", item.RequestType);

                    // notification has occurred for a last day notice
                    emailMessageSentFlag = (requestNotification != null && requestNotification.schedule.Any(sch => sch.Notification == notificationType));
                    if (emailMessageSentFlag)
                    {
                        // last run instance for this OneDay notification
                        var lastNotification = requestNotification.schedule.Where(sch => sch.Notification == notificationType).OrderByDescending(ob => ob.NotificationDateTime).FirstOrDefault();
                        if (lastNotification != null && UtcIsoDate.Subtract(lastNotification.NotificationDateTime).TotalMinutes > 15)
                        {
                            emailMessageSentFlag = false;
                        }
                    }
                }
                else if (spanDifference.TotalHours > 0 && spanDifference < SevenDaysSpan)
                {
                    // email 7 day reminder
                    notificationType = NotificationType.SevenDays;
                    emailSubject = string.Format("{0}: 7 day notice.", item.RequestType);
                    emailHeader = string.Format("{0} reminder.  Your exemption will be removed in 7 day(s).", item.RequestType);

                    // notification has occurred for a 7 day notice
                    emailMessageSentFlag = (requestNotification != null && requestNotification.schedule.Any(sch => sch.Notification == notificationType));
                    if (emailMessageSentFlag)
                    {
                        // last run instance for this 7 day notification [check if notification has occurred in the new schedule]
                        var lastNotification = requestNotification.schedule.Where(sch => sch.Notification == notificationType).OrderByDescending(ob => ob.NotificationDateTime).FirstOrDefault();
                        if (lastNotification != null && UtcIsoDate.Subtract(lastNotification.NotificationDateTime) > SevenDaysSpan)
                        {
                            emailMessageSentFlag = false;
                        }
                    }
                }
                else if (spanDifference.TotalHours > 0 && spanDifference < ThirtyDaysSpan)
                {
                    // email 30 day reminder
                    notificationType = NotificationType.ThirtyDays;
                    emailSubject = string.Format("{0}: 30 day notice.", item.RequestType);
                    emailHeader = string.Format("{0} reminder.  Your exemption will be removed in 30 day(s).", item.RequestType);

                    // notification has occurred for a 30 day notice
                    emailMessageSentFlag = (requestNotification != null && requestNotification.schedule.Any(sch => sch.Notification == notificationType));
                    if (emailMessageSentFlag)
                    {
                        // last run instance for this 30 day notification [icheck if notification has occurred in the new schedule]
                        var lastNotification = requestNotification.schedule.Where(sch => sch.Notification == notificationType).OrderByDescending(ob => ob.NotificationDateTime).FirstOrDefault();
                        if (lastNotification != null && UtcIsoDate.Subtract(lastNotification.NotificationDateTime) > ThirtyDaysSpan)
                        {
                            emailMessageSentFlag = false;
                        }
                    }
                }


                if (notificationType != NotificationType.NONE
                    && emailMessageSentFlag != true
                    && this.ShouldProcess(string.Format("Emailing user {0} regarding {1}", item.DisplayName, emailSubject)))
                {
                    try
                    {
                        // assumption is that by the time this gets to this point the date has been populated
                        var _expirationDate = lastAction.ToString("dddd dd MMMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));

                        var emailString = string.Empty;
                        var emailMessage = new StringBuilder();
                        emailMessage.Append("<div>");
                        emailMessage.Append("<div><br></div>");
                        emailMessage.AppendFormat("<div>Employee {0},</div>", item.DisplayName);
                        emailMessage.AppendFormat("<div>{0}</div>", emailHeader);
                        emailMessage.AppendFormat("<div>A recertification for your {0} request is approaching.</div>", item.RequestType);
                        emailMessage.AppendFormat("<div>You will be required to renew your request before {0}.</div>", _expirationDate);
                        emailMessage.AppendFormat("<div><a href=\"{0}\" title=\"click here to open the form\">Click here to view</a>", string.Format(FormatUrl, siteUrl, item.ID, ""));
                        emailMessage.Append("<div><br></div>");
                        emailMessage.Append("<div>Thank you,</div>");
                        emailMessage.Append("<div>EZ Forms Support</div>");
                        emailMessage.Append("</div>");
                        emailString = emailMessage.ToString();

                        //userEmail = "leonard.shawn@epa.gov";
                        var properties = new EmailProperties();
                        properties.To = new string[] { item.EmployeeEmail };
                        properties.BCC = EmailBCC;
                        properties.Subject = emailSubject;
                        properties.Body = emailString;

                        Microsoft.SharePoint.Client.Utilities.Utility.SendEmail(this.ClientContext, properties);
                        this.ClientContext.ExecuteQueryRetry();


                        // Add the notification to the log entry
                        if (requestNotification != null)
                        {
                            requestNotification.schedule.Add(new EzFormsNotifySchedule()
                            {
                                Notification = notificationType,
                                NotificationDateTime = UtcIsoDate
                            });
                        }
                        else
                        {
                            var entity = new EzFormsNotification()
                            {
                                SPId = item.ID,
                                email = item.EmployeeEmail
                            };
                            entity.schedule.Add(new EzFormsNotifySchedule()
                            {
                                Notification = notificationType,
                                NotificationDateTime = UtcIsoDate
                            });
                            NotificationJson.Notifications.Add(entity);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning("Failed to send an email {0} MSG {1}", item.EmployeeEmail, ex.Message);
                    }
                }
            }

            #endregion


            // Write the JSON file locally
            System.IO.File.WriteAllText(NotificationJsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(NotificationJson));

            LogVerbose($"Ran {emailNotificationOutput.Count} notifications");
            return 1;
        }
    }

    internal class ElevatedRecertifyItem
    {
        internal string recertDate { get; set; }
        internal bool updated { get; set; }

        public int ID { get; set; }
        public string dottedAccount { get; set; }
        public string routing { get; set; }
    }

    internal class EPASSReminderModel
    {
        public int ID { get; set; }
        public DateTime LastAction { get; set; }
        public Nullable<DateTime> RequestDate { get; set; }
        public bool ActivityStamp { get; set; }
        public Nullable<DateTime> ExistingCertificationDate { get; set; }
        public string DisplayName { get; set; }
        public string EmployeeEmail { get; set; }
        public string RequestType { get; set; }
    }
}
