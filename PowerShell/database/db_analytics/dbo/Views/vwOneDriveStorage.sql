CREATE VIEW [dbo].[vwOneDriveStorage]
	AS
SELECT 
	odd.ID AS ReportingID
	,odd.FormattedDate AS ReportingDate
	,eba.OfficeName AS Department
	,eba.OrgOffice AS [Office]
	,eba.OrgOfficeBranch AS [Branch]
	,eba.OrgOfficeBranchSection AS [Section]
	,eba.OrgOfficeBranchUnit AS [Unit]
	,case when [ODB_TotalofAllActivities] > 0 or Storage_Used > 0 then 1 else 0 end AS UsingOneDrive
	,case when [ODB_TotalofAllActivities] > 0 or Storage_Used > 0 then 'Yes' else 'No' end AS UsingOneDriveText
	,case when [ODB_TotalofAllActivities] > 0 or Storage_Used > 0 then 0 else 1 end AS NotUsingOneDrive
	,case when [ODB_TotalofAllActivities] > 0 or Storage_Used > 0 then 'Yes' else 'No' end AS NotUsingOneDriveText
	,case when [ODB_TotalFileSynched] > 0 then 1 else 0 end AS UsersSyncing
	,case when [ODB_TotalFileSynched] > 0 then 'Yes' else 'No' end AS UsersSyncingText
	,case when ODB_TotalFileSharedEXT > 0 or ODB_TotalFileSharedINT > 0 then 1 else 0 end AS UsersSharing
	,case when ODB_TotalFileSharedEXT > 0 or ODB_TotalFileSharedINT > 0 then 'Yes' else 'No' end AS UsersSharingText
	,aup.ProfilePicture
	,aup.TotalFiles
	,aup.Storage_Allocated
	,aup.Storage_Used
	,aup.Storage_Used_Perct
FROM [dbo].O365PreviewOneDriveMonthly oda
  inner join dbo.O365PreviewOneDriveDates odd on oda.ReportingDateID = odd.ID
  inner join dbo.eBusinessAccounts eba on oda.UPN = eba.UserEmailAddress
  inner join dbo.AnalyticsUserProfiles aup on oda.UPN = aup.Username
GO