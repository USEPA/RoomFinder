CREATE VIEW [dbo].[vwTenantWebAddIns]
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
,twa.[tenantWebId]
,twa.[SiteGuid]
,twa.[AppGuid]
,twa.[AppTitle]
,twa.[AppDescription]
,twa.[AppPermissions]
,twa.[AppStatus]
,twa.[HostedType]
,twa.[HostWebUrl]
,twa.[AppRedirectUrl]
,twa.[AppPrincipalId]
,twa.[AppWebFullUrl]
,twa.[ImageFallbackUrl]
,twa.[ImageUrl]
,twa.[InError]
,twa.[ProductGuid]
,twa.[RemoteAppUrl]
,twa.[SettingsPageUrl]
,twa.[StartPage]
,twa.[EulaUrl]
,twa.[PrivacyUrl]
,twa.[Publisher]
,twa.[SupportUrl]
,vaso.SiteOwners
from 
dbo.TenantWeb tw inner join 
dbo.vwAnalyticsSitesOwners vaso on vaso.WebId = tw.WebGuid inner join
dbo.TenantDates td on tw.DTUPD between td.DTSTART and td.DTEND inner join
dbo.TenantWebAddIn twa on tw.WebGuid = twa.WebGuid
where td.IsCurrent = 1
order by tw.SiteLastModified desc
GO


