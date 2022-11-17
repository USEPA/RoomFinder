
CREATE VIEW [dbo].[OneDriveTotalFileActivity] AS
  SELECT odd.ID
, odd.FormattedDate
,count(oda.ReportingPeriodInDays) totalcaptureddays
      ,sum([ODB_TotalFileViewedModified]) AS TotalFileViewedModified
      ,sum([ODB_TotalFileSynched]) AS TotalFileSynced
      ,sum([ODB_TotalFileSharedEXT]) AS TotalFileSharedEXT
      ,sum([ODB_TotalFileSharedINT]) AS TotalFileSharedINT
  FROM [dbo].[GraphOneDriveActivityFiles] ODA
  INNER JOIN dbo.O365PreviewOneDriveDates odd on oda.LastActivityDate between odd.DTSTART and odd.DTEND
  group by odd.ID, odd.FormattedDate