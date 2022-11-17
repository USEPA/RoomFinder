







CREATE VIEW [dbo].[vwAnalyticsSitesOwners]
AS
SELECT        
	tw.ID, 
	ts.Region, 
	ts.SiteType, 
	ts.SiteGuid AS SiteId,
	tw.tenantSiteId, 
	tw.WebGuid AS WebId, 
	tw.WebUrl AS Site, 
	tw.SiteOwners
FROM            
dbo.TenantWeb AS tw INNER JOIN
dbo.TenantSite AS ts ON tw.tenantSiteId = ts.ID  CROSS JOIN
dbo.TenantDates AS td 
WHERE TW.DTUPD BETWEEN td.DTSTART AND td.DTEND AND td.IsCurrent = 1