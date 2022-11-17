CREATE VIEW [dbo].[vwTenantWebs]
AS
SELECT 
TOP 100 PERCENT
DATEDIFF(MM, tw.SiteLastModified, tw.DTUPD) as MonthDiff,
td.FormattedDate as ReportPeriodDate,
td.IsCurrent,
tw.ID,
tw.tenantSiteId,
tw.WebGuid,
tw.WebUrl,
tw.WebTitle,
tw.SiteTemplateId,
tw.SiteCreatedDate,
tw.SiteLastModified,
tw.SiteIsAddIn,
tw.Total_Hits,
tw.Total_Hits_HomePage,
tw.Unique_Visitors_Home_Page,
tw.Total_UniqueVisitors,
tw.DocumentCount,
tw.DocumentLastEditedDate,
tw.DiscussionLastDate,
tw.DTADDED, tw.DTUPD
from 
dbo.TenantWeb tw inner join dbo.TenantDates td on tw.DTUPD between td.DTSTART and td.DTEND
where td.IsCurrent = 1
order by tw.SiteLastModified desc
GO


