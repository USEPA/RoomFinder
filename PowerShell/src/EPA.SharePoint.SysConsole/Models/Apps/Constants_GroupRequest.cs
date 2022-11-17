using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class Constants_GroupRequest
    {
        public const string GroupRequestListName = "GroupRequests";

        public class GroupRequestFields : ConstantsListFields
        {
            public const string FieldText_RequestCompletedFlag = "RequestCompletedFlag";
            public const string FieldText_RequestApprovedFlag = "RequestApprovedFlag";
            public const string FieldText_RequestRejectedFlag = "RequestRejectedFlag";
            public const string FieldText_EmailAddress = "EmailAddress";
            public const string FieldText_SharePointDocumentsUrl = "SharePointDocumentsUrl";
            public const string FieldText_SharePointSiteUrl = "SharePointSiteUrl";
            public const string FieldDate_GroupCreatedDate = "GroupCreatedDate";

            public const string FieldChoice_PublicFlag = "PublicFlag";
            public const string FieldText_GroupName = "GroupName";
            public const string FieldText_GroupOwner = "GroupOwner";
            public const string FieldText_GroupOwnerName = "GroupOwnerName";
            public const string FieldText_GroupMembers = "GroupMembers";
            public const string FieldMultiText_Description = "Description";
        }
    }
}
