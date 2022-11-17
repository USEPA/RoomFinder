using TinyCsvParser.Mapping;

namespace EPA.Office365.Graph.Reporting.TenantReport
{
    public class CsvOffice365GroupsActivityDetailMapping : CsvMapping<Office365GroupsActivityDetail>
    {
        public CsvOffice365GroupsActivityDetailMapping()
                : base()
        {
            MapProperty(0, x => x.ReportRefreshDate);
            MapProperty(1, x => x.GroupName);
            MapProperty(2, x => x.IsDeleted);
            MapProperty(3, x => x.OwnerPrincipalName);
            MapProperty(4, x => x.LastActivityDate);
            MapProperty(5, x => x.GroupType);
            MapProperty(6, x => x.MemberCount);
            MapProperty(7, x => x.ExternalMemberCount);
            MapProperty(8, x => x.ExchangeReceivedEmailCount);
            MapProperty(9, x => x.SharePointActiveFileCount);
            MapProperty(10, x => x.YammerPostedMessageCount);
            MapProperty(11, x => x.YammerReadMessageCount);
            MapProperty(12, x => x.YammerLikedMessageCount);
            MapProperty(13, x => x.ExchangeMailboxTotalItemCount);
            MapProperty(14, x => x.ExchangeMailboxStorageUsed_Byte);
            MapProperty(15, x => x.SharePointTotalFileCount);
            MapProperty(16, x => x.SharePointSiteStorageUsed_Byte);
            MapProperty(17, x => x.GroupId);
            MapProperty(18, x => x.ReportPeriod);
        }
    }
}
