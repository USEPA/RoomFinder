




CREATE VIEW [dbo].[AnalyticsSiteCollection]
AS
SELECT        
	ts.ID, 
	ts.SiteGuid AS SiteId, 
	ts.SiteUrl AS Site, 
	ts.Region, 
	ts.SiteType, 
	tsa.TotalSiteUsers, 
	tsa.SubSiteCount, 
	tsa.TrackedSite, 
	tsa.Storage_Usage_MB, 
	tsa.Storage_Usage_GB, 
	tsa.Storage_Used_Perct, 
	tsa.Storage_Allocated_MB, 
    tsa.Storage_Allocated_GB, 
	tsa.DTMETRIC, 
	td.FormattedDate
FROM            
	dbo.TenantSite AS ts INNER JOIN
	dbo.TenantSiteAnalytics AS tsa ON ts.ID = tsa.tenantSiteId INNER JOIN
	dbo.TenantDates AS td on tsa.fkTenantDateId = td.ID
WHERE 
	td.IsCurrent = 1