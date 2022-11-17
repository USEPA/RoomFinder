CREATE VIEW [dbo].[vwO365PreviewOneDriveUsageMonthlyTotal]
AS
SELECT        
	rdate.ID AS ReportingID, 
	rdate.FormattedDate AS ReportingDate, 

	SUM(rusage.TotalUsers) TotalUsers, 
	SUM(rusage.TotalActiveUsers) TotalActiveUsers, 
	SUM(rusage.TotalActiveUsers)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalUsers)) as TotalUsersPerct, 
	SUM(rusage.TotalEmployeeUsers) AS TotalEmployeeUsers, 

	SUM(rusage.TotalEmployeeUsers)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalActiveUsers)) AS TotalEmployeeUsersPerct, 

	SUM(rusage.TotalContractorUsers) AS TotalContractorUsers, 
	SUM(rusage.TotalContractorUsers)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalActiveUsers)) AS TotalContractorUsersPerct, 

	SUM(rusage.UsingOneDriveCount) AS UsingOneDriveCount, 
	SUM(rusage.UsingOneDriveCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalUsers)) AS UsingOneDrivePerct, 
	SUM(rusage.UsingOneDriveEnabledCount) AS UsingOneDriveEnabledCount,
	SUM(rusage.UsingOneDriveEnabledCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalActiveUsers)) AS UsingOneDriveEnabledPerct,
	SUM(rusage.UsingOneDriveEmployeeCount) AS UsingOneDriveEmployeeCount,
	SUM(rusage.UsingOneDriveEmployeeCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalEmployeeUsers)) AS UsingOneDriveEmployeePerct,
	SUM(rusage.UsingOneDriveContractorCount) AS UsingOneDriveContractorCount,
	SUM(rusage.UsingOneDriveContractorCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalContractorUsers)) AS UsingOneDriveContractorPerct,
	SUM(rusage.SyncingOneDriveCount) AS SyncingOneDriveCount, 
    SUM(rusage.SyncingOneDriveCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalUsers)) AS SyncingOneDrivePerct, 
	SUM(rusage.SyncingOneDriveEnabledCount) AS SyncingOneDriveEnabledCount,
	SUM(rusage.SyncingOneDriveEnabledCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalActiveUsers)) AS SyncingOneDriveEnabledPerct,
	SUM(rusage.SyncingOneDriveEmployeeCount) AS SyncingOneDriveEmployeeCount, 
	SUM(rusage.SyncingOneDriveEmployeeCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalEmployeeUsers)) AS SyncingOneDriveEmployeePerct,
	SUM(rusage.SyncingOneDriveContractorCount) AS SyncingOneDriveContractorCount,
	SUM(rusage.SyncingOneDriveContractorCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalContractorUsers)) AS SyncingOneDriveContractorPerct,
	SUM(rusage.SharingOneDriveCount) AS SharingOneDriveCount, 
    SUM(rusage.SharingOneDriveCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalUsers)) AS SharingOneDrivePerct, 
	SUM(rusage.SharingOneDriveEnabledCount) AS SharingOneDriveEnabledCount,
	SUM(rusage.SharingOneDriveEnabledCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalActiveUsers)) AS SharingOneDriveEnabledPerct,
	SUM(rusage.SharingOneDriveEmployeeCount) AS SharingOneDriveEmployeeCount, 
	SUM(rusage.SharingOneDriveEmployeeCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalEmployeeUsers)) AS SharingOneDriveEmployeePerct,
	SUM(rusage.SharingOneDriveContractorCount) AS SharingOneDriveContractorCount,
	SUM(rusage.SharingOneDriveContractorCount)/CONVERT(DECIMAL(18,6), SUM(rusage.TotalContractorUsers)) AS SharingOneDriveContractorPerct
FROM            
	dbo.O365PreviewOneDriveDates AS rdate 
INNER JOIN
	dbo.O365PreviewOneDriveMonthlyRollup AS rusage ON rdate.ID = rusage.ReportingDateID
GROUP BY
	rdate.ID, rdate.FormattedDate