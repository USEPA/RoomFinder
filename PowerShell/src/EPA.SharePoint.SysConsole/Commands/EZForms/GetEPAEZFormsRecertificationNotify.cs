using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using Newtonsoft.Json;
using OfficeDevPnP.Core.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("getEPAEZFormsRecertificationNotify", HelpText = "get recertifications and notify users via email.")]
    public class GetEPAEZFormsRecertificationNotifyOptions : CommonOptions
    {
        /// <summary>
        /// The directory where the notification schedule should be written/read
        /// </summary>
        [Option("ScheduleDirectory", Required = true)]
        public string ScheduleDirectory { get; set; }
    }

    public static class GetEPAEZFormsRecertificationNotifyOptionsExtension
    {
        /// <summary>
        /// Will query ezforms and email elevated permission users
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this GetEPAEZFormsRecertificationNotifyOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAEZFormsRecertificationNotify(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will perform recertification notifications
    ///    1. Will set listitems to the appropriate config/variables to ensure user can recert in browser
    ///    2. Will email user on the appropriate timeline
    /// Query for all Accounts that are Denied or have Past Expiration
    ///     Set a Recertification Workflow Row for Denial
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Too much effort to handle exceptions.")]
    public class GetEPAEZFormsRecertificationNotify : BaseEZFormsRecertification<GetEPAEZFormsRecertificationNotifyOptions>
    {
        public GetEPAEZFormsRecertificationNotify(GetEPAEZFormsRecertificationNotifyOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region private variables

        internal string FormatUrl
        {
            get
            {
                string mFormatUrl = "{0}/SitePages/Edit%20Request{2}.aspx?requestID={1}&Source={0}/Lists/Access%20Requests/Recertification.aspx";
                return mFormatUrl;
            }
        }

        /// <summary>
        /// Contains the JSON representation of all notifications sent by the system
        /// </summary>
        internal protected EzFormsNotifications NotificationJson { get; private set; }

        #endregion

        /// <summary>
        /// Check path for existence
        /// </summary>
        public override void OnBeforeRun()
        {
            base.OnBeforeRun();

            if (!System.IO.Directory.Exists(Opts.ScheduleDirectory))
            {
                throw new System.IO.DirectoryNotFoundException(string.Format("The directory {0} could not be found.", Opts.ScheduleDirectory));
            }
        }

        /// <summary>
        /// Execute the CMD-LET effecting the change
        /// </summary>
        public override int OnRun()
        {
            // Where we will write the logs and JSON files
            var runPath = System.Reflection.Assembly.GetExecutingAssembly()?.Location;
            var jsonPath = String.Format("{0}\\{1}", Opts.ScheduleDirectory, "EzFormsNotificationSchedule.json");
            LogVerbose("Running the following {0} directory.", runPath);


            #region Create Recertification Rows and Notify Users d

            NotificationJson = new EzFormsNotifications();

            // Read in the JSON file for existing notifications
            LogVerbose("Reading in JSON notification file from the following Path {0}", jsonPath);
            if (System.IO.File.Exists(jsonPath))
            {
                NotificationJson = JsonConvert.DeserializeObject<EzFormsNotifications>(System.IO.File.ReadAllText(jsonPath));
            }

            // Set the list items to recertifications
            SetUsersWithin30DaysToRecertificationAndEmail();

            // Write the JSON file locally
            System.IO.File.WriteAllText(jsonPath, JsonConvert.SerializeObject(NotificationJson));

            #endregion


            #region Query and Assert Disabled Rows

            // set rows for processing
            SetUsersWhichShouldBeDenied();

            #endregion

            return 1;
        }

        /// <summary>
        /// Will set all list items within 30 days and not in a recertification state to a recertification status
        /// </summary>
        /// <param name="camlFieldRefs"></param>
        /// <param name="camlWhereClause">Base caml where filter for request type</param>
        /// <param name="listInSite"></param>
        /// <returns></returns>
        private void SetUsersWithin30DaysToRecertificationAndEmail()
        {
            var utcIso30DaysFromToday = UtcIsoDate.AddHours(ThirtyDaysSpan.TotalHours);

            // The View XML for the requisite fields
            var camlViewClause = CAML.ViewFields(AccessRequestCamlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());

            // CAML Queries for all Approved requests that are within 30 days
            var camlWhereClauseNeedToRecertify = CAML.And(
                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, "Text", EzForms_RequestType_Constants.AD_Privileged_Account)),
                CAML.And(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Routing_x0020_Phase, "Text", "Approved")),
                    CAML.And(
                        // UTC+30 Days <= Recertification
                        CAML.Geq(string.Format("{0}<Value Type='DateTime' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{1}</Value>",
                            CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), UtcIsoDate.ToString(base.ZeroHourFormatString))),
                        // Today >= Recertification
                        CAML.Leq(string.Format("{0}<Value Type='DateTime' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{1}</Value>",
                            CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), utcIso30DaysFromToday.ToString(base.ZeroHourFormatString)))
                        )
                    )
                );



            // get ezforms site and query the list for approved requests
            ListItemCollectionPosition ListItemCollectionPosition = null;
            var camlQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, CAML.Where(camlWhereClauseNeedToRecertify), "", camlViewClause, 100)
            };

            LogDebugging(string.Format("Query: {0}", camlQuery.ViewXml));
            var accessRequestList = this.ClientContext.Web.Lists.GetByTitle(EzForms_AccessRequest.ListName);
            this.ClientContext.Load(accessRequestList);
            this.ClientContext.ExecuteQueryRetry();

            var output = new List<int>();
            while (true)
            {
                camlQuery.ListItemCollectionPosition = ListItemCollectionPosition;
                var spListItems = accessRequestList.GetItems(camlQuery);
                this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                this.ClientContext.ExecuteQueryRetry();
                ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    var requestId = requestItem.Id;
                    output.Add(requestId);
                }

                if (ListItemCollectionPosition == null)
                {
                    break;
                }
            }

            foreach (var spItemId in output)
            {
                try
                {
                    var requestItem = accessRequestList.GetItemById(spItemId);
                    this.ClientContext.Load(requestItem);
                    this.ClientContext.ExecuteQueryRetry();

                    var employee = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Employee);
                    var firstLevelApprover = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Division_x0020_Director_x0020_or);
                    var dottedAccount = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezFormsADAccount);
                    var recertificationDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezformsNextCertifyDate);
                    var previousRouting = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Routing_x0020_Phase);

                    LogVerbose("Effecting recertification on {0}", spItemId);

                    requestItem[EzForms_AccessRequest.Field_Request_x0020_Status] = "Recertification";
                    requestItem[EzForms_AccessRequest.Field_Routing_x0020_Phase] = "Recertification";
                    requestItem[EzForms_AccessRequest.Field_Previous_x0020_Routing_x0020_Pha] = previousRouting;

                    if (this.ShouldProcess(string.Format("Updating request {0} with recertification status {1}", spItemId, "Recertification")))
                    {
                        LogWarning("ListItem [{0}] User:{1} Recertification:{2} will be updated.", spItemId, dottedAccount, recertificationDate);
                        requestItem.SystemUpdate();
                        this.ClientContext.ExecuteQueryRetry();
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Migrate failed for list items MSG:{0}", ex.Message);
                }
            }



            // status check, get for reporting or checking against AD
            var camlWhereClauseNeedToNotify = CAML.And(
                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, "Text", EzForms_RequestType_Constants.AD_Privileged_Account)),
                CAML.And(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, "Text", "Recertification")),
                    CAML.And(
                        CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Routing_x0020_Phase, "Text", "Recertification")),  // indicates a non approval/denial
                        CAML.And(
                            CAML.Geq(string.Format("{0}<Value Type='{1}' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{2}</Value>",
                                CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), "DateTime", UtcIsoDate.ToString(base.ZeroHourFormatString))),
                            CAML.Leq(string.Format("{0}<Value Type='{1}' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{2}</Value>",
                                CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), "DateTime", utcIso30DaysFromToday.ToString(base.ZeroHourFormatString)))
                            )
                        )
                    )
                );


            // get ezforms site and query the list for approved requests
            ListItemCollectionPosition = null;
            camlQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, CAML.Where(camlWhereClauseNeedToNotify), "", camlViewClause, 100)
            };
            var emailSentTo = new List<string>();

            while (true)
            {
                try
                {
                    camlQuery.ListItemCollectionPosition = ListItemCollectionPosition;
                    var spListItems = accessRequestList.GetItems(camlQuery);
                    this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                    this.ClientContext.ExecuteQuery();
                    ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                    foreach (var requestItem in spListItems)
                    {
                        emailSentTo = new List<string>(); // reset the recepient email array
                        var emailHeader = string.Empty;
                        var emailSubject = string.Empty;
                        var emailMessageSentFlag = false; // indicates an email has been sent
                        var notificationType = NotificationType.NONE;
                        var employeeModel = new EzFormsUserProfileModel();
                        var requestId = requestItem.Id;
                        var certifyDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezformsNextCertifyDate);
                        var employee = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Employee);
                        var isemployee = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezPersonnelBool).ToUpper();
                        var firstLevelApprover = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Division_x0020_Director_x0020_or);

                        employeeModel.Email = employee.Email;
                        employeeModel.ID = employee.LookupId;
                        employeeModel.DisplayName = employee.LookupValue;
                        employeeModel.Employee = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezPersonnelBool).ToBoolean(true);
                        employeeModel.Office = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Office);
                        employeeModel.SamAccountName = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezLanIdText);
                        employeeModel.DottedAccountName = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezFormsADAccount);
                        employeeModel.WorkforceID = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezformsWorkforceID);

                        var userFirstLevelApproverDisplayName = string.Empty;
                        var userFirstLevelApprover = (isemployee.Equals("NO", StringComparison.CurrentCultureIgnoreCase) ? "Supervisor" : "Contracting Office Representative");

                        emailSentTo.Add(employeeModel.Email);
                        if (firstLevelApprover != null)
                        {
                            emailSentTo.Add(firstLevelApprover.Email);
                            userFirstLevelApproverDisplayName = firstLevelApprover.LookupValue;
                        }

                        DateTime certifyDateTime = DateTime.UtcNow;
                        DateTime.TryParse(certifyDate, out certifyDateTime);

                        // perform some lookup parsing to see if we should send an email
                        var requestNotification = NotificationJson.Notifications.FirstOrDefault(ab => ab.SPId == requestId);

                        // calculate the difference between the next recertification date and the current UTC date
                        var spanDifference = certifyDateTime.Date.Subtract(UtcIsoDate.Date);
                        if (spanDifference.TotalHours >= 0 && spanDifference < OneDaySpan)
                        {
                            notificationType = NotificationType.OneDay;
                            emailSubject = "AD Privileged Access: Recertification final day notice.";
                            emailHeader = "AD Privileged Account reminder.  This is the last day to recertify your account.  Your account will be disabled tomorrow.";

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
                            notificationType = NotificationType.SevenDays;
                            emailSubject = "AD Privileged Access: Recertification 7 day notice.";
                            emailHeader = "AD Privileged Account reminder email for 7 day(s).";

                            // notification has occurred for a 7 day notice
                            emailMessageSentFlag = (requestNotification != null && requestNotification.schedule.Any(sch => sch.Notification == notificationType));
                            if (emailMessageSentFlag)
                            {
                                // last run instance for this 7 day notification [if this has been recertified before, check if notification has occurred in the new schedule]
                                var lastNotification = requestNotification.schedule.Where(sch => sch.Notification == notificationType).OrderByDescending(ob => ob.NotificationDateTime).FirstOrDefault();
                                if (lastNotification != null && UtcIsoDate.Subtract(lastNotification.NotificationDateTime) > SevenDaysSpan)
                                {
                                    emailMessageSentFlag = false;
                                }
                            }
                        }
                        else if (spanDifference.TotalHours > 0 && spanDifference < ThirtyDaysSpan)
                        {
                            notificationType = NotificationType.ThirtyDays;
                            emailSubject = "AD Privileged Access: Recertification 30 day notice.";
                            emailHeader = "AD Privileged Account reminder email for 30 day(s).";

                            // notification has occurred for a 30 day notice
                            emailMessageSentFlag = (requestNotification != null && requestNotification.schedule.Any(sch => sch.Notification == notificationType));
                            if (emailMessageSentFlag)
                            {
                                // last run instance for this 30 day notification [if this has been recertified before, check if notification has occurred in the new schedule]
                                var lastNotification = requestNotification.schedule.Where(sch => sch.Notification == notificationType).OrderByDescending(ob => ob.NotificationDateTime).FirstOrDefault();
                                if (lastNotification != null && UtcIsoDate.Subtract(lastNotification.NotificationDateTime) > ThirtyDaysSpan)
                                {
                                    emailMessageSentFlag = false;
                                }
                            }
                        }

                        // Log the request variables for debugging purposes
                        LogVerbose("Checking {0} UTC:{1} Recertification Date:{2} Notification:{3} Hour Span:{4} with message flag {5}", employeeModel.DisplayName, UtcIsoDate, certifyDateTime, notificationType, spanDifference.TotalHours, emailMessageSentFlag);

                        if (notificationType != NotificationType.NONE
                         && !emailMessageSentFlag
                         && this.ShouldProcess(string.Format("Emailing user {0} regarding their recertification status {1}", employeeModel.DisplayName, emailSubject)))
                        {
                            var emailMessage = new StringBuilder();
                            emailMessage.Append("<div><br></div>");
                            emailMessage.AppendFormat("<div>Hello {0},</div>", employeeModel.DisplayName);
                            emailMessage.AppendFormat("<div>{0}</div>", emailHeader);
                            if (!string.IsNullOrEmpty(userFirstLevelApproverDisplayName))
                            {
                                emailMessage.Append("<div>");
                                emailMessage.AppendFormat("You previously identified your {1} as {0}.  They have been CC'd with this email.", userFirstLevelApproverDisplayName, userFirstLevelApprover);
                                emailMessage.Append("During the recertification process you can update this individual.");
                                emailMessage.Append("</div>");
                            }
                            emailMessage.AppendFormat("<div><a href=\"{0}\" title=\"click here to open the form\">Click here to start the Recertification process</a>", string.Format(FormatUrl, ClientContext.Url, requestId, ""));
                            emailMessage.AppendFormat("<div><a href=\"{0}\" title=\"click here to open the 508 compliance form\">Recertification process 508 alternate view</a>", string.Format(FormatUrl, ClientContext.Url, requestId, "_508"));
                            emailMessage.Append("<div><br></div>");
                            emailMessage.Append("<div>Thank you,</div>");
                            emailMessage.Append("<div>EZ Forms Support</div>");

                            EmailProperties properties = new EmailProperties
                            {
                                To = emailSentTo,
                                Subject = emailSubject,
                                Body = string.Format("<div>{0}</div>", emailMessage),
                                BCC = EmailBCC
                            };

                            Microsoft.SharePoint.Client.Utilities.Utility.SendEmail(this.ClientContext, properties);
                            this.ClientContext.ExecuteQueryRetry();

                            var processedAccount = new EzFormsRequestEmailModel()
                            {
                                ID = requestId,
                                employee = employeeModel,
                                request = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Type),
                                status = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Status),
                                routing = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Routing_x0020_Phase),
                                recertificationDate = certifyDate.ToNullableDatetime(),
                                computerName = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Computer_x0020_Name),
                                requestedOn = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Date).ToNullableDatetime(),
                                subject = emailSubject,
                                message = emailMessage.ToString(),
                                emailType = notificationType,
                                messageSent = UtcIsoDate
                            };

                            // Add the notification to the log entry
                            if (NotificationJson.Notifications.Any(s => s.SPId == processedAccount.ID))
                            {
                                var usr = NotificationJson.Notifications.FirstOrDefault(s => s.SPId == processedAccount.ID);
                                usr.schedule.Add(new EzFormsNotifySchedule()
                                {
                                    Notification = processedAccount.emailType,
                                    NotificationDateTime = processedAccount.messageSent
                                });
                            }
                            else
                            {
                                var entity = new EzFormsNotification()
                                {
                                    SPId = processedAccount.ID,
                                    email = processedAccount.employee.Email
                                };
                                entity.schedule.Add(new EzFormsNotifySchedule()
                                {
                                    Notification = processedAccount.emailType,
                                    NotificationDateTime = processedAccount.messageSent
                                });
                                NotificationJson.Notifications.Add(entity);
                            }

                            LogVerbose("User {0} was sent an email with subject {1}", employeeModel.Email, emailSubject);
                        }
                    }

                    if (ListItemCollectionPosition == null)
                    {
                        break;
                    }


                }
                catch (Exception ex)
                {
                    LogError(ex, "Migrate failed for list items MSG:{0}", ex.Message);
                }
            }

            // Save when it occurred
            NotificationJson.RunOccurrences.Add(UtcIsoDate);
        }

        /// <summary>
        /// should pull all Denied requests or all requests which were in a recertification stage that have past the certification date
        /// use this to ensure there are rows in the User Recertification table
        /// </summary>
        private void SetUsersWhichShouldBeDenied()
        {

            var camlDeniedOrPastPeriod = CAML.And(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, FieldType.Text.ToString("f"), EzForms_RequestType_Constants.AD_Privileged_Account)),
                    CAML.And(
                        CAML.IsNotNull(CAML.FieldRef(EzForms_AccessRequest.Field_ezFormsADAccount)),
                        CAML.Or(
                            CAML.And(
                                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), "Recertification")),
                                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Routing_x0020_Phase, FieldType.Text.ToString("f"), "Denied"))),
                            CAML.And(
                                CAML.Leq(string.Format("{0}<Value Type='{1}' IncludeTimeValue='TRUE' StorageTZ='TRUE'>{2}</Value>",
                                    CAML.FieldRef(EzForms_AccessRequest.Field_ezformsNextCertifyDate), FieldType.DateTime.ToString("f"), UtcIsoDate.ToString(base.ZeroHourFormatString))),
                                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Routing_x0020_Phase, FieldType.Text.ToString("f"), "Recertification")))
                                )
                            )
                    );


            // get ezforms site and query the list for approved requests
            var accessRequestCamlViewClause = CAML.ViewFields(AccessRequestCamlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());

            ListItemCollectionPosition ListItemCollectionPosition = null;
            var camlQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(
                    ViewScope.RecursiveAll,
                    CAML.Where(camlDeniedOrPastPeriod), "",
                    CAML.ViewFields(
                        CAML.FieldRef("ID"),
                        CAML.FieldRef(EzForms_AccessRequest.Field_Employee)),
                    100)
            };

            // Get Request Forms
            var accessRequestList = this.ClientContext.Web.Lists.GetByTitle(EzForms_AccessRequest.ListName);
            this.ClientContext.Load(accessRequestList);
            this.ClientContext.ExecuteQueryRetry();

            // Recertification List for determining Rows [NOT PROCESSED]
            var recertificationList = this.ClientContext.Web.Lists.GetByTitle(EzForms_UserRecertification.ListName);
            this.ClientContext.Load(recertificationList);
            this.ClientContext.ExecuteQueryRetry();

            var rowsWhichShouldBeDisabledInActiveDirectory = new List<EzFormsNonProcessedRows>();

            while (true)
            {
                try
                {
                    camlQuery.ListItemCollectionPosition = ListItemCollectionPosition;
                    var spListItems = accessRequestList.GetItems(camlQuery);
                    this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                    this.ClientContext.ExecuteQueryRetry();
                    ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                    foreach (var requestItem in spListItems)
                    {
                        var requestId = requestItem.Id;
                        var employee = requestItem.RetrieveListItemUserValue("Employee");


                        // query user recertification to evaluate if a row exists for the Denied or recertification that has past without action
                        // get ezforms site and query the list for approved requests
                        ListItemCollectionPosition RecertListItemCollectionPosition = null;
                        var camlRecertQuery = new CamlQuery
                        {
                            ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll,
                            CAML.Where(
                                CAML.And(
                                    CAML.Eq(CAML.FieldValue(EzForms_UserRecertification.Field_ezItemID, FieldType.Integer.ToString("f"), requestId.ToString())),
                                    CAML.Eq(CAML.FieldValue(EzForms_UserRecertification.Field_RowProcessed, FieldType.Boolean.ToString("f"), 0.ToString())))
                            ),
                            "",
                            CAML.ViewFields(CAML.FieldRef("ID")),
                            100)
                        };

                        while (true)
                        {
                            camlRecertQuery.ListItemCollectionPosition = RecertListItemCollectionPosition;
                            var recertListItems = recertificationList.GetItems(camlRecertQuery);
                            this.ClientContext.Load(recertListItems, lti => lti.ListItemCollectionPosition);
                            this.ClientContext.ExecuteQueryRetry();
                            RecertListItemCollectionPosition = recertListItems.ListItemCollectionPosition;

                            if (recertListItems.Count <= 0)
                            {
                                rowsWhichShouldBeDisabledInActiveDirectory.Add(new EzFormsNonProcessedRows()
                                {
                                    requestId = requestItem.Id,
                                    employeeId = employee.LookupId
                                });
                            }

                            if (RecertListItemCollectionPosition == null)
                            {
                                break;
                            }
                        }
                    }

                    if (ListItemCollectionPosition == null)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Migrate failed for list items MSG:{0}", ex.Message);
                }
            }

            foreach (var request in rowsWhichShouldBeDisabledInActiveDirectory)
            {
                // create item for notifications
                var recertificationItem = new ListItemCreationInformation();
                var recertificationListItem = recertificationList.AddItem(recertificationItem);
                recertificationListItem[EzForms_UserRecertification.Field_Title] = "PSB Recertification";
                recertificationListItem[EzForms_UserRecertification.Field_ezItemID] = request.requestId;
                recertificationListItem[EzForms_UserRecertification.Field_ezEmployeeUser] = new Microsoft.SharePoint.Client.FieldUserValue() { LookupId = request.employeeId };
                recertificationListItem[EzForms_UserRecertification.Field_ezRecertificationDate] = UtcIsoDate;
                recertificationListItem[EzForms_UserRecertification.Field_Routing_x0020_Phase] = "Denied";
                recertificationListItem[EzForms_UserRecertification.Field_RowProcessed] = 0;
                recertificationListItem.Update();

                // push the row into SP
                this.ClientContext.ExecuteQueryRetry();
            }
        }



    }
}