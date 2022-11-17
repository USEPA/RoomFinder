








CREATE VIEW [dbo].[AnalyticsSites]
AS
SELECT        
	tw.ID, 
	ts.Region, 
	ts.SiteType, 
	ts.SiteGuid AS SiteId,
	tw.tenantSiteId, 
	tw.WebGuid AS WebId, 
	tw.WebUrl AS Site, 
	tw.SiteTemplateId, 
	tw.SiteCreatedDate, 
	tw.SiteLastModified, 
	tw.SiteIsAddIn, 
    tw.DocumentActivityStatus, 
	tw.DocumentCount, 
	tw.DocumentLastEditedDate, 
	tw.SiteActivity, 
	tw.SiteMetadata, 
	tw.SiteMetadataCount, 
	tw.SiteMetadataPermissions, 
	tw.SiteMasterPage, 
	tw.Total_Hits, 
	tw.Total_UniqueVisitors, 
	tw.Total_Hits_HomePage, 
    tw.Unique_Visitors_Home_Page, 
	tw.WelcomePageError,
	tw.Total_Lists_Calculated,
	tw.HasCommunity, 
	tw.MemberJoinCount, 
	tw.MemberJoinLastDate, 
	tw.DiscussionCount, 
	tw.DiscussionLastDate, 
	tw.DiscussionReplyCount, 
	tw.DiscussionReplyLastDate
FROM            
dbo.TenantWeb AS tw INNER JOIN
dbo.TenantSite AS ts ON tw.tenantSiteId = ts.ID  CROSS JOIN
dbo.TenantDates AS td 
WHERE TW.DTUPD BETWEEN td.DTSTART AND td.DTEND AND td.IsCurrent = 1