using CommandLine;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.PowerShell.Extensions;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using Newtonsoft.Json;
using OfficeDevPnP.Core.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("setEPAEZFormsAdminProcess", HelpText = "process ezforms into various folders and take administrative action.")]
    public class SetEPAEZFormsAdminProcessOptions : CommonOptions
    {
        [Option("log-directory", Required = true)]
        public string LogDirectory { get; set; }
    }

    public static class SetEPAEZFormsAdminProcessOptionsExtension
    {
        /// <summary>
        /// Will process ezforms into various folders and take administrative action
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this SetEPAEZFormsAdminProcessOptions opts, IAppSettings appSettings)
        {
            var cmd = new SetEPAEZFormsAdminProcess(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will read the privileged accounts tables 
    ///
    ///     anything set to Create - will kick off recertifications
    ///     anything set to Delete - will set the request as deleted and move it to a folder
    ///     anything set to Extend - will reset recertifications and forms
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "COM unknown exceptions.")]
    public class SetEPAEZFormsAdminProcess : BaseEZFormsRecertification<SetEPAEZFormsAdminProcessOptions>
    {
        public SetEPAEZFormsAdminProcess(SetEPAEZFormsAdminProcessOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        /// <summary>
        /// Format string for viewing the request
        /// </summary>
        internal string FormattedEditUrl
        {
            get
            {
                return "{0}SitePages/Edit%20Request.aspx?requestID={1}&Source={0}Lists/Access%20Requests/Recertification.aspx";
            }
        }

        #endregion

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();
            if (!System.IO.Directory.Exists(Opts.LogDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"Directory {Opts.LogDirectory} not found.");
            }
        }

        public override int OnRun()
        {
            var runDateTime = System.DateTime.Now;
            var formattedDateRun = runDateTime.ToString("yyyy-MM-ddTZ");
            var expirationDate = runDateTime.AddDays(30);
            var siteUrl = TokenHelper.EnsureTrailingSlash(this.ClientContext.Url);
            var emailObjects = new List<EZFormsPrivilegedAccountModel>();

            // Constants that related to AD Privileged Access
            var camlEqClause = CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, FieldType.Text.ToString("f"), EzForms_RequestType_Constants.AD_Privileged_Account));
            var camlViewClause = CAML.ViewFields(AccessRequestCamlFieldRefs.Select(s => CAML.FieldRef(s)).ToArray());

            var partialFolders = new string[] { "Archived", "Deleted", "2015", "2016", "2017", "2018" };


            // establish the UPS connection
            var peopleManager = new Microsoft.SharePoint.Client.UserProfiles.PeopleManager(ClientContext);

            // Get the Access Request list
            var listInSite = ClientContext.Web.GetListByTitle(EzForms_AccessRequest.ListName,
                      inc => inc.ItemCount,
                      inn => inn.RootFolder,
                      inn => inn.RootFolder.ServerRelativeUrl,
                      inn => inn.ParentWeb.ServerRelativeUrl,
                      inn => inn.LastItemModifiedDate);

            var deletedFolder = listInSite.GetOrCreateFolder(listInSite.RootFolder, "Deleted");
            if (deletedFolder == null)
            {
                LogWarning("Deleted folder not found; leaving the cmdlet");
                return -1;
            }

            deletedFolder.EnsureProperties(afold => afold.ServerRelativeUrl);
            var folderRelativeUrl = deletedFolder.ServerRelativeUrl;



            // Get the User Recertification List
            var userRecertList = ClientContext.Web.Lists.GetByTitle(EzForms_UserRecertification.ListName);
            ClientContext.Load(userRecertList);
            ClientContext.ExecuteQueryRetry();


            // Get the Privileged Process list which contains administrative actions
            var privProcessList = ClientContext.Web.GetListByTitle(EzForms_PrivProcess.ListName);

            // query the to be processed list and filter on any row that needs to be processed
            var camlProcessViewClause = (new string[] {
                EzForms_PrivProcess.Field_ID,
                EzForms_PrivProcess.Field_Title,
                EzForms_PrivProcess.Field_ezItemID,
                EzForms_PrivProcess.FieldChoice_ezAction,
                EzForms_PrivProcess.FieldUser_Employee,
                EzForms_PrivProcess.Field_ShortAccount,
                EzForms_PrivProcess.Field_LANEmployeeID,
                EzForms_PrivProcess.Field_DottedEmployeeNumber,
                EzForms_PrivProcess.Field_DottedSamAccountName,
                EzForms_PrivProcess.FieldDate_DottedAccountCreated,
                EzForms_PrivProcess.FieldBoolean_RowProcessed,
                EzForms_PrivProcess.FieldMulti_RowMessage,
                EzForms_PrivProcess.FieldUser_EmployeeManager,
                }).Select(s => CAML.FieldRef(s)).ToArray();

            var camlProcessQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(
                    ViewScope.RecursiveAll,
                    CAML.Where(
                        CAML.Eq(CAML.FieldValue(EzForms_PrivProcess.FieldBoolean_RowProcessed, FieldType.Boolean.ToString("f"), 0.ToString()))
                    ),
                    "",
                    CAML.ViewFields(camlProcessViewClause),
                    100)
            };

            var privProcess = new List<EZFormsPrivilegedAccountModel>();

            while (true)
            {
                var spListItems = privProcessList.GetItems(camlProcessQuery);
                ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition,
                    lti => lti.IncludeWithDefaultProperties(lnc => lnc.Id, lnc => lnc.ContentType));
                ClientContext.ExecuteQueryRetry();
                camlProcessQuery.ListItemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    var requestId = requestItem.Id;
                    var employee = requestItem.RetrieveListItemUserValue(EzForms_PrivProcess.FieldUser_Employee);
                    var manager = requestItem.RetrieveListItemUserValue(EzForms_PrivProcess.FieldUser_EmployeeManager);

                    LogWarning("Failed to retreive Employee for ID {0}", requestId);

                    var record = new EZFormsPrivilegedAccountModel()
                    {
                        ID = requestId,
                        RequestId = requestItem.RetrieveListItemValue(EzForms_PrivProcess.Field_ezItemID).ToInt32(0),
                        Action = requestItem.RetrieveListItemValue(EzForms_PrivProcess.FieldChoice_ezAction),
                        EmployeeId = employee.LookupId,
                        LANMail = employee.ToUserEmailValue(),
                        LANSamAccountName = requestItem.RetrieveListItemValue(EzForms_PrivProcess.Field_ShortAccount),
                        LANEmployeeID = requestItem.RetrieveListItemValue(EzForms_PrivProcess.Field_LANEmployeeID),
                        DottedEmployeeNumber = requestItem.RetrieveListItemValue(EzForms_PrivProcess.Field_DottedEmployeeNumber),
                        DottedSamAccountName = requestItem.RetrieveListItemValue(EzForms_PrivProcess.Field_DottedSamAccountName),
                        DottedAccountCreated = requestItem.RetrieveListItemValue(EzForms_PrivProcess.FieldDate_DottedAccountCreated).ToNullableDatetime(),
                        RowProcessed = requestItem.RetrieveListItemValue(EzForms_PrivProcess.FieldBoolean_RowProcessed).ToBoolean(),
                        RowMessage = requestItem.RetrieveListItemValue(EzForms_PrivProcess.FieldMulti_RowMessage),
                        Title = requestItem.RetrieveListItemValue(EzForms_PrivProcess.Field_Title)
                    };

                    if (manager != null)
                    {
                        record.EmployeeManagerId = manager.LookupId;
                        record.EmployeeManagerMail = manager.ToUserEmailValue();
                    }

                    privProcess.Add(record);
                }

                if (camlProcessQuery.ListItemCollectionPosition == null)
                {
                    break;
                }
            }



            /*
             * if request ID exists (process the row and check User Recertiifcations
             * if no request ID, check email for requests 
             *      if request was found, check user recertifications
             * */
            #region Delete and Move Requests

            foreach (var rowDeletion in privProcess.Where(w => w.Action == "Delete"))
            {
                LogVerbose("Checking retracting record {0}", rowDeletion.ID);
                if (rowDeletion.RequestId != 0)
                {
                    DeleteAndMoveRequest(listInSite, userRecertList, folderRelativeUrl, rowDeletion, rowDeletion.RequestId);
                }
                else
                {
                    var emailaddress = rowDeletion.LANMail;
                    var dottedaccount = rowDeletion.DottedSamAccountName;

                    var camleqclause = CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_ezFormsADAccount, "Text", dottedaccount));

                    if (!string.IsNullOrEmpty(emailaddress))
                    {
                        var userIdentity = string.Format("{0}|{1}", ClaimIdentifier, emailaddress);
                        try
                        {
                            var webUser = ClientContext.Web.EnsureUser(userIdentity);
                            ClientContext.Load(webUser);
                            ClientContext.ExecuteQueryRetry();

                            camleqclause = CAML.Or(camleqclause,
                                  CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Employee, "Lookup", webUser.Id.ToString(), "LookupId=\"true\"")));

                        }
                        catch (Exception ex)
                        {
                            var userException = string.Format("Failed to request user {1} with MSG:{0}", ex.Message, userIdentity);
                            LogError(ex, userException);
                        }
                    }

                    // query the list for any user without an AD LAN ID where Next Recertification date is > today
                    var foundItems = listInSite.GetItems(new CamlQuery()
                    {
                        ViewXml = CAML.ViewQuery(
                            CAML.Where(
                                CAML.And(
                                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, FieldType.Text.ToString("f"), EzForms_RequestType_Constants.AD_Privileged_Account)),
                                    camleqclause)),
                            string.Empty,
                            15)
                    });
                    listInSite.Context.Load(foundItems);
                    listInSite.Context.ExecuteQueryRetry();

                    if (foundItems.Any() && foundItems.Count() > 1)
                    {
                        // no records found in SharePoint
                        rowDeletion.ActionErrored = true;
                        rowDeletion.RowMessage = string.Format("Too many requests were found that matched {0} or {1}.  Please find the ID that matches the request.", dottedaccount, emailaddress);
                    }
                    else if (foundItems.Any())
                    {
                        var foundItem = foundItems.FirstOrDefault();

                        int RequestId = foundItem.Id;
                        DeleteAndMoveRequest(listInSite, userRecertList, folderRelativeUrl, rowDeletion, RequestId);
                    }
                    else
                    {
                        // no records found in SharePoint
                        rowDeletion.ActionErrored = true;
                        rowDeletion.RowMessage = "No request or dotted accounts were found.  Previously moved or deleted";
                    }
                }
            }

            #endregion

            #region Create Requests and Email Employees

            foreach (var rowCreate in privProcess.Where(w => w.Action == "Create"))
            {
                // the list of users with access to the request
                var approvalUsersList = new List<FieldUserValue>();
                // manager assocated with this request
                var managerId = default(System.Nullable<int>);
                var whenCreated = DateTime.Now;

                var userException = string.Empty;
                var userIdentity = string.Format("{0}|{1}", ClaimIdentifier, rowCreate.LANMail);
                IDictionary<string, string> userProperties = null;


                try
                {
                    var userProfile = peopleManager.GetPropertiesFor(userIdentity);
                    ClientContext.Load(userProfile);
                    ClientContext.ExecuteQueryRetry();

                    userProperties = userProfile.UserProfileProperties;
                }
                catch (Exception ex)
                {
                    userException = string.Format("Failed to request user with MSG:{0}", ex.Message);
                    LogError(ex, userException);
                }


                if (userProperties == null || userProperties.Count <= 0)
                {
                    // user no longer exists or can't be found in the SharePoint Profile, lets write failing message
                    rowCreate.ActionErrored = true;
                    rowCreate.RowMessage = userException;
                }
                else
                {

                    var emailSent = false;

                    try
                    {


                        var UserName = userProperties.RetrieveUserProperty("UserName");
                        var office = userProperties.RetrieveUserProperty(new string[] { "EPA-Department", "Department", "SPS-Department" }); // and value ORD/OSIM/IO
                        var userManager = userProperties.RetrieveUserProperty("Manager"); // 
                        var _workPhone = userProperties.RetrieveUserProperty("WorkPhone");
                        var _cubicle = userProperties.RetrieveUserProperty("EPA-OfficeCubicalLocation");
                        var _location = userProperties.RetrieveUserProperty("EPA-BuldingLocation");
                        var userFirstName = userProperties.RetrieveUserProperty("FirstName");
                        var userLastName = userProperties.RetrieveUserProperty("LastName");
                        var userDisplayName = userProperties.RetrieveUserProperty("PreferredName");

                        if (rowCreate.DottedAccountCreated.HasValue)
                        {
                            whenCreated = rowCreate.DottedAccountCreated.Value;
                        }
                        var webUser = ClientContext.Web.EnsureUser(userIdentity);
                        ClientContext.Load(webUser);
                        ClientContext.ExecuteQueryRetry();

                        var activityLog = string.Format(EzForms_AccessRequest.Formatted_Log, "Provisioned", "Approved", base.CurrentUserName, string.Empty, DateTime.Now.ToString(base.HourFormatString));

                        managerId = default;
                        approvalUsersList = new List<FieldUserValue>();
                        if (!string.IsNullOrEmpty(rowCreate.EmployeeManagerMail))
                        {
                            var managerIdentity = string.Format("{0}|{1}", ClaimIdentifier, rowCreate.EmployeeManagerMail);
                            var managerDisplayName = string.Empty;

                            try
                            {
                                var userProfile = peopleManager.GetPropertiesFor(managerIdentity);
                                ClientContext.Load(userProfile);
                                ClientContext.ExecuteQueryRetry();

                                managerDisplayName = userProfile.UserProfileProperties.RetrieveUserProperty("PreferredName");
                            }
                            catch (Exception ex)
                            {
                                LogError(ex, userException);
                            }

                            var managerUser = ClientContext.Web.EnsureUser(managerIdentity);
                            ClientContext.Load(managerUser);
                            ClientContext.ExecuteQueryRetry();

                            managerId = managerUser.Id;
                            approvalUsersList.Add(new FieldUserValue() { LookupId = managerId.Value });
                            if (string.IsNullOrEmpty(managerDisplayName))
                            {
                                managerDisplayName = managerUser.ToUserEmailValue();
                            }
                            activityLog += string.Format(EzForms_AccessRequest.Formatted_Log, "Approved", "Approved", managerDisplayName, string.Empty, DateTime.Now.ToString(base.HourFormatString));
                        }

                        // query the list for any user without an AD LAN ID where Next Recertification date is > today
                        var camlWhereClause = CAML.Where(
                            CAML.And(
                                camlEqClause,
                                CAML.And(
                                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Employee, "Lookup", webUser.Id.ToString(), "LookupId=\"true\"")),
                                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_ezformsWorkforceID, "Text", rowCreate.LANEmployeeID)))
                                ));

                        // get ezforms site and query the list for approved requests
                        var itemExists = false; var requestId = 0; var requestStatus = string.Empty;
                        ListItemCollectionPosition ListItemCollectionPosition = null;
                        var camlQuery = new CamlQuery
                        {
                            ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, camlWhereClause, "", camlViewClause, 100)
                        };
                        while (true)
                        {
                            camlQuery.ListItemCollectionPosition = ListItemCollectionPosition;
                            var spListItems = listInSite.GetItems(camlQuery);
                            ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition,
                                lti => lti.IncludeWithDefaultProperties(lnc => lnc.Id, lnc => lnc.ContentType));
                            ClientContext.ExecuteQueryRetry();

                            if (spListItems.Count() > 0)
                            {
                                itemExists = true;
                                var requestItem = spListItems.FirstOrDefault();
                                requestId = requestItem.Id;
                                requestStatus = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Status);
                            }

                            if (ListItemCollectionPosition == null)
                            {
                                break;
                            }

                        }

                        // In Case the user started the process ahead of time - lets store the request ID
                        if (itemExists)
                        {
                            rowCreate.RequestId = requestId;
                            rowCreate.ActionTaken = true;
                            rowCreate.RowMessage = string.Format("Request id {0} previously found, no action taken", requestId);
                        }
                        else
                        {
                            if (ShouldProcess(string.Format("The system will create a recertification record for {0}", rowCreate.LANSamAccountName)))
                            {
                                var newItem = new ListItemCreationInformation();
                                var _item = listInSite.AddItem(newItem);
                                var replacechars = new char[] { '/', '\\', '-' };
                                replacechars.ToList().ForEach(replaceit => office = office.Replace(replaceit, ','));
                                office = office.Replace(" ", "");
                                var officeSplit = office.Split(new string[] { "," }, StringSplitOptions.None);
                                var officeAcronym = officeSplit[0];

                                var employeeStatusBool = (rowCreate.LANEmployeeID.StartsWith("99", StringComparison.CurrentCultureIgnoreCase) ? "Yes" : "No");

                                _item[EzForms_AccessRequest.Field_Request_x0020_Type] = EzForms_RequestType_Constants.AD_Privileged_Account;
                                _item[EzForms_AccessRequest.Field_Request_x0020_Status] = "Recertification";
                                _item[EzForms_AccessRequest.Field_Routing_x0020_Phase] = "Recertification";
                                _item[EzForms_AccessRequest.Field_Previous_x0020_Routing_x0020_Pha] = "Approved";
                                _item[EzForms_AccessRequest.Field_Request_x0020_Date] = DateTime.Now.ToString("MM/dd/yyyy");
                                _item[EzForms_AccessRequest.Field_Employee] = new FieldUserValue() { LookupId = webUser.Id };
                                _item[EzForms_AccessRequest.Field_Office] = office;
                                _item[EzForms_AccessRequest.Field_Office_x0020_Acronym] = officeAcronym;
                                _item[EzForms_AccessRequest.Field_Activity_x0020_Log] = activityLog;
                                _item[EzForms_AccessRequest.Field_Justification] = "Auto provisioned from existing privileged accounts";
                                _item[EzForms_AccessRequest.Field_ezLanIdText] = rowCreate.LANSamAccountName;
                                _item[EzForms_AccessRequest.Field_ezFormsADAccount] = rowCreate.DottedSamAccountName;
                                _item[EzForms_AccessRequest.Field_ezPersonnelBool] = employeeStatusBool;
                                _item[EzForms_AccessRequest.Field_ezformsWorkforceID] = rowCreate.LANEmployeeID;
                                _item[EzForms_AccessRequest.Field_ezFormsWorkforceNumber] = rowCreate.DottedEmployeeNumber;
                                _item[EzForms_AccessRequest.Field_ezformsNextCertifyDate] = expirationDate;
                                _item[EzForms_AccessRequest.Field_ezformsADUserTermsDate] = whenCreated;
                                _item[EzForms_AccessRequest.Field_Building_x002C__x0020_Desk_x002F] = _location;
                                _item[EzForms_AccessRequest.Field_Location_x0020__x002d__x0020_Cit] = _workPhone;
                                _item[EzForms_AccessRequest.Field_DataMigrated] = 1; // Force this as a system action and not a user action

                                if (managerId.HasValue)
                                {
                                    _item[EzForms_AccessRequest.Field_Division_x0020_Director_x0020_or] = new FieldUserValue() { LookupId = managerId.Value };
                                }
                                if (approvalUsersList.Any())
                                {
                                    _item[EzForms_AccessRequest.Field_Approvers] = approvalUsersList;
                                }

                                _item.Update();
                                ClientContext.ExecuteQueryRetry();

                                requestId = _item.Id;

                                emailSent = SendEmailForRecertificationCreated(rowCreate, siteUrl, requestId, expirationDate);
                            }

                            rowCreate.EmailSent = emailSent;
                            rowCreate.RequestId = requestId;
                            rowCreate.ActionTaken = true;
                            rowCreate.RowMessage = string.Format("Recertification started for {0} with request ID {1}", rowCreate.LANMail, requestId);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Failed to assert request MSG:{0}", ex.Message);

                        // user no longer exists or can't be found in the SharePoint Profile, lets write failing message
                        rowCreate.ActionErrored = true;
                        rowCreate.RowMessage = ex.Message;
                    }
                }
            }

            #endregion

            #region Extending Requests

            foreach (var rowExtend in privProcess.Where(w => w.Action == "Extend"))
            {
                var userException = string.Empty;
                var userIdentity = string.Format("{0}|{1}", ClaimIdentifier, rowExtend.LANMail);
                IDictionary<string, string> userProperties = null;

                try
                {
                    var userProfile = peopleManager.GetPropertiesFor(userIdentity);
                    ClientContext.Load(userProfile);
                    ClientContext.ExecuteQueryRetry();

                    userProperties = userProfile.UserProfileProperties;
                }
                catch (Exception ex)
                {
                    userException = string.Format("Failed to request user with MSG:{0}", ex.Message);
                    LogError(ex, userException);
                }


                if (userProperties == null || userProperties.Count <= 0)
                {
                    // user no longer exists or can't be found in the SharePoint Profile, lets write failing message
                    rowExtend.ActionErrored = true;
                    rowExtend.RowMessage = userException;
                }
                else
                {

                    try
                    {
                        var UserName = userProperties.RetrieveUserProperty("UserName");

                        // get ezforms site and query the list for approved requests
                        var _item = listInSite.GetItemById(rowExtend.RequestId);
                        ClientContext.Load(_item);
                        ClientContext.ExecuteQueryRetry();

                        var requestId = _item.Id;
                        var requestStatus = "Recertification";
                        var employee = _item.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Employee);
                        var activityLog = _item.RetrieveListItemValue(EzForms_AccessRequest.Field_Activity_x0020_Log);
                        var fileRef = _item.RetrieveListItemValue(EzForms_AccessRequest.Field_FileRef);
                        var fileDirRef = _item.RetrieveListItemValue(EzForms_AccessRequest.Field_FileDirRef);
                        var emailSent = false;

                        if (employee == null || (rowExtend.EmployeeId != employee.LookupId))
                        {
                            rowExtend.ActionErrored = true;
                            rowExtend.RowMessage = string.Format("Employee {0} does not match the employee {1} in the request id {0} specified", rowExtend.LANMail, employee.ToUserEmailValue(), rowExtend.RequestId);
                            continue;
                        }


                        if (ShouldProcess(string.Format("The system will push out the recertification record for {0}", rowExtend.LANSamAccountName)))
                        {
                            activityLog += string.Format(EzForms_AccessRequest.Formatted_Log, "Extended", "Approved", base.CurrentUserName, string.Empty, DateTime.Now.ToString(base.HourFormatString));

                            _item[EzForms_AccessRequest.Field_Request_x0020_Status] = requestStatus;
                            _item[EzForms_AccessRequest.Field_Routing_x0020_Phase] = requestStatus;
                            _item[EzForms_AccessRequest.Field_Activity_x0020_Log] = activityLog;
                            _item[EzForms_AccessRequest.Field_ezformsNextCertifyDate] = expirationDate;
                            _item[EzForms_AccessRequest.Field_DataMigrated] = 1; // Force this as a system action and not a user action
                            _item.Update();

                            if (partialFolders.Any(s => fileRef.IndexOf("/" + s, StringComparison.CurrentCultureIgnoreCase) > 0))
                            {
                                var targetUrl = fileRef;
                                foreach (var partialFolder in partialFolders)
                                {
                                    targetUrl = targetUrl.Replace("/" + partialFolder, "");
                                }

                                var moved = ClientContext.MoveFileToFolder(fileRef, targetUrl);
                                if (!moved)
                                {
                                    LogWarning($"Failed to move {fileRef}");
                                }
                            }

                            ClientContext.ExecuteQueryRetry();

                            emailSent = SendEmailForRecertificationExtended(rowExtend, siteUrl, requestId, expirationDate);
                        }

                        rowExtend.EmailSent = emailSent;
                        rowExtend.RequestId = requestId;
                        rowExtend.ActionTaken = true;
                        rowExtend.RowMessage = string.Format("Request {0} successfully moved back into {1} status", requestId, requestStatus);
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Failed to assert request MSG:{0}", ex.Message);
                        rowExtend.ActionErrored = true;
                        rowExtend.RowMessage = string.Format("Failed to extend {0} with message {1}", rowExtend.RequestId, ex.Message);
                    }
                }
            }

            #endregion


            // Write the results of each activity back to the sharepoint list
            foreach (var privRow in privProcess.Where(s => s.ActionTaken == true || s.ActionErrored == true))
            {
                var privItem = privProcessList.GetItemById(privRow.ID);
                privProcessList.Context.Load(privItem);
                privProcessList.Context.ExecuteQueryRetry();

                try
                {

                    privRow.URL = string.Format(FormattedEditUrl, siteUrl, privRow.RequestId);
                    if (this.ShouldProcess(string.Format("Updating privprocessing row {0}", privRow.ID)))
                    {
                        if (privRow.RequestId != 0)
                        {
                            privItem[EzForms_PrivProcess.Field_ezItemID] = privRow.RequestId;
                        }
                        privItem[EzForms_PrivProcess.FieldBoolean_RowError] = (privRow.ActionErrored ? 1 : 0);
                        privItem[EzForms_PrivProcess.FieldBoolean_RowProcessed] = (privRow.ActionTaken ? 1 : 0);
                        privItem[EzForms_PrivProcess.FieldMulti_RowMessage] = privRow.RowMessage;

                        privItem.Update();
                        privItem.Context.ExecuteQueryRetry();
                    }

                    // spit back the results
                    emailObjects.Add(privRow);
                }
                catch (Exception ex)
                {
                    LogError(ex, "Failed to process priviledge request process {0}", ex.Message);
                }
            }


            if (ShouldProcess("Writing file to disc"))
            {
                var jsonPath = $"{Opts.LogDirectory}\\schedule-EZFormsAdminProcess-{DateTime.UtcNow:yyyy-MM-dd_HH_mm_ss}.json";
                var emailObjectsJson = JsonConvert.SerializeObject(emailObjects, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 5,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                System.IO.File.WriteAllText(jsonPath, emailObjectsJson);
            }

            if (Opts.Verbose)
            {
                var emails = "";
                emailObjects.ForEach((ezform) =>
                {
                    emails = $"{ezform.LANMail};{emails}";
                    LogVerbose($"Email {ezform.LANMail} URL {ezform.URL}");
                });
            }


            return 1;
        }

        private bool SendEmailForRecertificationCreated(EZFormsPrivilegedAccountModel user, string siteUrl, int requestId, DateTime expirationDate)
        {
            var requestUrl = string.Format(FormattedEditUrl, siteUrl, requestId, "");
            var userEmail = user.LANMail;
            var dottedExpirationDate = expirationDate.ToString("dddd dd MMMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            LogVerbose("ListItem [{0}] User:{1} Recertification:{2} sending email.", requestId, userEmail, dottedExpirationDate);

            var emailMessage = new StringBuilder();
            emailMessage.Append("<div>");
            emailMessage.Append("<div><br></div>");
            emailMessage.AppendFormat("<div>A recertification for your AD Privileged Access {0} account has been initiated on your behalf.</div>", user.DottedSamAccountName);
            emailMessage.AppendFormat("<div>You will be required to recertify your account before {0}.   After this date your AD Privileged account will be disabled.</div>", dottedExpirationDate);
            emailMessage.AppendFormat("<div><a href=\"{0}\" title=\"click here to open the form\">Click here to view</a>", requestUrl);
            emailMessage.Append("<div><br></div>");
            emailMessage.Append("<div>Thank you,</div>");
            emailMessage.Append("<div>EZ Forms Support</div>");
            emailMessage.Append("</div>");
            var emailString = emailMessage.ToString();


            var properties = new EmailProperties
            {
                To = new string[] { userEmail },
                BCC = EmailBCC,
                Subject = "AD Privileged Access: Recertification required.",
                Body = emailString
            };

            if (ShouldProcess($"Sending email to {properties.To} with subject {properties.Subject}"))
            {
                try
                {
                    Microsoft.SharePoint.Client.Utilities.Utility.SendEmail(this.ClientContext, properties);
                    this.ClientContext.ExecuteQueryRetry();
                    return true;
                }
                catch (Exception emailEx)
                {
                    LogWarning("Failed to relay creation email to user {0} for request ID {1} EX:{2}", userEmail, requestId, emailEx.Message);
                    return false;
                }
            }
            return false;
        }

        private bool SendEmailForRecertificationExtended(EZFormsPrivilegedAccountModel user, string siteUrl, int requestId, DateTime expirationDate)
        {
            var requestUrl = string.Format(FormattedEditUrl, siteUrl, requestId, "");
            var userEmail = user.LANMail;
            var dottedAccount = user.DottedSamAccountName;
            var dottedExpirationDate = expirationDate.ToString("dddd dd MMMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            LogVerbose("ListItem [{0}] User:{1} Recertification:{2} will be updated.", requestId, userEmail, dottedExpirationDate);

            var emailMessage = new StringBuilder();
            emailMessage.Append("<div>");
            emailMessage.Append("<div><br></div>");
            emailMessage.AppendFormat("<div>A recertification for your AD Privileged Access {0} account has been extended on your behalf.</div>", dottedAccount);
            emailMessage.AppendFormat("<div>You will be required to recertify your account before {0}.   After this date your AD Privileged account will be disabled.</div>", dottedExpirationDate);
            emailMessage.AppendFormat("<div><a href=\"{0}\" title=\"click here to open the form\">Click here to view</a>", requestUrl);
            emailMessage.Append("<div><br></div>");
            emailMessage.Append("<div>Thank you,</div>");
            emailMessage.Append("<div>EZ Forms Support</div>");
            emailMessage.Append("</div>");
            var emailString = emailMessage.ToString();


            var properties = new EmailProperties
            {
                To = new string[] { userEmail },
                BCC = EmailBCC,
                Subject = "AD Privileged Access: Recertification required.",
                Body = emailString
            };

            if (ShouldProcess($"Sending email to {properties.To} with subject {properties.Subject}"))
            {
                try
                {
                    Microsoft.SharePoint.Client.Utilities.Utility.SendEmail(this.ClientContext, properties);
                    this.ClientContext.ExecuteQueryRetry();
                    return true;
                }
                catch (Exception emailEx)
                {
                    LogWarning("Failed to relay extension email to user {0} for request ID {1} EX:{2}", userEmail, requestId, emailEx.Message);
                    return false;
                }
            }
            return false;
        }

        private void DeleteAndMoveRequest(List listInSite, List userRecertList, string folderRelativeUrl, EZFormsPrivilegedAccountModel deletion, int RequestId)
        {
            try
            {
                // get the request
                var requestItem = listInSite.GetItemById(RequestId);
                ClientContext.Load(requestItem);

                // query for the user recertification rows
                var userRecertItems = userRecertList.GetItems(new CamlQuery()
                {
                    ViewXml = CAML.ViewQuery(
                        CAML.Where(
                            CAML.And(
                                CAML.Eq(CAML.FieldValue(EzForms_UserRecertification.Field_ezItemID, FieldType.Number.ToString("f"), RequestId.ToString())),
                                CAML.Eq(CAML.FieldValue(EzForms_UserRecertification.Field_RowProcessed, FieldType.Boolean.ToString("f"), 0.ToString())))),
                        string.Empty,
                        20)
                });
                ClientContext.Load(userRecertItems);
                ClientContext.ExecuteQueryRetry();


                var targetSet = false;
                var employee = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Employee);
                var fileRef = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_FileRef);
                var fileDirRef = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_FileDirRef);
                var created = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Created).ToDateTime();
                var modified = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Modified).ToDateTime();
                var modifiedBy = requestItem.RetrieveListItemUserValue(EzForms_AccessRequest.Field_Editor);
                var requestType = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Type);
                var requestStatus = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Status);
                var routingPhase = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Routing_x0020_Phase);

                if (employee == null || (deletion.EmployeeId != employee.LookupId))
                {
                    deletion.ActionErrored = true;
                    deletion.RowMessage = string.Format("Employee {0} does not match the employee {1} in the request id {0} specified", deletion.LANMail, employee.ToUserEmailValue(), deletion.RequestId);
                    return;
                }

                if (!requestStatus.Equals("deleted", StringComparison.CurrentCultureIgnoreCase)
                    || !routingPhase.Equals("deleted", StringComparison.CurrentCultureIgnoreCase))
                {
                    // lets move the archived files
                    targetSet = true;
                    LogVerbose("Request not in deleted state");
                }

                foreach (var userRecertItem in userRecertItems)
                {
                    LogVerbose("Processing Recertification row {0}", userRecertItem.Id);
                    userRecertItem[EzForms_UserRecertification.Field_RowProcessed] = 1;
                    userRecertItem.SystemUpdate();
                }

                if (!targetSet)
                {
                    // we moved the sharepoint record to the deleted folder, updated any Recertification rows
                    deletion.ActionTaken = true;
                    deletion.RowMessage = string.Format("Previously Moved or Deleted the request. Current status {0} and phase {1}", requestStatus, routingPhase);
                }
                else
                {
                    if (this.ShouldProcess(string.Format("File {0} url {1} created {2} modified {3} by {4}", RequestId, fileRef, created, modified, modifiedBy.Email)))
                    {

                        requestItem[EzForms_AccessRequest.Field_Routing_x0020_Phase] = "Deleted";
                        requestItem[EzForms_AccessRequest.Field_Request_x0020_Status] = "Deleted";
                        requestItem.SystemUpdate();

                        if (fileRef.IndexOf("deleted", StringComparison.CurrentCultureIgnoreCase) < 0)
                        {
                            var targetUrl = fileRef.Replace(fileDirRef, folderRelativeUrl);
                            
                            LogVerbose("Moving Request {0} to {1}", RequestId, targetUrl);
                            var moved = ClientContext.MoveFileToFolder(fileRef, targetUrl);
                            if (!moved)
                            {
                                LogWarning($"Failed to move {fileRef}");
                            }
                        }
                    }

                    // we moved the sharepoint record to the deleted folder, updated any Recertification rows
                    deletion.ActionTaken = true;
                    deletion.RowMessage = "Moved and Deleted the request successfully";
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to move item from access list into deleted folder");
                deletion.ActionErrored = true;
                deletion.RowMessage = string.Format("Failed to access, move, or delete request with message {0}", ex.Message);
            }
        }

    }
}
