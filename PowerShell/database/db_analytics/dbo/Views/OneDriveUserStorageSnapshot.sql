
CREATE VIEW [dbo].[OneDriveUserStorageSnapshot] AS
SELECT odd.ID AS ReportingID
	  ,odd.FormattedDate as ReportingDate
	  ,[LastActivityDate]
      ,[SiteURL]
      ,[UPN]
      ,[SiteOwner]
      ,[Deleted]
      ,[TotalFiles]
      ,[TotalFilesViewedModified]
      ,[Storage_Allocated_B]
      ,[Storage_Used_B]
      ,(convert(decimal(28,6), [Storage_Used_B])/convert(decimal(28,6),(1048576))) AS [Storage_Used_MB]
      ,(convert(decimal(28,6), [Storage_Used_B])/convert(decimal(28,6),(1073741824))) AS [Storage_Used_GB]
      ,(convert(decimal(28,6), [Storage_Used_B])/convert(decimal(28,6),(1099511627776))) AS [Storage_Used_TB]
  FROM [dbo].[GraphOneDriveUsageDetail] oud
  left outer join dbo.O365PreviewOneDriveDates odd on oud.LastActivityDate between odd.DTSTART and odd.DTEND