


CREATE VIEW [dbo].[vwAnalyticsSites]
AS

with results AS 
(
SELECT [Site]
      ,[Region]
      ,[SiteType]
      ,[DocumentCount]
      ,DocumentLastEditedDate
	  ,CASE WHEN DATEDIFF(DD, isnull(eas.DocumentLastEditedDate, DATEADD(M, -5, GETDATE())), GETDATE()) > 195 THEN -20 ELSE LOG(5, DATEDIFF(s, DocumentLastEditedDate, GETDATE()))*86400 END AS Freshness
      ,[Total_Hits]
      ,[Total_UniqueVisitors]
      ,[Total_Hits_HomePage]
      ,[Unique_Visitors_Home_Page]
  FROM [dbo].[AnalyticsSites] eas
  ),
  Uniqueness AS (
  select top 10 'Unique' as Filter, *, eas.Freshness * .25 as FreshnessRatio, eas.[Total_UniqueVisitors] * 2 as UniqueCountRatio
  from results eas
  order by UniqueCountRatio desc
  ),
  Freshness AS (
  select top 10 'Freshness' as Filter, *, eas.Freshness * 2 as FreshnessRatio, eas.[Total_UniqueVisitors] * .25 as UniqueCountRatio
  from results eas
 order by FreshnessRatio desc
 ),
 TotalResults AS (
	select * from Uniqueness
	union all
	select * from Freshness
 )

 select * from TotalResults