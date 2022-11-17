

CREATE VIEW [dbo].[vwAnalyticsSitesRollup] AS
WITH SiteCollections AS
(
	SELECT 
		tsa.tenantSiteId, tsa.fkTenantDateId, tsa.TotalSiteUsers, tsa.SubSiteCount, tsa.Storage_Usage_MB, tsa.Storage_Usage_GB, tsa.Storage_Used_Perct, tsa.DTMETRIC, td.IsCurrent, td.DTEND
	FROM
		[dbo].TenantSiteAnalytics tsa 
	INNER JOIN
		[dbo].TenantDates td ON tsa.fkTenantDateId = td.ID
	WHERE EXISTS (
		SELECT MAX(tsd.DTMETRIC) FROM [dbo].TenantSiteAnalytics tsd WHERE tsa.tenantSiteId = tsd.tenantSiteId GROUP BY tsd.tenantSiteId HAVING tsa.DTMETRIC = MAX(tsd.DTMETRIC)
	)
),

SiteStatistics AS 
(
SELECT
	ts.tenantSiteId,
	DATEDIFF(MM, ts.DTEND, tw.DTUPD) MonthSinceLatestRun,  -- Pulls when the last scan took place
	CASE WHEN DATEDIFF(MM, ts.DTEND, tw.DTUPD) >= 0 THEN 1 ELSE 0 END AS ActiveWeb,
	CASE WHEN ts.IsCurrent = 1 THEN 1 ELSE 0 END AS ActiveSite,
	ts.IsCurrent,
	tw.DTUPD,
	tw.SiteActivity,
	tw.SiteTemplateId,
	tw.SiteIsAddIn,
	tw.WelcomePageError,
	tw.Total_Hits,
	tw.Total_UniqueVisitors
FROM
	[dbo].TenantWeb tw INNER JOIN
	SiteCollections ts ON tw.tenantSiteId = ts.tenantSiteId
),

SiteAnalytics AS
(
SELECT 
	CONVERT(DECIMAL(18, 6), ISNULL(COUNT(*), 0)) AS TotalSiteCount,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 THEN 1 ELSE 0 END), 0)) TotalCurrentSites,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 0 THEN 1 ELSE 0 END), 0)) TotalDeletedSites,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 THEN CONVERT(INT, SiteIsAddIn) ELSE 0 END), 0)) TotalAddIns,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 THEN CONVERT(INT, WelcomePageError) ELSE 0 END), 0)) AS TotalWelcomePageErrors,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 0 OR (SiteActivity = -1 OR SiteActivity > 730) THEN 1 ELSE 0 END), 0)) TotalUnused,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 AND (SiteActivity > 365 AND SiteActivity < 730) THEN 1 ELSE 0 END), 0)) TotalLowUsage,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 AND (SiteActivity > 90 AND SiteActivity < 365) THEN 1 ELSE 0 END), 0)) TotalMediumUsage,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 AND (SiteActivity > -1 AND SiteActivity < 90) THEN 1 ELSE 0 END), 0)) TotalHighUsage,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 THEN Total_Hits ELSE 0 END), 0)) AS TotalHits,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 THEN Total_UniqueVisitors ELSE 0 END), 0)) AS TotalUniqueVisitors,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 AND SiteTemplateId = 'STS' THEN 1 ELSE 0 END), 0)) AS TotalOrganizationalSites,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 AND SiteTemplateId = 'PROJECTSITE' THEN 1 ELSE 0 END), 0)) AS TotalWorkSites,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 AND SiteTemplateId = 'COMMUNITY' THEN 1 ELSE 0 END), 0)) AS TotalCommunitySites,
	CONVERT(DECIMAL(18, 6), ISNULL(SUM(CASE WHEN ss.ActiveWeb = 1 AND SiteTemplateId = 'ACCSVC' THEN 1 ELSE 0 END), 0)) AS TotalAccessServices
FROM 
	SiteStatistics ss
)


SELECT 
	TotalSiteCount,
	TotalCurrentSites,
	TotalDeletedSites,
	TotalAddIns,
	TotalWelcomePageErrors,
	TotalUnused,
	TotalUnused/TotalCurrentSites AS TotalUnusedPerct,
	TotalLowUsage,
	TotalLowUsage/TotalCurrentSites AS TotalLowUsagePerct,
	TotalMediumUsage,
	TotalMediumUsage/TotalCurrentSites AS TotalMediumUsagePerct,
	TotalHighUsage,
	TotalHighUsage/TotalCurrentSites AS TotalHighUsagePerct,
	TotalHits,
	TotalUniqueVisitors,
	TotalOrganizationalSites,
	TotalOrganizationalSites/TotalCurrentSites AS TotalOrganizationalSitesPerct,
	TotalWorkSites,
	TotalWorkSites/TotalCurrentSites AS TotalWorkSitesPerct,
	TotalCommunitySites,
	TotalCommunitySites/TotalCurrentSites AS TotalCommunitySitesPerct,
	TotalAccessServices,
	TotalAccessServices/TotalCurrentSites AS TotalAccessServicesPerct
FROM 
	SiteAnalytics