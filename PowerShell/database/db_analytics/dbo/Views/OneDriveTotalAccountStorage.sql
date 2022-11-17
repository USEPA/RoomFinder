
CREATE VIEW [dbo].[OneDriveTotalAccountStorage] AS
SELECT odd.ID
, odd.FormattedDate
,count(oda.ReportingPeriodInDays) totalcaptureddays
      ,max([Storage_Used_B]) AS StorageBytes
      ,max((convert(decimal(28,6), [Storage_Used_B])/convert(decimal(28,6),(1048576)))) AS [Storage_Used_MB]
      ,max((convert(decimal(28,6), [Storage_Used_B])/convert(decimal(28,6),(1073741824)))) AS [Storage_Used_GB]
      ,max((convert(decimal(28,6), [Storage_Used_B])/convert(decimal(28,6),(1099511627776)))) AS [Storage_Used_TB]
  FROM [dbo].[GraphOneDriveUsageStorage] ODA
  INNER JOIN dbo.O365PreviewOneDriveDates odd on oda.LastActivityDate between odd.DTSTART and odd.DTEND
where ODA.SiteType = 'OneDrive'
group by odd.ID, odd.FormattedDate