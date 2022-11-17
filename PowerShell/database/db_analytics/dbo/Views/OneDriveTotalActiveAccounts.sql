
CREATE VIEW [dbo].[OneDriveTotalActiveAccounts] AS
SELECT odd.ID
, odd.FormattedDate
,count(oda.ReportingPeriodInDays) totalcaptureddays
      ,max(Total_Accounts) AS TotalAccounts
      ,sum(Total_ActiveAccounts) AS TotalActiveAccountsAggregate
  FROM [dbo].GraphOneDriveUsageAccount ODA
  INNER JOIN dbo.O365PreviewOneDriveDates odd on oda.LastActivityDate between odd.DTSTART and odd.DTEND
  group by odd.ID, odd.FormattedDate