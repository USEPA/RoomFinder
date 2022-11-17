using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Apps;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using Newtonsoft.Json;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    /// <summary>
    /// The function of the EPA’s Ez Forms Notification script is to ensure reminders are being sent to all individuals who are required to progress a workflow to the next step on a users behalf
    /// Description: Send notifications and archive idle requests 
    /// 
    /// 1. Enumerate all "Pending" requests
    /// 2. If request has been idle for more then 7 days and less then 14 days --> send an email remider to the current approver 
    /// 3. If reuqest has been idle for more then 14 days, archive the request and norify the requestor 
    /// </summary>
    [Verb("sendEPAEZFormsNotifications", HelpText = "will notifications to approvers and requestors.")]
    public class SendEPAEZFormsNotificationsOptions : CommonOptions
    {
        /// <summary>
        /// Includes the destination directory for the files
        /// </summary>
        [Option("log-directory", Required = true, HelpText = "Includes the destination directory for the files.")]
        public string LogDirectory { get; set; }

        /// <summary>
        /// Represents the total days before we archive a request; if sent by parameter override the default
        /// </summary>
        [Option("archive-afterdays", Required = false)]
        public int ArchiveAfterDays { get; set; } = 14;

        /// <summary>
        /// Represents the start with ID to filter results based on ID specified
        /// </summary>
        [Option("startswithid", Required = false)]
        public int StartsWithId { get; set; } = 0;
    }

    public static class SendEPAEZFormsNotificationsOptionsExtension
    {
        /// <summary>
        /// Will query ezform requests, those ready for renewal and send reminder emails
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this SendEPAEZFormsNotificationsOptions opts, IAppSettings appSettings)
        {
            var cmd = new SendEPAEZFormsNotifications(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CSOM unhandled exceptions")]
    public class SendEPAEZFormsNotifications : BaseEZFormsRecertification<SendEPAEZFormsNotificationsOptions>
    {
        public SendEPAEZFormsNotifications(SendEPAEZFormsNotificationsOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        /// <summary>
        /// Holds a collection of requests that require approval
        /// </summary>
        internal List<EZFormsApprovers> ApproverItems { get; set; }

        #endregion

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();

            if (!System.IO.Directory.Exists(Opts.LogDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"{Opts.LogDirectory} could not be found.");
            }
        }

        public override int OnRun()
        {
            ApproverItems = new List<EZFormsApprovers>();


            var EndDate = DateTime.Now;
            var NotificationSiteUrl = TokenHelper.EnsureTrailingSlash(this.ClientContext.Url);

            //Load access requests list
            var accessList = this.ClientContext.Web.Lists.GetByTitle(EzForms_AccessRequest.ListName);
            this.ClientContext.Load(accessList);
            this.ClientContext.ExecuteQuery();

            var camlFields = new string[] {
                EzForms_AccessRequest.Field_Request_x0020_Status,
                EzForms_AccessRequest.Field_Request_x0020_Type,
                EzForms_AccessRequest.Field_Routing_x0020_Phase,
                EzForms_AccessRequest.Field_Employee,
                EzForms_AccessRequest.Field_Division_x0020_Director_x0020_or,
                EzForms_AccessRequest.Field_ORD_x0020_IT_x0020_Operations_x0,
                EzForms_AccessRequest.Field_Local_x0020_Information_x0020_Se,
                EzForms_AccessRequest.Field_Office_x0020_Information_x0020_S,
                EzForms_AccessRequest.Field_Information_x0020_Management_x00,
                EzForms_AccessRequest.Field_Senior_x0020_Information_x0020_S,
                EzForms_AccessRequest.Field_Business_x0020_Relationship_x002,
                EzForms_AccessRequest.Field_International_x0020_Travel_x0020,
                ConstantsFields.Field_Modified,
                ConstantsFields.Field_ID
            };
            var camlFieldRefs = camlFields.Select(s => CAML.FieldRef(s)).ToArray();
            var camlViewXml = CAML.ViewFields(camlFieldRefs);

            var camlWhere = CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), "Pending"));
            if (Opts.StartsWithId > 0)
            {
                camlWhere = CAML.And(
                        camlWhere,
                        CAML.Gt(CAML.FieldValue("ID", "Number", Opts.StartsWithId.ToString())));
            }


            // get ezforms site and query the list for pending requests
            ListItemCollectionPosition ListItemCollectionPosition = null;
            var camlQuery = CamlQuery.CreateAllItemsQuery();
            camlQuery.ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, CAML.Where(camlWhere), string.Empty, camlViewXml, 50);

            while (true)
            {
                camlQuery.ListItemCollectionPosition = ListItemCollectionPosition;
                var spListItems = accessList.GetItems(camlQuery);
                var ListItemIdx = 0;
                this.ClientContext.Load(spListItems);
                this.ClientContext.ExecuteQuery();
                ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    var StartDate = (DateTime)requestItem[EzForms_AccessRequest.Field_Modified];
                    var timeSpan = EndDate.Subtract(StartDate);

                    if ((timeSpan.Days > 7) && (timeSpan.Days <= Opts.ArchiveAfterDays))
                    {
                        // first notice
                        //Send first notice email to current approver
                        AddNotificationListItem(requestItem, NotificationTypeEnum.Idle_Approver_Alert, timeSpan);
                        ListItemIdx++;
                    }
                    else if (timeSpan.Days > Opts.ArchiveAfterDays)
                    {
                        // last notice
                        //Send notice to requestor indicating the request has been archived and they need to create a new one
                        AddNotificationListItem(requestItem, NotificationTypeEnum.Archive_Alert, timeSpan);
                        ListItemIdx++;
                    }
                    else
                    {
                        LogVerbose("..... notification not required as it does not meet the timespan defaults");
                    }
                }

                if (ListItemCollectionPosition == null)
                {
                    break;
                }

            }

            // this collection is populated by enumerating the list and compiling outdated requests
            ApproverItems.ForEach(missingApproval =>
            {
                var emailSubject = "EzForms Outstanding: Request for approval";
                var emailBody = string.Format("There are EPA EZForm request(s) awaiting your approval. <div>You can view requests at <a href='{0}Lists/Access%20Requests/MyApprovalsAll.aspx'>My Approvals</a></div>", NotificationSiteUrl);
                emailBody += "<div><br></div>";
                emailBody += string.Format("<div>The following {0} requests are awaiting your approval:</div>", missingApproval.TotalRequests);
                missingApproval.Requests.OrderByDescending(ob => ob.TotalDays).ToList().ForEach(request =>
                {
                    emailBody += string.Format("<div><hr /></div><div>Request from {1} on {2} {3} days ago.<div>Please click <a href=\"{0}{4}\">Edit Request</a> to Approve/Deny the request.</div></div>", NotificationSiteUrl, request.EmailAddress, request.DateOfLastModification, request.TotalDays, request.EmailEditUrl);
                });
                emailBody += "<div><br></div>";
                emailBody += "<div>Thank you,</div>";
                emailBody += "<div>EZ Forms Support</div>";

                if (this.ShouldProcess(
                        string.Format("Will send an email to {0} with {1} requests.", missingApproval.EmailAddress, missingApproval.TotalRequests)))
                {
                    var properties = new EmailProperties()
                    {
                        To = new string[] { missingApproval.EmailAddress },
                        Subject = emailSubject,
                        Body = string.Format("<div>{0}</div>", emailBody),
                        BCC = EmailBCC
                    };
                    Microsoft.SharePoint.Client.Utilities.Utility.SendEmail(this.ClientContext, properties);
                    this.ClientContext.ExecuteQueryRetry();
                }
            });

            LogVerbose("Writing {0} objects to memory", ApproverItems.Count);
            if (ShouldProcess("Writing file to disc"))
            {
                var jsonPath = $"{Opts.LogDirectory}\\schedule-EPAEZFormsNotifications-{DateTime.UtcNow:yyyy-MM-dd_HH_mm_ss}.json";
                var approverItemsJson = JsonConvert.SerializeObject(ApproverItems, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 5,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                System.IO.File.WriteAllText(jsonPath, approverItemsJson);
            }
            return 1;
        }


        /// <summary>
        /// Add notification to the queue list and start the workflow
        /// </summary>
        /// <param name="requestItem">The request which requires a reminder notification</param>
        /// <param name="noticeOperation">The request type as depicted by the <seealso cref="timeDifference"/> span</param>
        /// <param name="timeDifference">The span of time between the request and this query</param>
        private void AddNotificationListItem(ListItem requestItem, NotificationTypeEnum noticeOperation, TimeSpan timeDifference)
        {
            var currentRequestId = requestItem.Id;
            var currentRequestStatus = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Status);
            var currentRequestPhase = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Routing_x0020_Phase);
            var currentRequestType = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Type);
            var currentModifiedDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Modified);
            var currentNotificationType = GetNotificationTypePlain(noticeOperation);

            FieldUserValue notificationRecepient = null;
            FieldUserValue notificationApprover = RetrieveListItemApprover(requestItem, currentRequestPhase);
            FieldUserValue notificationSubmitter = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Employee);
            if (notificationSubmitter == null)
            {
                LogWarning("Invalid user and object for request ID {0}", currentRequestId);
                return;
            }

            var SubmitterEmailAddress = notificationSubmitter.Email;

            var currentRequest = new EZFormsArchive()
            {
                EmailAddress = SubmitterEmailAddress,
                Id = currentRequestId,
                TotalDays = timeDifference.Days,
                DateOfLastModification = currentModifiedDate,
                TypeOfEmailNotification = currentNotificationType
            };

            try
            {

                if (currentRequestPhase == "None")
                {
                    LogWarning(".... Recepient [Notification:'{5}'] has not been submitted [ItemId:{4}] for [Request Status:{0}] [Routing Phase:{1}] [Request Type:{2}] [Submitter:{3}] [Modified:{6}] [TimeSpan.Days:{7}]",
                        currentRequestStatus, currentRequestPhase, currentRequestType, SubmitterEmailAddress, currentRequestId, currentNotificationType, currentModifiedDate, timeDifference.Days);
                    return;
                }

                if (noticeOperation == NotificationTypeEnum.Archive_Alert)
                {
                    notificationRecepient = notificationSubmitter;
                }
                else if (noticeOperation == NotificationTypeEnum.Idle_Approver_Alert)
                {
                    notificationRecepient = notificationApprover;
                }

                if (notificationRecepient == null)
                {
                    LogWarning(".... Recepient [Notification:'{5}'] is not clearly defined in [ItemId:{4}] for [Request Status:{0}] [Routing Phase:{1}] [Request Type:{2}] [Submitter:{3}] [Modified:{6}] [TimeSpan.Days:{7}]",
                        currentRequestStatus, currentRequestPhase, currentRequestType, SubmitterEmailAddress, currentRequestId, currentNotificationType, currentModifiedDate, timeDifference.Days);
                    return;
                }
                else if (notificationApprover == null)
                {
                    LogWarning(".... Recepient [Notification:'{5}'] is not clearly defined in [ItemId:{4}] for [Request Status:{0}] [Routing Phase:{1}] [Request Type:{2}] [Submitter:{3}] [Modified:{6}] [TimeSpan.Days:{7}]",
                        currentRequestStatus, currentRequestPhase, currentRequestType, SubmitterEmailAddress, currentRequestId, currentNotificationType, currentModifiedDate, timeDifference.Days);
                    return;
                }

                LogVerbose(".... Creating [Notification:'{5}'] for [ID:{4}] to be sent to: [Request Status:{0}] [Routing Phase:{1}] [Request Type:{2}] [Recepient:{3}] [Modified:{6}] [TimeSpan.Days:{7}]",
                    currentRequestStatus, currentRequestPhase, currentRequestType, notificationRecepient.Email, currentRequestId, currentNotificationType, currentModifiedDate, timeDifference.Days);

                var ApproverEmailAddress = notificationApprover.Email;

                if (this.ApproverItems.Count(ai => ai.EmailAddress == ApproverEmailAddress) > 0)
                {
                    var approver = this.ApproverItems.FirstOrDefault(fd => fd.EmailAddress == ApproverEmailAddress);
                    approver.Requests.Add(currentRequest);
                }
                else
                {
                    this.ApproverItems.Add(new EZFormsApprovers()
                    {
                        EmailAddress = ApproverEmailAddress,
                        Requests = new List<EZFormsArchive>() { currentRequest }
                    });
                }


            }
            catch (Exception ex)
            {
                LogError(ex, "SetEzFormsNotification(ID:{0} MSG:{1})", requestItem.Id, ex.Message);
            }
        }

        private FieldUserValue RetrieveListItemApprover(ListItem requestItem, string currentRequestPhase)
        {
            FieldUserValue notificationRecepient = null;

            if (currentRequestPhase == "Supervisor/Director")
            {
                notificationRecepient = requestItem.RetrieveListItemUserValue("Division_x0020_Director_x0020_or");
            }
            else if (currentRequestPhase == "ORD IT Operations Manager")
            {
                notificationRecepient = requestItem.RetrieveListItemUserValue("ORD_x0020_IT_x0020_Operations_x0");
            }
            else if (currentRequestPhase == "Local Information Security Officer")
            {
                notificationRecepient = requestItem.RetrieveListItemUserValue("Local_x0020_Information_x0020_Se");
            }
            else if (currentRequestPhase == "Information Security Officer")
            {
                notificationRecepient = requestItem.RetrieveListItemUserValue("Office_x0020_Information_x0020_S");
            }
            else if (currentRequestPhase == "Information Management Officer")
            {
                notificationRecepient = requestItem.RetrieveListItemUserValue("Information_x0020_Management_x00");
            }
            else if (currentRequestPhase == "Senior Information Officer")
            {
                notificationRecepient = requestItem.RetrieveListItemUserValue("Senior_x0020_Information_x0020_S");
            }
            else if (currentRequestPhase == "Business Relationship Manager")
            {
                notificationRecepient = requestItem.RetrieveListItemUserValue("Business_x0020_Relationship_x002");
            }
            else if (currentRequestPhase == "International Travel Representative")
            {
                notificationRecepient = requestItem.RetrieveListItemUserValue("International_x0020_Travel_x0020");
            }

            return notificationRecepient;
        }

        /// <summary>
        /// Parse enum and return friendly display text
        /// </summary>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        internal string GetNotificationTypePlain(NotificationTypeEnum notificationType)
        {
            return notificationType.ToString().Replace(@"_", " ");
        }
    }
}