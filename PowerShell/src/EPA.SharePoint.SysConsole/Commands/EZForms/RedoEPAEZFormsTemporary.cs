using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.Models.Apps;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("redoEPAEZFormsTemporary", HelpText = "will move a ezform forms to a temp directory.")]
    public class RedoEPAEZFormsTemporaryOptions : CommonOptions
    {
    }

    public static class RedoEPAEZFormsTemporaryOptionsExtension
    {
        /// <summary>
        /// Will execute the scan for OneDrive changes
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this RedoEPAEZFormsTemporaryOptions opts, IAppSettings appSettings)
        {
            var cmd = new RedoEPAEZFormsTemporary(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Temporary EZForm Activities
    /// </summary>
    /// <remarks>
    /// Examples including extracting data into separate columns, mass data operations, etc
    /// </remarks>
    public class RedoEPAEZFormsTemporary : BaseEZFormsRecertification<RedoEPAEZFormsTemporaryOptions>
    {
        public RedoEPAEZFormsTemporary(RedoEPAEZFormsTemporaryOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override int OnRun()
        {
            ProcessUserRecertificationsDenied();
            return 1;
        }

        private string PrivADInfoViewCaml()
        {
            var camlFieldRefs = new string[]
            {
                EzForms_PrivADInfo.Field_Title,
                EzForms_PrivADInfo.Field_EmployeeID,
                EzForms_PrivADInfo.Field_EmployeeNumber,
                EzForms_PrivADInfo.Field_SamAccountName,
                EzForms_PrivADInfo.Field_GivenName,
                EzForms_PrivADInfo.Field_SurName,
                EzForms_PrivADInfo.Field_DisplayName,
                EzForms_PrivADInfo.Field_ADObjectID,
                EzForms_PrivADInfo.Field_EmailAddress,
                EzForms_PrivADInfo.FieldDate_LastSyncDate,
                EzForms_PrivADInfo.FieldDate_DottedAccountExpirationDate,
                EzForms_PrivADInfo.Field_DottedAccountName,
                EzForms_PrivADInfo.Field_EmployeeManager,
                EzForms_PrivADInfo.Field_DottedAccountSID,
                EzForms_PrivADInfo.Field_DottedProvisioningMessage,
                EzForms_PrivADInfo.Field_DottedRequestID
            };
            var camlViewClause = CAML.ViewFields(camlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());

            return camlViewClause;
        }

        /// <summary>
        /// Check the formatting of the PrivADInfo Processing messaging JSON
        /// </summary>
        private void QueryPrivADInfo()
        {
            var camlViewClause = PrivADInfoViewCaml();
            // get ezforms site and query the list for approved requests
            var listADInfo = this.ClientContext.Web.GetListByTitle(EzForms_PrivADInfo.ListName, fn => fn.Title, fnx => fnx.ItemCount, fnx => fnx.LastItemUserModifiedDate);
            var camlQueries = listADInfo.SafeCamlClauseFromThreshold(1000, CAML.IsNotNull(CAML.FieldRef(EzForms_PrivADInfo.Field_DottedProvisioningMessage)));
            foreach (var camlQuery in camlQueries)
            {
                var caml = new CamlQuery
                {
                    ListItemCollectionPosition = null,
                    ViewXml = CAML.ViewQuery(ViewScope.DefaultValue,
                        CAML.Where(camlQuery),
                        string.Empty,
                        camlViewClause,
                        100)
                };

                do
                {
                    var spListItems = listADInfo.GetItems(caml);
                    this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                    this.ClientContext.ExecuteQueryRetry();
                    caml.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                    foreach (var spItem in spListItems)
                    {
                        var localAccount = spItem.RetrieveListItemValue(EzForms_PrivADInfo.Field_SamAccountName);
                        try
                        {
                            var provisioningMessage = spItem.RetrieveListItemValue(EzForms_PrivADInfo.Field_DottedProvisioningMessage);
                            var provisioningList = new List<AccountProcessingActivityModel>();
                            if (!string.IsNullOrEmpty(provisioningMessage))
                            {
                                var serializedMessages = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AccountProcessingActivityModel>>(provisioningMessage);
                                if (serializedMessages != null && serializedMessages.Any())
                                {
                                    provisioningList.AddRange(serializedMessages.OrderByDescending(s => s.EventDate));
                                }
                            }
                            var serializedMessage = Newtonsoft.Json.JsonConvert.SerializeObject(provisioningList, Newtonsoft.Json.Formatting.None);

                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Error occurred on record {spItem.Id} for user {localAccount}");
                            LogError(ex, $"Failed with msg {ex.Message} and stack trace {ex?.StackTrace}");
                        }
                    }
                }
                while (caml.ListItemCollectionPosition != null);
            }
        }

        /// <summary>
        /// Will update the formatting of the User Access List column
        /// </summary>
        private void UpdateUserAccessList()
        {
            var accessList = this.ClientContext.Web.GetListByTitle(EzForms_AccessRequest.ListName, fn => fn.Title, fnx => fnx.ItemCount, fnx => fnx.LastItemUserModifiedDate);

        }

        /// <summary>
        /// Process user recertifications that are stale and denied
        /// </summary>
        private void ProcessUserRecertificationsDenied()
        {
            var accessList = this.ClientContext.Web.GetListByTitle(EzForms_AccessRequest.ListName, fn => fn.Title, fnx => fnx.ItemCount, fnx => fnx.LastItemUserModifiedDate);

            // Recertification List for determining Rows [NOT PROCESSED]
            var recertificationList = this.ClientContext.Web.GetListByTitle(EzForms_UserRecertification.ListName, fnx => fnx.Title, fnx => fnx.ItemCount, fnx => fnx.LastItemUserModifiedDate);


            var camlViewClause = PrivADInfoViewCaml();
            var listADInfo = this.ClientContext.Web.GetListByTitle(EzForms_PrivADInfo.ListName, fn => fn.Title, fnx => fnx.ItemCount, fnx => fnx.LastItemUserModifiedDate);


            // query user recertification to evaluate if a row exists for the Denied or recertification that has past without action
            // get ezforms site and query the list for approved requests
            var camlViewName = new string[]
            {
                EzForms_UserRecertification.Field_ID,
                EzForms_UserRecertification.Field_ezItemID,
                EzForms_UserRecertification.Field_ezEmployeeUser,
                EzForms_UserRecertification.Field_Routing_x0020_Phase,
                EzForms_UserRecertification.Field_ezRecertificationDate,
                EzForms_UserRecertification.Field_Created
            };
            var camlViewFields = camlViewName.Select(cvn => CAML.FieldRef(cvn)).ToArray();

            var camlIdx = 0;
            var recertQueries = recertificationList.SafeCamlClauseFromThreshold(1000, CAML.And(
                     CAML.Eq(CAML.FieldValue(EzForms_UserRecertification.Field_RowProcessed, FieldType.Boolean.ToString("f"), 0.ToString())),
                     CAML.Eq(CAML.FieldValue(EzForms_UserRecertification.Field_Routing_x0020_Phase, FieldType.Text.ToString("f"), "Denied"))));
            foreach (var camlRecert in recertQueries)
            {
                LogVerbose($"Querying idx {camlIdx++} {camlRecert}");

                var camlRecertQuery = new CamlQuery
                {
                    ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll,
                    CAML.Where(camlRecert),
                    "",
                    CAML.ViewFields(camlViewFields),
                    100),
                    ListItemCollectionPosition = null
                };

                do
                {
                    var recertListItems = recertificationList.GetItems(camlRecertQuery);
                    this.ClientContext.Load(recertListItems, lti => lti.ListItemCollectionPosition);
                    this.ClientContext.ExecuteQueryRetry();
                    camlRecertQuery.ListItemCollectionPosition = recertListItems.ListItemCollectionPosition;

                    foreach (var recertificationListItem in recertListItems)
                    {
                        try
                        {

                            var requestId = recertificationListItem.RetrieveListItemValue(EzForms_UserRecertification.Field_ezItemID).ToInt32(0);
                            var requestAction = recertificationListItem.RetrieveListItemValue(EzForms_UserRecertification.Field_Routing_x0020_Phase);
                            var requestRecertificationDate = recertificationListItem.RetrieveListItemValue(EzForms_UserRecertification.Field_ezRecertificationDate).ToNullableDatetime();
                            var recertActionDate = recertificationListItem.RetrieveListItemValue(EzForms_UserRecertification.Field_Created).ToDateTime();

                            var requestItem = accessList.GetItemById(requestId);
                            this.ClientContext.Load(requestItem);
                            this.ClientContext.ExecuteQueryRetry();

                            var employee = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Employee);
                            var firstLevelApprover = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Division_x0020_Director_x0020_or);
                            var currentRequestStatus = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Status);
                            var currentRecertDate = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezformsNextCertifyDate).ToNullableDatetime();
                            var currentRoutingPhase = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Routing_x0020_Phase);
                            var requestWorkforceID = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezformsWorkforceID);
                            var localDottedAccount = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_ezFormsADAccount);
                            var userADInfoItems = RetrieveUserInfo(listADInfo, camlViewClause, requestWorkforceID);
                            if (userADInfoItems?.Count() > 1)
                            {
                                LogWarning($"Failed {employee.Email} with too many rows {userADInfoItems?.Count()}");
                                continue;
                            }
                            else
                            {
                                var userADInfo = userADInfoItems.FirstOrDefault();
                                var provisioningMessage = userADInfo.RetrieveListItemValue(EzForms_PrivADInfo.Field_DottedProvisioningMessage);
                                var provisioningList = new List<AccountProcessingActivityModel>();
                                if (!string.IsNullOrEmpty(provisioningMessage))
                                {
                                    var serializedMessages = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AccountProcessingActivityModel>>(provisioningMessage);
                                    if (serializedMessages != null && serializedMessages.Any())
                                    {
                                        provisioningList.AddRange(serializedMessages);
                                    }
                                }

                                provisioningList.Add(new AccountProcessingActivityModel()
                                {
                                    EventDate = recertActionDate,
                                    ExpirationDate = recertActionDate,
                                    ID = requestId,
                                    Message = string.Format("Disabling account {0}", localDottedAccount)
                                });


                                // Update Request with information
                                var updateRequest = false;
                                if (currentRequestStatus != "Denied")
                                {
                                    updateRequest = true;
                                    requestItem[EzForms_AccessRequest.Field_Request_x0020_Status] = "Denied"; // this sets the flag that the AD object is now expired
                                }
                                if (!currentRecertDate.HasValue || (currentRecertDate.HasValue && currentRecertDate != recertActionDate))
                                {
                                    updateRequest = true;
                                    requestItem[EzForms_AccessRequest.Field_ezformsNextCertifyDate] = recertActionDate;
                                }
                                if (currentRoutingPhase != "Denied")
                                {
                                    updateRequest = true;
                                    requestItem[EzForms_AccessRequest.Field_Routing_x0020_Phase] = "Denied";
                                }

                                if (updateRequest)
                                {
                                    requestItem.SystemUpdate();

                                    // Update the PrivADInfo list with the message
                                    var serializedMessage = Newtonsoft.Json.JsonConvert.SerializeObject(provisioningList.OrderByDescending(ob => ob.EventDate), Newtonsoft.Json.Formatting.None);
                                    userADInfo[EzForms_PrivADInfo.FieldDate_DottedAccountExpirationDate] = recertActionDate;
                                    userADInfo[EzForms_PrivADInfo.Field_DottedProvisioningMessage] = serializedMessage;
                                    userADInfo.SystemUpdate();
                                }

                                // Update Recertification record
                                recertificationListItem[EzForms_UserRecertification.Field_RowProcessed] = 1;
                                recertificationListItem.SystemUpdate();

                                this.ClientContext.ExecuteQueryRetry();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, "Failed to query UserRecertification items MSG:{0}", ex.Message);
                        }
                    }
                }
                while (camlRecertQuery.ListItemCollectionPosition != null);
            }
        }

        private ListItemCollection RetrieveUserInfo(List listADInfo, string camlViewClause, string workforceID)
        {
            // get ezforms site and query the list for approved requests
            var camlQuery = new CamlQuery
            {
                ListItemCollectionPosition = null,
                ViewXml = CAML.ViewQuery(ViewScope.DefaultValue, CAML.Where(CAML.Eq(CAML.FieldValue(EzForms_PrivADInfo.Field_EmployeeID, FieldType.Text.ToString("f"), workforceID))), string.Empty, camlViewClause, 5)
            };

            var spListItems = listADInfo.GetItems(camlQuery);
            this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
            this.ClientContext.ExecuteQueryRetry();
            camlQuery.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

            if (camlQuery.ListItemCollectionPosition != null)
            {
                LogWarning($"Too many rows were found for {workforceID}... please check with an admin.");
                return null;
            }


            return spListItems;
        }
    }
}
