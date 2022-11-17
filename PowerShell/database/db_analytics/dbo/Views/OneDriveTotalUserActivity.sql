
CREATE VIEW [dbo].[OneDriveTotalUserActivity] AS
SELECT odd.ID
, odd.FormattedDate
,count(oda.ReportingPeriodInDays) totalcaptureddays
      ,sum([ODB_TotalFileViewedModified]) AS UserTotalFileViewedModified
      ,sum([ODB_TotalFileSynched]) AS UserTotalFileSynced
      ,sum([ODB_TotalFileSharedEXT]) AS UserTotalFileSharedEXT
      ,sum([ODB_TotalFileSharedINT]) AS UserTotalFileSharedINT
  FROM [dbo].[GraphOneDriveActivityUsers] ODA
  INNER JOIN dbo.O365PreviewOneDriveDates odd on oda.LastActivityDate between odd.DTSTART and odd.DTEND
  group by odd.ID, odd.FormattedDate