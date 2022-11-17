CREATE VIEW [dbo].[zzOneDriveStats-Syncing]
AS
 select opm.ReportingDateID, odd.FormattedDate, odd.TotalMetricDays,
 sum(TotalUsers) TotalUsers, 
 sum(TotalActiveUsers) TotalActiveUsers, 
 sum(TotalEmployeeUsers) as TotalEmployeeUsers,
 sum(TotalContractorUsers) as TotalContractorUsers,
 convert(decimal(18,6), sum(TotalActiveUsers))/convert(decimal(18,6), sum(TotalUsers)) TotalActiveUsersperct,

 -- Using OneDrive
 sum(UsingOneDriveCount) totalusingonedrive,
 convert(decimal(18,6), sum(UsingOneDriveCount)) /convert(decimal(18,6), sum(TotalUsers)) totalusingonedriveperct,
 sum(UsingOneDriveEmployeeCount) totalUsingOneDriveEmployeeCount,
 convert(decimal(18,6), sum(UsingOneDriveEmployeeCount))/convert(decimal(18,6), sum(TotalActiveUsers)) totalUsingOneDriveEmployeeCountperct,
 convert(decimal(18,6), sum(UsingOneDriveEmployeeCount))/convert(decimal(18,6), sum(TotalEmployeeUsers)) totalUsingOneDriveActiveEmployeeCountperct,

 -- Syncing with OneDrive 
 sum(SyncingOneDriveCount) totalsyncingondrive,
 convert(decimal(18,6),  sum(SyncingOneDriveCount)) /convert(decimal(18,6), sum(TotalUsers)) totalsyncingonedriveperct,
 sum(SyncingOneDriveEmployeeCount) totalSyncingOneDriveEmployeeCount,
 convert(decimal(18,6),  sum(SyncingOneDriveEmployeeCount)) /convert(decimal(18,6), sum(TotalActiveUsers)) totalSyncingOneDriveEmployeeCountperct,
 convert(decimal(18,6),  sum(SyncingOneDriveEmployeeCount)) /convert(decimal(18,6), sum(TotalEmployeeUsers)) totalSyncingOneDriveActiveEmployeeCountperct,

-- Sharing with OneDrive
sum(SharingOneDriveCount) totalSharingOneDriveCount,
convert(decimal(18,6),  sum(SharingOneDriveCount)) /convert(decimal(18,6), sum(TotalUsers)) totalSharingOneDriveCountperct,
sum(SharingOneDriveEmployeeCount) totalSharingOneDriveEmployeeCount,
convert(decimal(18,6), sum(SharingOneDriveEmployeeCount))/convert(decimal(18,6), sum(TotalActiveUsers)) totalSharingOneDriveEmployeeCountperct,
convert(decimal(18,6), sum(SharingOneDriveEmployeeCount))/convert(decimal(18,6), sum(TotalEmployeeUsers)) totalSharingOneDriveActiveEmployeeCountperct

 from O365PreviewOneDriveMonthlyRollup opm
 inner join dbo.O365PreviewOneDriveDates odd on opm.ReportingDateID = odd.ID
 group by ReportingDateID, odd.FormattedDate, odd.TotalMetricDays
GO