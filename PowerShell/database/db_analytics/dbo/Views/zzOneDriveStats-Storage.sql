CREATE VIEW dbo.[zzOneDriveStats-Storage] 
AS
with storageas as (
	SELECT [LastActivityDate], DATEPART(DD, [LastActivityDate]) AS LastDay, DATEPART(MM, [LastActivityDate]) AS LastMonth, DATEPART(YYYY, [LastActivityDate]) AS LastYear,
      [SiteType],
      [ReportingPeriodInDays],
	  gds.[Storage_Used_B], 
	  gds.Storage_Used_MB, 
	  gds.Storage_Used_GB, 
	  gds.Storage_Used_TB
	from 
	  [dbo].[GraphOneDriveUsageStorage] gds
	WHERE gds.SiteType = 'OneDrive'
)


select 
	LastActivityDate, SiteType, sas.Storage_Used_TB, sas.Storage_Used_GB, sas.Storage_Used_MB
from storageas sas
WHERE EXISTS(SELECT 1
         FROM storageas t2
         WHERE t2.LastMonth = sas.LastMonth
           AND t2.LastYear = sas.LastYear
         GROUP BY t2.LastMonth,
                  t2.LastYear
         HAVING sas.LastActivityDate = MAX(t2.LastActivityDate))
GO