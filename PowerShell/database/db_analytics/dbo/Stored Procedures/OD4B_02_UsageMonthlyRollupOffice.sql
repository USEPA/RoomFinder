CREATE PROCEDURE [dbo].[OD4B_02_UsageMonthlyRollupOffice] AS
BEGIN
-- compiles the collection of results into Office separation

	with orgebusiness AS 
	(
	-- collate the ebusiness data with AD sync data
		SELECT 
			ISNULL(eba.OrgOffice, '-N/A-') AS OrgOffice, 
			eba.UserEmailAddress,
			CONVERT(INT, eba.UserIsContractor) AS UserIsContractor,
			CONVERT(INT, eba.UserEnabled) AS UserEnabled
		FROM
			dbo.eBusinessAccounts eba
			where eba.IsDeactivated = 0
	),

	-- grab statistics by the Office strings
	odbusersum AS 
	(
		SELECT 
			eba.OrgOffice AS OrgOffice, 
			convert(decimal(18,6),ISNULL(count(eba.UserEmailAddress),0)) TotalUsers, -- all users in the Office
			convert(decimal(18,6),ISNULL(sum(eba.UserEnabled),0)) TotalEnabledUsers,
			convert(decimal(18,6),ISNULL(SUM(CASE WHEN eba.UserIsContractor = 0 AND eba.UserEnabled = 1 THEN 1 ELSE 0 END), 0)) AS TotalEnabledEPAUsers,
			convert(decimal(18,6),ISNULL(SUM(CASE WHEN eba.UserIsContractor = 0 THEN 1 ELSE 0 END), 0)) AS TotalEPAUsers,
			convert(decimal(18,6),ISNULL(SUM(CASE WHEN eba.UserIsContractor = 1 AND eba.UserEnabled = 1 THEN 1 ELSE 0 END), 0)) AS TotalEnabledContractors,
			convert(decimal(18,6),ISNULL(SUM(CASE WHEN eba.UserIsContractor = 1 THEN 1 ELSE 0 END), 0)) AS TotalContractors
		FROM 
			orgebusiness eba
		group by eba.OrgOffice
	),

	-- build the individual metrics by user for a ReportingPeriod
	odbmetrics AS 
	(
		SELECT 
			odd.ID as ReportingID,
			odd.FormattedDate AS ReportingDate,
			odd.RequiresCalculation,
			pod.UPN,
			orgb.UserEnabled,
			orgb.UserIsContractor,
			orgb.OrgOffice,
			COUNT(pod.UPN) AS TotalActiveDays,
			CONVERT(VARCHAR(10), MAX(pod.LastActivityDate), 101) AS LastActivityDate,
			ISNULL(sum([ODB_TotalFileSynched]),0) + ISNULL(sum(ODB_TotalFileViewedModified),0)
				+ ISNULL(sum(ODB_CollaboratedByOthers),0) +ISNULL(sum(ODB_CollaboratedByOwner),0) 
				+ ISNULL(sum([ODB_TotalFileSharedEXT]),0) + ISNULL(sum([ODB_TotalFileSharedINT]),0) 
			AS [TotalofAllActivities],
			sum(ODB_TotalofAllActivities) AS [ODB_TotalofAllActivities],
			sum([ODB_TotalFileSynched]) AS [ODB_TotalFileSynched],
			sum(pod.ODB_TotalFileViewedModified) AS [ODB_TotalFileViewed_Modified],
			sum(ODB_CollaboratedByOthers) AS ODB_CollaboratedByOthers,
			sum(ODB_CollaboratedByOwner) AS ODB_CollaboratedByOwner,
			sum(ODB_TotalFileSharedEXT) AS ODB_TotalFileSharedEXT,
			sum(ODB_TotalFileSharedINT) AS ODB_TotalFileSharedINT,

			-- create calculations
			CASE when 
				ISNULL(sum([ODB_TotalFileSynched]),0) + ISNULL(sum(ODB_TotalFileViewedModified),0)
				+ ISNULL(sum(ODB_CollaboratedByOthers),0) +ISNULL(sum(ODB_CollaboratedByOwner),0) 
				+ ISNULL(sum([ODB_TotalFileSharedEXT]),0) + ISNULL(sum([ODB_TotalFileSharedINT]),0) > 0 then 1 else 0 end AS UsingOneDrive,

			case when sum(pod.ODB_TotalFileSharedEXT) + sum(pod.ODB_TotalFileSharedINT) > 0 then 1 else 0 end  AS UserIsSharing,
			case when sum(pod.ODB_TotalFileSynched) > 0 then 1 else 0 end AS UserIsSyncing

		FROM 
			[dbo].GraphOneDriveActivityDetail pod
			inner join dbo.O365PreviewOneDriveDates odd on pod.LastActivityDate between odd.DTSTART and odd.DTEND
			inner join orgebusiness orgb on pod.UPN = orgb.UserEmailAddress
		where 
			odd.RequiresCalculation = 1 
		group by 
			odd.ID, odd.FormattedDate, odd.RequiresCalculation, pod.UPN, orgb.OrgOffice, orgb.UserIsContractor, orgb.UserEnabled
	),

	odborgs AS (
	-- collapse the statistics for the rows (which indicate OneDrive usage)
		SELECT
			ods.ReportingID,
			ods.OrgOffice,
			SUM(CASE WHEN ods.UserEnabled = 1 THEN 1 ELSE 0 END) TotalActiveEnabledUsers,
			SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 0 THEN 1 ELSE 0 END) TotalActiveEnabledEPAUsers,
			SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 1 THEN 1 ELSE 0 END) TotalActiveEnabledContractorUsers,
			COUNT(ods.UPN) TotalActiveUsers
		FROM
			odbmetrics ods
		where 
			ods.RequiresCalculation = 1
		GROUP BY
			ods.ReportingID, ods.OrgOffice	
	),

	odbtotals AS (
		SELECT
			ods.ReportingID,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalUsers),0)) TotalUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledUsers),0)) AS TotalEnabledUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledEPAUsers),0)) AS TotalEnabledEPAUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledContractors),0)) AS TotalEnabledContractorsInPeriod
		FROM
			odborgs ods
			inner join odbusersum obs ON ods.OrgOffice = obs.OrgOffice
		GROUP BY
			ods.ReportingID
	),

	-- Calculate the rollup by Office in the ReportingPeriod
	odbrollup AS (
		SELECT 
			ods.ReportingID, ods.ReportingDate, ods.RequiresCalculation,
			ods.OrgOffice, 
			obs.TotalUsers, 
			obs.TotalEnabledUsers,
			obs.TotalEnabledEPAUsers,
			obs.TotalEnabledContractors,
			odt.TotalEnabledUsersInPeriod,
			ISNULL(obs.TotalEnabledUsers / NULLIF(odt.TotalEnabledUsersInPeriod,0), 0) AS TotalEnabledUsersInPeriodPerct,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.UsingOneDrive),0)) AS UsingOneDriveCount, 
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 THEN ods.UsingOneDrive ELSE 0 END),0)) AS UsingOneDriveEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 0 THEN ods.UsingOneDrive ELSE 0 END),0)) AS UsingOneDriveEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 1 THEN ods.UsingOneDrive ELSE 0 END),0)) AS UsingOneDriveEnabledContractorCount,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.UserIsSyncing),0)) AS SyncingOneDriveCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 then ods.UserIsSyncing ELSE 0 END),0)) AS SyncingOneDriveEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 0 then ods.UserIsSyncing ELSE 0 END),0)) AS SyncingOneDriveEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 1 then ods.UserIsSyncing ELSE 0 END),0)) AS SyncingOneDriveEnabledContractorCount,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.UserIsSharing),0)) AS SharingOneDriveCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 then ods.UserIsSharing ELSE 0 END),0)) AS SharingOneDriveEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 0 then ods.UserIsSharing ELSE 0 END),0)) AS SharingOneDriveEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 1 then ods.UserIsSharing ELSE 0 END),0)) AS SharingOneDriveEnabledContractorCount,
			-- rolling up numbers by the ORG
			AVG(ods.TotalActiveDays) TotalAvgActiveDays,
			sum(ods.ODB_TotalofAllActivities) AS ODB_TotalofAllActivities,
			sum(ods.ODB_TotalFileSynched) AS [ODB_TotalFileSynched],
			sum(ods.ODB_CollaboratedByOthers) AS ODB_CollaboratedByOthers,
			sum(ods.ODB_CollaboratedByOwner) AS ODB_CollaboratedByOwner,
			sum(ods.ODB_TotalFileSharedEXT) AS ODB_TotalFileSharedEXT,
			sum(ods.ODB_TotalFileSharedINT) AS ODB_TotalFileSharedINT,
			sum(ods.ODB_TotalFileViewed_Modified) AS [ODB_TotalFileViewed_Modified]
		from 
			odbmetrics ods
			inner join odbusersum obs ON ods.OrgOffice = obs.OrgOffice
			inner join odbtotals odt ON ods.ReportingID = odt.ReportingID
		where 
			ods.RequiresCalculation = 1
		group by 
			ods.OrgOffice, ods.ReportingID, ods.ReportingDate, ods.RequiresCalculation, obs.TotalUsers, obs.TotalEnabledUsers, obs.TotalEnabledEPAUsers, obs.TotalEnabledContractors, odt.TotalEnabledUsersInPeriod
	),

	odbrollupwithcalc AS (
		SELECT 
			ReportingID, ReportingDate, RequiresCalculation,
			OrgOffice, 
			TotalUsers, 
			TotalEnabledUsers,
			TotalEnabledEPAUsers,
			TotalEnabledContractors,
			TotalEnabledUsersInPeriod,
			TotalEnabledUsersInPeriodPerct,

			UsingOneDriveCount, 
			UsingOneDriveCount / NULLIF(TotalEnabledUsers,0) AS UsingOneDrivePerct,
			UsingOneDriveEnabledCount, 
			UsingOneDriveEnabledCount / NULLIF(TotalEnabledUsers,0) AS UsingOneDriveEnabledPerct,
			UsingOneDriveEnabledEPACount,
			UsingOneDriveEnabledEPACount / NULLIF(TotalEnabledEPAUsers,0) AS UsingOneDriveEnabledEPAPerct,
			UsingOneDriveEnabledContractorCount,
			UsingOneDriveEnabledContractorCount / NULLIF(TotalEnabledContractors,0) AS UsingOneDriveEnabledContractorPerct,

			SyncingOneDriveCount,
			SyncingOneDriveCount / NULLIF(TotalEnabledUsers,0) AS SyncingOneDrivePerct,
			SyncingOneDriveEnabledCount,
			SyncingOneDriveEnabledCount / NULLIF(TotalEnabledUsers,0) AS SyncingOneDriveEnabledPerct,
			SyncingOneDriveEnabledEPACount,
			SyncingOneDriveEnabledEPACount / NULLIF(TotalEnabledEPAUsers,0) AS SyncingOneDriveEnabledEPAPerct,
			SyncingOneDriveEnabledContractorCount,
			SyncingOneDriveEnabledContractorCount / NULLIF(TotalEnabledContractors,0) AS SyncingOneDriveEnabledContractorPerct,

			SharingOneDriveCount,
			SharingOneDriveCount / NULLIF(TotalEnabledUsers,0) AS SharingOneDrivePerct,
			SharingOneDriveEnabledCount,
			SharingOneDriveEnabledCount / NULLIF(TotalEnabledUsers,0) AS SharingOneDriveEnabledPerct,
			SharingOneDriveEnabledEPACount,
			SharingOneDriveEnabledEPACount / NULLIF(TotalEnabledEPAUsers,0) AS SharingOneDriveEnabledEPAPerct,
			SharingOneDriveEnabledContractorCount,
			SharingOneDriveEnabledContractorCount / NULLIF(TotalEnabledContractors,0) AS SharingOneDriveEnabledContractorPerct,

			TotalAvgActiveDays,
			ODB_TotalofAllActivities,
			ODB_TotalFileSynched,
			ODB_CollaboratedByOthers,
			ODB_CollaboratedByOwner,
			ODB_TotalFileSharedEXT,
			ODB_TotalFileSharedINT,
			ODB_TotalFileViewed_Modified
		FROM
			odbrollup
	)


	-- establish the target or destination table
	MERGE INTO [dbo].[O365PreviewOneDriveMonthlyRollup] AS odsm

	-- MERGE key/logic
	USING odbrollupwithcalc AS odb on odb.OrgOffice = odsm.Office and odb.ReportingID = odsm.ReportingDateID

	-- IF No Row, Add
	WHEN NOT MATCHED BY TARGET THEN
	INSERT ( 
		ReportingDateID, Office, TotalUsers, TotalActiveUsers, TotalActiveUsersPerct, TotalEmployeeUsers, TotalContractorUsers,
		UsingOneDriveCount, UsingOneDrivePerct, 
		UsingOneDriveEnabledCount, UsingOneDriveEnabledPerct,
		UsingOneDriveEmployeeCount, UsingOneDriveEmployeePerct,
		UsingOneDriveContractorCount, UsingOneDriveContractorPerct,
		SyncingOneDriveCount, SyncingOneDrivePerct,
		SyncingOneDriveEnabledCount, SyncingOneDriveEnabledPerct,
		SyncingOneDriveEmployeeCount, SyncingOneDriveEmployeePerct,
		SyncingOneDriveContractorCount, SyncingOneDriveContractorPerct,
		SharingOneDriveCount, SharingOneDrivePerct, 
		SharingOneDriveEnabledCount, SharingOneDriveEnabledPerct,
		SharingOneDriveEmployeeCount, SharingOneDriveEmployeePerct,
		SharingOneDriveContractorCount, SharingOneDriveContractorPerct,
		DTADDED, DTUPD
	)
	VALUES (
		ReportingID, OrgOffice, TotalUsers, TotalEnabledUsers, odb.TotalEnabledUsersInPeriodPerct, odb.TotalEnabledEPAUsers, odb.TotalEnabledContractors,
		odb.UsingOneDriveCount, UsingOneDrivePerct, 
		odb.UsingOneDriveEnabledCount, odb.UsingOneDriveEnabledPerct,
		odb.UsingOneDriveEnabledEPACount, odb.UsingOneDriveEnabledEPAPerct,
		odb.UsingOneDriveEnabledContractorCount, odb.UsingOneDriveEnabledContractorPerct,
		odb.SyncingOneDriveCount, odb.SyncingOneDrivePerct,
		odb.SyncingOneDriveEnabledCount, odb.SyncingOneDriveEnabledPerct,
		odb.SyncingOneDriveEnabledEPACount, odb.SyncingOneDriveEnabledEPAPerct,
		odb.SyncingOneDriveEnabledContractorCount, odb.SyncingOneDriveEnabledContractorPerct,
		odb.SharingOneDriveCount, odb.SharingOneDrivePerct,
		odb.SharingOneDriveEnabledCount, odb.SharingOneDriveEnabledPerct,
		odb.SharingOneDriveEnabledEPACount, odb.SharingOneDriveEnabledEPAPerct,
		odb.SharingOneDriveEnabledContractorCount, odb.SharingOneDriveEnabledContractorPerct,
		GETDATE(), GETDATE()
	)
	-- Update the Values
	WHEN MATCHED THEN
		UPDATE SET 
		odsm.TotalUsers = odb.TotalUsers,
		odsm.TotalActiveUsers = odb.TotalEnabledUsers,
		odsm.TotalActiveUsersPerct = odb.TotalEnabledUsersInPeriodPerct,
		odsm.TotalEmployeeUsers = odb.TotalEnabledEPAUsers, 
		odsm.TotalContractorUsers = odb.TotalEnabledContractors,

		odsm.UsingOneDriveCount = odb.UsingOneDriveCount,
		odsm.UsingOneDrivePerct = odb.UsingOneDrivePerct,
		odsm.UsingOneDriveEnabledCount = odb.UsingOneDriveEnabledCount,
		odsm.UsingOneDriveEnabledPerct = odb.UsingOneDriveEnabledPerct,
		odsm.UsingOneDriveEmployeeCount = odb.UsingOneDriveEnabledEPACount,
		odsm.UsingOneDriveEmployeePerct = odb.UsingOneDriveEnabledEPAPerct,
		odsm.UsingOneDriveContractorCount = odb.UsingOneDriveEnabledContractorCount,
		odsm.UsingOneDriveContractorPerct = odb.UsingOneDriveEnabledContractorPerct,

		odsm.SyncingOneDriveCount = odb.SyncingOneDriveCount,
		odsm.SyncingOneDrivePerct = odb.SyncingOneDrivePerct,
		odsm.SyncingOneDriveEnabledCount = odb.SyncingOneDriveEnabledCount,
		odsm.SyncingOneDriveEnabledPerct = odb.SyncingOneDriveEnabledPerct,
		odsm.SyncingOneDriveEmployeeCount = odb.SyncingOneDriveEnabledEPACount,
		odsm.SyncingOneDriveEmployeePerct = odb.SyncingOneDriveEnabledEPAPerct,
		odsm.SyncingOneDriveContractorCount = odb.SyncingOneDriveEnabledContractorCount,
		odsm.SyncingOneDriveContractorPerct = odb.SyncingOneDriveEnabledContractorPerct,

		odsm.SharingOneDriveCount = odb.SharingOneDriveCount,
		odsm.SharingOneDrivePerct = odb.SharingOneDrivePerct,
		odsm.SharingOneDriveEnabledCount = odb.SharingOneDriveEnabledCount,
		odsm.SharingOneDriveEnabledPerct = odb.SharingOneDriveEnabledPerct,
		odsm.SharingOneDriveEmployeeCount = odb.SharingOneDriveEnabledEPACount,
		odsm.SharingOneDriveEmployeePerct = odb.SharingOneDriveEnabledEPAPerct,
		odsm.SharingOneDriveContractorCount = odb.SharingOneDriveEnabledContractorCount,
		odsm.SharingOneDriveContractorPerct = odb.SharingOneDriveEnabledContractorPerct,
		odsm.DTUPD = GETDATE()

	-- Double Check the Query and Updates
	OUTPUT 
		'[dbo].[O365PreviewOneDriveMonthlyRollup]' AS TableName,
		ISNULL(inserted.ReportingDateID, deleted.ReportingDateID) as ReportingDateID, 
		ISNULL(inserted.Office, deleted.Office) as Office, 
		SUSER_SNAME() + ' ' +$action AS logAction,
		GETDATE() AS logDate
	INTO [dbo].[O365PreviewOneDrive_DataBuildLog] (TableName, ReportingDateID, Office, logAction, logDate)
	;

END
GO


