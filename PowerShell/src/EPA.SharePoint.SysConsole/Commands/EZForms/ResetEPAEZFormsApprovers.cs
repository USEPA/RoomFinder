using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("resetEPAEZFormsApprovers", HelpText = "will reset approvers for ezform requests.")]
    public class ResetEPAEZFormsApproversOptions : CommonOptions
    {
        [Option("approver-column", Required = true)]
        public string ApproverColumn { get; set; }

        [Option("existing-useridentity", Required = true)]
        public string ExistingUserIdentity { get; set; }

        [Option("replacement-useridentity", Required = true)]
        public string ReplacementUserIdentity { get; set; }
    }

    public static class ResetEPAEZFormsApproversOptionsExtension
    {
        /// <summary>
        /// Will execute the scan for OneDrive changes
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="appSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static int RunGenerateAndReturnExitCode(this ResetEPAEZFormsApproversOptions opts, IAppSettings appSettings)
        {
            var cmd = new ResetEPAEZFormsApprovers(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will bulk modify EZ Forms approvers
    /// </summary>
    public class ResetEPAEZFormsApprovers : BaseEZFormsRecertification<ResetEPAEZFormsApproversOptions>
    {
        public ResetEPAEZFormsApprovers(ResetEPAEZFormsApproversOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables


        #endregion

        public override int OnRun()
        {

            var existingUser = ClientContext.Web.EnsureUser(EncodeUsername(Opts.ExistingUserIdentity));
            ClientContext.Load(existingUser, ctx => ctx.Id, ctx => ctx.Email, ctx => ctx.UserId);

            var targetUser = ClientContext.Web.EnsureUser(EncodeUsername(Opts.ReplacementUserIdentity));
            ClientContext.Load(targetUser, ctx => ctx.Id, ctx => ctx.Email, ctx => ctx.UserId);

            // Query both users
            ClientContext.ExecuteQueryRetry();


            var accessRequestFields = new List<string>()
            {
                EzForms_AccessRequest.Field_Request_x0020_Type,
                EzForms_AccessRequest.Field_Request_x0020_Status,
                EzForms_AccessRequest.Field_Routing_x0020_Phase,
                EzForms_AccessRequest.Field_Employee,
                EzForms_AccessRequest.Field_Office,
                EzForms_AccessRequest.Field_Office_x0020_Acronym,
                EzForms_AccessRequest.Field_ezLanIdText,
                EzForms_AccessRequest.Field_ezFormsADAccount,
                EzForms_AccessRequest.Field_ezformsWorkforceID,
                EzForms_AccessRequest.Field_Division_x0020_Director_x0020_or,
                EzForms_AccessRequest.Field_ezformsNextCertifyDate,
                EzForms_AccessRequest.Field_Computer_x0020_Name,
                EzForms_AccessRequest.Field_Request_x0020_Date,
                EzForms_AccessRequest.Field_ezPersonnelBool,
                EzForms_AccessRequest.Field_Activity_x0020_Log
            };


            accessRequestFields.Add(Opts.ApproverColumn);

            // The View XML for the requisite fields
            var camlViewClause = CAML.ViewFields(accessRequestFields.Select(s => CAML.FieldRef(s)).ToArray());

            //TODO: Update where clause to have an OVERRIDE

            var camlWhereClause = CAML.And(
                CAML.Or(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), EzForms_RequestStatus_Constants.Pending)),
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), EzForms_RequestStatus_Constants.Recertification))
                ),
                CAML.Eq(CAML.FieldValue(Opts.ApproverColumn, FieldType.User.ToString("f"), existingUser.Id.ToString(), "LookupId='TRUE'"))
                );


            camlWhereClause = CAML.And(
                CAML.And(
                    CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Type, FieldType.Text.ToString("f"), EzForms_RequestType_Constants.AD_Privileged_Account)),
                    CAML.Eq(CAML.FieldValue(Opts.ApproverColumn, FieldType.User.ToString("f"), existingUser.Id.ToString(), "LookupId='TRUE'"))
                    ),
                CAML.Eq(CAML.FieldValue(EzForms_AccessRequest.Field_Request_x0020_Status, FieldType.Text.ToString("f"), EzForms_RequestStatus_Constants.Approved))
                );


            // get ezforms site and query the list for approved requests
            ListItemCollectionPosition itemCollectionPosition = null;
            var camlQuery = new CamlQuery()
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, CAML.Where(camlWhereClause), "", camlViewClause, 100)
            };

            var accessRequestList = this.ClientContext.Web.Lists.GetByTitle(EzForms_AccessRequest.ListName);
            this.ClientContext.Load(accessRequestList);
            this.ClientContext.ExecuteQueryRetry();

            var output = new List<UserForms>();
            while (true)
            {

                LogDebugging("Query: {0}  as position {1}", camlQuery.ViewXml, (itemCollectionPosition == null ? "Default" : itemCollectionPosition.PagingInfo));

                camlQuery.ListItemCollectionPosition = itemCollectionPosition;
                var spListItems = accessRequestList.GetItems(camlQuery);
                this.ClientContext.Load(spListItems, lti => lti.ListItemCollectionPosition);
                this.ClientContext.ExecuteQueryRetry();
                itemCollectionPosition = spListItems.ListItemCollectionPosition;

                foreach (var requestItem in spListItems)
                {
                    var requestId = requestItem.Id;
                    var userItem = new UserForms
                    {
                        Id = requestItem.Id,
                        ExistingColumn = requestItem.RetrieveListItemUserValue(Opts.ApproverColumn),
                        RequestType = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Type),
                        RequestPhase = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Routing_x0020_Phase),
                        RequestStatus = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Request_x0020_Status)
                    };
                    output.Add(userItem);
                }

                if (itemCollectionPosition == null)
                {
                    break;
                }
            }

            foreach (var request in output)
            {
                var requestItem = accessRequestList.GetItemById(request.Id);
                this.ClientContext.Load(requestItem);
                this.ClientContext.ExecuteQueryRetry();

                if (ShouldProcess(string.Format("Updating user column {0} for new approver {1}", Opts.ApproverColumn, targetUser.Email)))
                {
                    requestItem[Opts.ApproverColumn] = new FieldUserValue() { LookupId = targetUser.Id };
                    requestItem.SystemUpdate();
                    accessRequestList.Context.ExecuteQueryRetry();
                }
            }

            return 1;
        }

        internal class UserForms
        {
            public FieldUserValue ExistingColumn { get; set; }
            public int Id { get; set; }
            public string RequestType { get; set; }
            public string RequestStatus { get; set; }
            public string RequestPhase { get; set; }
        }
    }
}
