CREATE PROCEDURE [dbo].[MSTeams_02_UsageMonthlyRollupOffice] AS
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
	teamsusersum AS 
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
	teamsmetrics AS 
	(
		SELECT 
			otd.ID as ReportingID,
			otd.FormattedDate AS ReportingDate,
			otd.RequiresCalculation,
			gtaud.UPN,
			orgb.UserEnabled,
			orgb.UserIsContractor,
			orgb.OrgOffice,
			COUNT(gtaud.UPN) AS TotalActiveDays,
			CONVERT(VARCHAR(10), MAX(gtaud.LastActivityDate), 101) AS LastActivityDate,
			ISNULL(sum(gtaud.[TeamChatMessageCount]),0) 
				+ ISNULL(sum(gtaud.[PrivateChatMessageCount]),0)
				+ ISNULL(sum(gtaud.[CallCount]),0) 
				+ ISNULL(sum(gtaud.[MeetingCount]),0) 
			AS [TotalofAllActivities],

			ISNULL(sum([TeamChatMessageCount]),0) TotalTeamChatMessageCount,
			ISNULL(sum([PrivateChatMessageCount]),0) TotalPrivateChatMessageCount,
			ISNULL(sum([CallCount]),0) TotalCallCount,
			ISNULL(sum([MeetingCount]),0) TotalMeetingCount
		FROM 
			[dbo].[GraphTeamsActivityUserDetail] gtaud
			inner join dbo.O365PreviewTeamsDates otd on gtaud.LastActivityDate between otd.DTSTART and otd.DTEND
			inner join orgebusiness orgb on gtaud.UPN = orgb.UserEmailAddress
		where 
			otd.RequiresCalculation = 1 
		group by 
			otd.ID, otd.FormattedDate, otd.RequiresCalculation, gtaud.UPN, orgb.OrgOffice, orgb.UserIsContractor, orgb.UserEnabled
	),

	teamsorgs AS (
	-- collapse the statistics for the rows (which indicate OneDrive usage)
		SELECT
			ods.ReportingID,
			ods.OrgOffice,
			SUM(CASE WHEN ods.UserEnabled = 1 THEN 1 ELSE 0 END) TotalActiveEnabledUsers,
			SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 0 THEN 1 ELSE 0 END) TotalActiveEnabledEPAUsers,
			SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 1 THEN 1 ELSE 0 END) TotalActiveEnabledContractorUsers,
			COUNT(ods.UPN) TotalActiveUsers
		FROM
			teamsmetrics ods
		where 
			ods.RequiresCalculation = 1
		GROUP BY
			ods.ReportingID, ods.OrgOffice	
	),

	teamstotals AS (
		SELECT
			ods.ReportingID,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalUsers),0)) TotalUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledUsers),0)) AS TotalEnabledUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledEPAUsers),0)) AS TotalEnabledEPAUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledContractors),0)) AS TotalEnabledContractorsInPeriod
		FROM
			teamsorgs ods
			inner join teamsusersum obs ON ods.OrgOffice = obs.OrgOffice
		GROUP BY
			ods.ReportingID
	),

	-- Calculate the rollup by Office in the ReportingPeriod
	teamsRollup AS (
		SELECT 
			ods.ReportingID, ods.ReportingDate, ods.RequiresCalculation,
			ods.OrgOffice, 
			obs.TotalUsers, 
			obs.TotalEnabledUsers,
			obs.TotalEnabledEPAUsers,
			obs.TotalEnabledContractors,
			odt.TotalEnabledUsersInPeriod,
			ISNULL(obs.TotalEnabledUsers / odt.TotalEnabledUsersInPeriod, 0) AS TotalEnabledUsersInPeriodPerct,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.TotalTeamChatMessageCount),0)) AS TotalTeamChatMessageCountCount, 
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 THEN ods.TotalTeamChatMessageCount ELSE 0 END),0)) AS TotalTeamChatMessageCountEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 0 THEN ods.TotalTeamChatMessageCount ELSE 0 END),0)) AS TotalTeamChatMessageCountEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 1 THEN ods.TotalTeamChatMessageCount ELSE 0 END),0)) AS TotalTeamChatMessageCountEnabledContractorCount,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.TotalPrivateChatMessageCount),0)) AS TotalPrivateChatMessageCountCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 then ods.TotalPrivateChatMessageCount ELSE 0 END),0)) AS TotalPrivateChatMessageCountEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 0 then ods.TotalPrivateChatMessageCount ELSE 0 END),0)) AS TotalPrivateChatMessageCountEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 1 then ods.TotalPrivateChatMessageCount ELSE 0 END),0)) AS TotalPrivateChatMessageCountEnabledContractorCount,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.TotalCallCount),0)) AS TotalCallCountCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 then ods.TotalCallCount ELSE 0 END),0)) AS TotalCallCountEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 0 then ods.TotalCallCount ELSE 0 END),0)) AS TotalCallCountEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 1 then ods.TotalCallCount ELSE 0 END),0)) AS TotalCallCountEnabledContractorCount,

			--Organizer or Participant
			CONVERT(decimal(18,6), ISNULL(SUM(ods.TotalMeetingCount),0)) AS TotalMeetingCountCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 then ods.TotalMeetingCount ELSE 0 END),0)) AS TotalMeetingCountEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 0 then ods.TotalMeetingCount ELSE 0 END),0)) AS TotalMeetingCountEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 1 then ods.TotalMeetingCount ELSE 0 END),0)) AS TotalMeetingCountEnabledContractorCount,
			
			-- rolling up numbers by the ORG
			AVG(ods.TotalActiveDays) TotalAvgActiveDays,
			sum(ods.[TotalofAllActivities]) AS [TotalofAllActivities],
			sum(ods.TotalTeamChatMessageCount) AS ORGTotalTeamChatMessageCount,
			sum(ods.TotalPrivateChatMessageCount) AS ORGTotalPrivateChatMessageCount,
			sum(ods.TotalCallCount) AS ORGTotalCallCount,
			sum(ods.TotalMeetingCount) AS ORGTotalMeetingCount
		from 
			teamsmetrics ods
			inner join teamsusersum obs ON ods.OrgOffice = obs.OrgOffice
			inner join teamstotals odt ON ods.ReportingID = odt.ReportingID
		where 
			ods.RequiresCalculation = 1
		group by 
			ods.OrgOffice, ods.ReportingID, ods.ReportingDate, ods.RequiresCalculation, obs.TotalUsers, obs.TotalEnabledUsers, obs.TotalEnabledEPAUsers, obs.TotalEnabledContractors, odt.TotalEnabledUsersInPeriod
	),

	teamsRollupwithcalc AS (
		SELECT 
			ReportingID, ReportingDate, RequiresCalculation,
			OrgOffice, 
			TotalUsers, 
			TotalEnabledUsers,
			TotalEnabledEPAUsers,
			TotalEnabledContractors,
			TotalEnabledUsersInPeriod,
			TotalEnabledUsersInPeriodPerct,

			TotalTeamChatMessageCountCount, 
			TotalTeamChatMessageCountEnabledCount, 
			TotalTeamChatMessageCountEnabledEPACount,
			TotalTeamChatMessageCountEnabledContractorCount,

			TotalPrivateChatMessageCountCount,
			TotalPrivateChatMessageCountEnabledCount,
			TotalPrivateChatMessageCountEnabledEPACount,
			TotalPrivateChatMessageCountEnabledContractorCount,

			TotalCallCountCount,
			TotalCallCountEnabledCount,
			TotalCallCountEnabledEPACount,
			TotalCallCountEnabledContractorCount,

			--Organizer or Participant
			TotalMeetingCountCount,
			TotalMeetingCountEnabledCount,
			TotalMeetingCountEnabledEPACount,
			TotalMeetingCountEnabledContractorCount,

			TotalAvgActiveDays,
			TotalofAllActivities,
			ORGTotalTeamChatMessageCount,
			ORGTotalPrivateChatMessageCount,
			ORGTotalCallCount,
			ORGTotalMeetingCount
		FROM
			teamsRollup
	)

	--select * from teamsRollupwithcalc

	-- establish the target or destination table
	MERGE INTO [dbo].[O365PreviewTeamsMonthlyRollup] AS odsm

	-- MERGE key/logic
	USING teamsRollupwithcalc AS odb on odb.OrgOffice = odsm.Office and odb.ReportingID = odsm.ReportingDateID

	-- IF No Row, Add
	WHEN NOT MATCHED BY TARGET THEN
	INSERT ( 
		ReportingDateID, Office, TotalUsers, TotalActiveUsers, TotalEmployeeUsers, TotalContractorUsers,
		TotalTeamChatMessageCountCount, 
		TotalTeamChatMessageCountEnabledCount, 
		TotalTeamChatMessageCountEmployeeCount, 
		TotalTeamChatMessageCountContractorCount, 
		TotalPrivateChatMessageCountCount, 
		TotalPrivateChatMessageCountEnabledCount, 
		TotalPrivateChatMessageCountEmployeeCount, 
		TotalPrivateChatMessageCountContractorCount, 
		TotalCallCountCount,  
		TotalCallCountEnabledCount, 
		TotalCallCountEmployeeCount, 
		TotalCallCountContractorCount, 
		TotalMeetingCountCount,
		TotalMeetingCountEnabledCount,
		TotalMeetingCountEmployeeCount,
		TotalMeetingCountContractorCount,
		TotalAvgActiveDays,
		TotalofAllActivities,
		ORGTotalTeamChatMessageCount,
		ORGTotalPrivateChatMessageCount,
		ORGTotalCallCount,
		ORGTotalMeetingCount,
		DTADDED, DTUPD
	)
	VALUES (
		ReportingID, OrgOffice, TotalUsers, TotalEnabledUsers, odb.TotalEnabledEPAUsers, odb.TotalEnabledContractors,
		odb.TotalTeamChatMessageCountCount, 
		odb.TotalTeamChatMessageCountEnabledCount, 
		odb.TotalTeamChatMessageCountEnabledEPACount,
		odb.TotalTeamChatMessageCountEnabledContractorCount, 
		odb.TotalPrivateChatMessageCountCount, 
		odb.TotalPrivateChatMessageCountEnabledCount, 
		odb.TotalPrivateChatMessageCountEnabledEPACount, 
		odb.TotalPrivateChatMessageCountEnabledContractorCount, 
		odb.TotalCallCountCount, 
		odb.TotalCallCountEnabledCount, 
		odb.TotalCallCountEnabledEPACount, 
		odb.TotalCallCountEnabledContractorCount, 
		odb.TotalMeetingCountCount,
		odb.TotalMeetingCountEnabledCount,
		odb.TotalMeetingCountEnabledEPACount,
		odb.TotalMeetingCountEnabledContractorCount,
		odb.TotalAvgActiveDays,
		odb.TotalofAllActivities,
		odb.ORGTotalTeamChatMessageCount,
		odb.ORGTotalPrivateChatMessageCount,
		odb.ORGTotalCallCount,
		odb.ORGTotalMeetingCount,
		GETDATE(), GETDATE()
	)
	-- Update the Values
	WHEN MATCHED THEN
		UPDATE SET 
		odsm.TotalUsers = odb.TotalUsers,
		odsm.TotalActiveUsers = odb.TotalEnabledUsers,
		odsm.TotalEmployeeUsers = odb.TotalEnabledEPAUsers, 
		odsm.TotalContractorUsers = odb.TotalEnabledContractors,

		odsm.TotalTeamChatMessageCountCount = odb.TotalTeamChatMessageCountCount,
		odsm.TotalTeamChatMessageCountEnabledCount = odb.TotalTeamChatMessageCountEnabledCount,
		odsm.TotalTeamChatMessageCountEmployeeCount = odb.TotalTeamChatMessageCountEnabledEPACount,
		odsm.TotalTeamChatMessageCountContractorCount = odb.TotalTeamChatMessageCountEnabledContractorCount,

		odsm.TotalPrivateChatMessageCountCount = odb.TotalPrivateChatMessageCountCount,
		odsm.TotalPrivateChatMessageCountEnabledCount = odb.TotalPrivateChatMessageCountEnabledCount,
		odsm.TotalPrivateChatMessageCountEmployeeCount = odb.TotalPrivateChatMessageCountEnabledEPACount,
		odsm.TotalPrivateChatMessageCountContractorCount = odb.TotalPrivateChatMessageCountEnabledContractorCount,

		odsm.TotalCallCountCount = odb.TotalCallCountCount,
		odsm.TotalCallCountEnabledCount = odb.TotalCallCountEnabledCount,
		odsm.TotalCallCountEmployeeCount = odb.TotalCallCountEnabledEPACount,
		odsm.TotalCallCountContractorCount = odb.TotalCallCountEnabledContractorCount,

		odsm.TotalMeetingCountCount = odb.TotalMeetingCountCount,
		odsm.TotalMeetingCountEnabledCount = odb.TotalMeetingCountEnabledCount,
		odsm.TotalMeetingCountEmployeeCount = odb.TotalMeetingCountEnabledEPACount,
		odsm.TotalMeetingCountContractorCount = odb.TotalMeetingCountEnabledContractorCount,

		odsm.TotalAvgActiveDays = odb.TotalAvgActiveDays,
		odsm.TotalofAllActivities = odb.TotalofAllActivities,
		odsm.ORGTotalTeamChatMessageCount = odb.ORGTotalTeamChatMessageCount,
		odsm.ORGTotalPrivateChatMessageCount = odb.ORGTotalPrivateChatMessageCount,
		odsm.ORGTotalCallCount = odb.ORGTotalCallCount,
		odsm.ORGTotalMeetingCount = odb.ORGTotalMeetingCount,
		odsm.DTUPD = GETDATE()

	-- Double Check the Query and Updates
	OUTPUT 
		'[dbo].[O365PreviewTeamsMonthlyRollup]' AS TableName,
		ISNULL(inserted.ReportingDateID, deleted.ReportingDateID) as ReportingDateID, 
		ISNULL(inserted.Office, deleted.Office) as Office, 
		SUSER_SNAME() + ' ' +$action AS logAction,
		GETDATE() AS logDate
	INTO [dbo].[O365PreviewTeams_DataBuildLog] (TableName, ReportingDateID, Office, logAction, logDate)
	;

END
GO


