



CREATE VIEW [dbo].[vwO365PreviewOneDriveUsageMonthly]
AS
SELECT        
	rdate.ID AS ReportingID, 
	rdate.FormattedDate AS ReportingDate, 
	rusage.Office, 
	rusage.TotalUsers, 
	rusage.TotalActiveUsers, 
	rusage.TotalActiveUsersPerct as TotalUsersPerct, 
	rusage.TotalEmployeeUsers, 
	rusage.TotalEmployeeUsersPerct, 
	rusage.TotalContractorUsers, 
	rusage.TotalContractorUsersPerct, 
	rusage.UsingOneDriveCount, 
	rusage.UsingOneDrivePerct, 
	rusage.UsingOneDriveEnabledCount,
	rusage.UsingOneDriveEnabledPerct,
	rusage.UsingOneDriveEmployeeCount,
	rusage.UsingOneDriveEmployeePerct,
	rusage.UsingOneDriveContractorCount,
	rusage.UsingOneDriveContractorPerct,
	rusage.SyncingOneDriveCount, 
    rusage.SyncingOneDrivePerct, 
	rusage.SyncingOneDriveEnabledCount,
	rusage.SyncingOneDriveEnabledPerct,
	rusage.SyncingOneDriveEmployeeCount, 
	rusage.SyncingOneDriveEmployeePerct,
	rusage.SyncingOneDriveContractorCount,
	rusage.SyncingOneDriveContractorPerct,
	rusage.SharingOneDriveCount, 
    rusage.SharingOneDrivePerct, 
	rusage.SharingOneDriveEnabledCount,
	rusage.SharingOneDriveEnabledPerct,
	rusage.SharingOneDriveEmployeeCount, 
	rusage.SharingOneDriveEmployeePerct,
	rusage.SharingOneDriveContractorCount,
	rusage.SharingOneDriveContractorPerct
FROM            
	dbo.O365PreviewOneDriveDates AS rdate 
INNER JOIN
	dbo.O365PreviewOneDriveMonthlyRollup AS rusage ON rdate.ID = rusage.ReportingDateID