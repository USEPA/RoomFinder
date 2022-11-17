CREATE PROCEDURE [dbo].[Skype_02_UsageMonthlyRollupOffice] AS
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
	skypeusersum AS 
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
	skypemetrics AS 
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
			ISNULL(sum(pod.[TotalPeerToPeerSessionCount]),0) 
				+ ISNULL(sum(pod.[TotalOrganizedConferenceCount]),0)
				+ ISNULL(sum(pod.[TotalParticipatedConferenceCount]),0) 
			AS [TotalofAllActivities],

			sum(pod.[TotalPeerToPeerSessionCount]) AS [TotalPeerToPeerSessionCount],			
			ISNULL(sum([PeerToPeerIMCount]),0) PeerToPeerIMCount,
			ISNULL(sum([PeerToPeerAudioMinutes]),0) PeerToPeerAudioMinutes,
			ISNULL(sum([PeerToPeerAudioCount]),0) AS PeerToPeerAudioCount,
			ISNULL(sum([PeerToPeerVideoCount]),0) AS PeerToPeerVideoCount,
			ISNULL(sum([PeerToPeerVideoMinutes]),0) PeerToPeerVideoMinutes,
			CASE when ISNULL(sum([TotalPeerToPeerSessionCount]),0) > 0 then 1 else 0 end AS UsingPeerToPeer,

			ISNULL(sum([TotalOrganizedConferenceCount]), 0) AS [TotalOrganizedConferenceCount],
			ISNULL(sum([OrganizedConferenceIMCount]), 0) AS [OrganizedConferenceIMCount],
			ISNULL(sum([OrganizedConferenceAudioVideoCount]), 0) AS [OrganizedConferenceAudioVideoCount],
			ISNULL(sum([OrganizedConferenceAudioVideoMinutes]), 0) AS [OrganizedConferenceAudioVideoMinutes],
			ISNULL(sum([OrganizedConferenceAppSharingCount]), 0) AS [OrganizedConferenceAppSharingCount],
			ISNULL(sum([OrganizedConferenceWebCount]), 0) AS [OrganizedConferenceWebCount],
			ISNULL(sum([OrganizedConferenceDialInOut3rdPartyCount]), 0) AS [OrganizedConferenceDialInOut3rdPartyCount],
			ISNULL(sum([OrganizedConferenceCloudDialInOutMicrosoftCount]), 0) AS [OrganizedConferenceCloudDialInOutMicrosoftCount],
			ISNULL(sum([OrganizedConferenceCloudDialInMicrosoftMinutes]), 0) AS [OrganizedConferenceCloudDialInMicrosoftMinutes],
			ISNULL(sum([OrganizedConferenceCloudDialOutMicrosoftMinutes]), 0) AS [OrganizedConferenceCloudDialOutMicrosoftMinutes],
			max([OrganizedConferenceLastActivityDate]) AS [OrganizedConferenceLastActivityDate],
			CASE when ISNULL(sum([TotalOrganizedConferenceCount]),0) > 0 then 1 else 0 end AS UsingOrganizedConference,

			ISNULL(sum([TotalParticipatedConferenceCount]), 0) AS [TotalParticipatedConferenceCount],
			ISNULL(sum([ParticipatedConferenceIMCount]), 0) AS [ParticipatedConferenceIMCount],
			ISNULL(sum([ParticipatedConferenceAudioVideoCount]), 0) AS [ParticipatedConferenceAudioVideoCount],
			ISNULL(sum([ParticipatedConferenceAudioVideoMinutes]), 0) AS [ParticipatedConferenceAudioVideoMinutes],
			ISNULL(sum([ParticipatedConferenceAppSharingCount]), 0) AS [ParticipatedConferenceAppSharingCount],
			ISNULL(sum([ParticipatedConferenceWebCount]), 0) AS [ParticipatedConferenceWebCount],
			ISNULL(sum([ParticipatedConferenceDialInOut3rdPartyCount]), 0) AS [ParticipatedConferenceDialInOut3rdPartyCount],
			max([ParticipatedConferenceLastActivityDate]) AS [ParticipatedConferenceLastActivityDate],
			CASE when ISNULL(sum([TotalParticipatedConferenceCount]),0) > 0 then 1 else 0 end AS UsingParticipated,

			--Organizer or Participant
			CASE WHEN ((ISNULL(sum([TotalOrganizedConferenceCount]),0) > 0) OR (ISNULL(sum([TotalParticipatedConferenceCount]),0) > 0)) THEN 1 ELSE 0 END AS UsingOrganizedOrParticipated

		FROM 
			[dbo].[GraphSkypeForBusinessActivityUserDetail] pod
			inner join dbo.O365PreviewSkypeDates odd on pod.LastActivityDate between odd.DTSTART and odd.DTEND
			inner join orgebusiness orgb on pod.UPN = orgb.UserEmailAddress
		where 
			odd.RequiresCalculation = 1 
		group by 
			odd.ID, odd.FormattedDate, odd.RequiresCalculation, pod.UPN, orgb.OrgOffice, orgb.UserIsContractor, orgb.UserEnabled
	),

	skypeorgs AS (
	-- collapse the statistics for the rows (which indicate OneDrive usage)
		SELECT
			ods.ReportingID,
			ods.OrgOffice,
			SUM(CASE WHEN ods.UserEnabled = 1 THEN 1 ELSE 0 END) TotalActiveEnabledUsers,
			SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 0 THEN 1 ELSE 0 END) TotalActiveEnabledEPAUsers,
			SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 1 THEN 1 ELSE 0 END) TotalActiveEnabledContractorUsers,
			COUNT(ods.UPN) TotalActiveUsers
		FROM
			skypemetrics ods
		where 
			ods.RequiresCalculation = 1
		GROUP BY
			ods.ReportingID, ods.OrgOffice	
	),

	skypetotals AS (
		SELECT
			ods.ReportingID,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalUsers),0)) TotalUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledUsers),0)) AS TotalEnabledUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledEPAUsers),0)) AS TotalEnabledEPAUsersInPeriod,
			convert(decimal(18,6),ISNULL(SUM(obs.TotalEnabledContractors),0)) AS TotalEnabledContractorsInPeriod
		FROM
			skypeorgs ods
			inner join skypeusersum obs ON ods.OrgOffice = obs.OrgOffice
		GROUP BY
			ods.ReportingID
	),

	-- Calculate the rollup by Office in the ReportingPeriod
	skypeRollup AS (
		SELECT 
			ods.ReportingID, ods.ReportingDate, ods.RequiresCalculation,
			ods.OrgOffice, 
			obs.TotalUsers, 
			obs.TotalEnabledUsers,
			obs.TotalEnabledEPAUsers,
			obs.TotalEnabledContractors,
			odt.TotalEnabledUsersInPeriod,
			ISNULL(obs.TotalEnabledUsers / odt.TotalEnabledUsersInPeriod, 0) AS TotalEnabledUsersInPeriodPerct,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.UsingPeerToPeer),0)) AS UsingPeerToPeerCount, 
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 THEN ods.UsingPeerToPeer ELSE 0 END),0)) AS UsingPeerToPeerEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 0 THEN ods.UsingPeerToPeer ELSE 0 END),0)) AS UsingPeerToPeerEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(CASE WHEN ods.UserEnabled = 1 AND ods.UserIsContractor = 1 THEN ods.UsingPeerToPeer ELSE 0 END),0)) AS UsingPeerToPeerEnabledContractorCount,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.UsingOrganizedConference),0)) AS UsingOrganizedCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 then ods.UsingOrganizedConference ELSE 0 END),0)) AS UsingOrganizedEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 0 then ods.UsingOrganizedConference ELSE 0 END),0)) AS UsingOrganizedEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 1 then ods.UsingOrganizedConference ELSE 0 END),0)) AS UsingOrganizedEnabledContractorCount,

			CONVERT(decimal(18,6), ISNULL(SUM(ods.UsingParticipated),0)) AS UsingParticipatedCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 then ods.UsingParticipated ELSE 0 END),0)) AS UsingParticipatedEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 0 then ods.UsingParticipated ELSE 0 END),0)) AS UsingParticipatedEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 1 then ods.UsingParticipated ELSE 0 END),0)) AS UsingParticipatedEnabledContractorCount,

			--Organizer or Participant
			CONVERT(decimal(18,6), ISNULL(SUM(ods.UsingOrganizedOrParticipated),0)) AS UsingOrganizedOrParticipatedCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 then ods.UsingOrganizedOrParticipated ELSE 0 END),0)) AS UsingOrganizedOrParticipatedEnabledCount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 0 then ods.UsingOrganizedOrParticipated ELSE 0 END),0)) AS UsingOrganizedOrParticipatedEnabledEPACount,
			CONVERT(decimal(18,6), ISNULL(SUM(case when ods.UserEnabled = 1 AND ods.UserIsContractor = 1 then ods.UsingOrganizedOrParticipated ELSE 0 END),0)) AS UsingOrganizedOrParticipatedEnabledContractorCount,
			
			-- rolling up numbers by the ORG
			AVG(ods.TotalActiveDays) TotalAvgActiveDays,
			sum(ods.[TotalofAllActivities]) AS [TotalofAllActivities],
			sum(ods.TotalPeerToPeerSessionCount) AS TotalPeerToPeerSessionCount,
			sum(ods.TotalOrganizedConferenceCount) AS TotalOrganizedConferenceCount,
			sum(ods.TotalParticipatedConferenceCount) AS TotalParticipatedConferenceCount,
			sum(ods.PeerToPeerAudioMinutes) AS PeerToPeerAudioMinutes,
			sum(ods.PeerToPeerVideoMinutes) AS PeerToPeerVideoMinutes,
			sum(ods.PeerToPeerIMCount) AS PeerToPeerIMCount,

			ISNULL(sum([OrganizedConferenceIMCount]), 0) AS [OrganizedConferenceIMCount],
			ISNULL(sum([OrganizedConferenceAudioVideoCount]), 0) AS [OrganizedConferenceAudioVideoCount],
			ISNULL(sum([OrganizedConferenceAudioVideoMinutes]), 0) AS [OrganizedConferenceAudioVideoMinutes],

			ISNULL(sum([ParticipatedConferenceIMCount]), 0) AS [ParticipatedConferenceIMCount],
			ISNULL(sum([ParticipatedConferenceAudioVideoCount]), 0) AS [ParticipatedConferenceAudioVideoCount],
			ISNULL(sum([ParticipatedConferenceAudioVideoMinutes]), 0) AS [ParticipatedConferenceAudioVideoMinutes]
		from 
			skypemetrics ods
			inner join skypeusersum obs ON ods.OrgOffice = obs.OrgOffice
			inner join skypetotals odt ON ods.ReportingID = odt.ReportingID
		where 
			ods.RequiresCalculation = 1
		group by 
			ods.OrgOffice, ods.ReportingID, ods.ReportingDate, ods.RequiresCalculation, obs.TotalUsers, obs.TotalEnabledUsers, obs.TotalEnabledEPAUsers, obs.TotalEnabledContractors, odt.TotalEnabledUsersInPeriod
	),

	skypeRollupwithcalc AS (
		SELECT 
			ReportingID, ReportingDate, RequiresCalculation,
			OrgOffice, 
			TotalUsers, 
			TotalEnabledUsers,
			TotalEnabledEPAUsers,
			TotalEnabledContractors,
			TotalEnabledUsersInPeriod,
			TotalEnabledUsersInPeriodPerct,

			UsingPeerToPeerCount, 
			UsingPeerToPeerEnabledCount, 
			UsingPeerToPeerEnabledEPACount,
			UsingPeerToPeerEnabledContractorCount,

			UsingOrganizedCount,
			UsingOrganizedEnabledCount,
			UsingOrganizedEnabledEPACount,
			UsingOrganizedEnabledContractorCount,

			UsingParticipatedCount,
			UsingParticipatedEnabledCount,
			UsingParticipatedEnabledEPACount,
			UsingParticipatedEnabledContractorCount,

			--Organizer or Participant
			UsingOrganizedOrParticipatedCount,
			UsingOrganizedOrParticipatedEnabledCount,
			UsingOrganizedOrParticipatedEnabledEPACount,
			UsingOrganizedOrParticipatedEnabledContractorCount,

			TotalAvgActiveDays,
			TotalofAllActivities,
			TotalPeerToPeerSessionCount,
			TotalOrganizedConferenceCount,
			TotalParticipatedConferenceCount,
			PeerToPeerAudioMinutes,
			PeerToPeerVideoMinutes,
			PeerToPeerIMCount,
			[OrganizedConferenceIMCount],
			[OrganizedConferenceAudioVideoCount],
			[OrganizedConferenceAudioVideoMinutes],
			[ParticipatedConferenceIMCount],
			[ParticipatedConferenceAudioVideoCount],
			[ParticipatedConferenceAudioVideoMinutes]
		FROM
			skypeRollup
	)

	--select * from skypeRollupwithcalc

	-- establish the target or destination table
	MERGE INTO [dbo].[O365PreviewSkypeMonthlyRollup] AS odsm

	-- MERGE key/logic
	USING skypeRollupwithcalc AS odb on odb.OrgOffice = odsm.Office and odb.ReportingID = odsm.ReportingDateID

	-- IF No Row, Add
	WHEN NOT MATCHED BY TARGET THEN
	INSERT ( 
		ReportingDateID, Office, TotalUsers, TotalActiveUsers, TotalEmployeeUsers, TotalContractorUsers,
		UsingPeerToPeerCount, 
		UsingPeerToPeerEnabledCount, 
		UsingPeerToPeerEmployeeCount, 
		UsingPeerToPeerContractorCount, 
		UsingOrganizedCount, 
		UsingOrganizedEnabledCount, 
		UsingOrganizedEmployeeCount, 
		UsingOrganizedContractorCount, 
		UsingParticipatedCount,  
		UsingParticipatedEnabledCount, 
		UsingParticipatedEmployeeCount, 
		UsingParticipatedContractorCount, 
		UsingOrganizedOrParticipatedCount,
		UsingOrganizedOrParticipatedEnabledCount,
		UsingOrganizedOrParticipatedEmployeeCount,
		UsingOrganizedOrParticipatedContractorCount,
		TotalAvgActiveDays,
		TotalofAllActivities,
		TotalPeerToPeerSessionCount,
		TotalOrganizedConferenceCount,
		TotalParticipatedConferenceCount,
		PeerToPeerAudioMinutes,
		PeerToPeerVideoMinutes,
		PeerToPeerIMCount,
		[OrganizedIMCount],
		[OrganizedAudioCount],
		[OrganizedAudioMinutes],
		[ParticipatedIMCount],
		[ParticipatedAudioCount],
		[ParticipatedAudioMinutes],
		DTADDED, DTUPD
	)
	VALUES (
		ReportingID, OrgOffice, TotalUsers, TotalEnabledUsers, odb.TotalEnabledEPAUsers, odb.TotalEnabledContractors,
		odb.UsingPeerToPeerCount, 
		odb.UsingPeerToPeerEnabledCount, 
		odb.UsingPeerToPeerEnabledEPACount,
		odb.UsingPeerToPeerEnabledContractorCount, 
		odb.UsingOrganizedCount, 
		odb.UsingOrganizedEnabledCount, 
		odb.UsingOrganizedEnabledEPACount, 
		odb.UsingOrganizedEnabledContractorCount, 
		odb.UsingParticipatedCount, 
		odb.UsingParticipatedEnabledCount, 
		odb.UsingParticipatedEnabledEPACount, 
		odb.UsingParticipatedEnabledContractorCount, 
		odb.UsingOrganizedOrParticipatedCount,
		odb.UsingOrganizedOrParticipatedEnabledCount,
		odb.UsingOrganizedOrParticipatedEnabledEPACount,
		odb.UsingOrganizedOrParticipatedEnabledContractorCount,
		odb.TotalAvgActiveDays,
		odb.TotalofAllActivities,
		odb.TotalPeerToPeerSessionCount,
		odb.TotalOrganizedConferenceCount,
		odb.TotalParticipatedConferenceCount,
		odb.PeerToPeerAudioMinutes,
		odb.PeerToPeerVideoMinutes,
		odb.PeerToPeerIMCount,
		[OrganizedConferenceIMCount],
		[OrganizedConferenceAudioVideoCount],
		[OrganizedConferenceAudioVideoMinutes],
		[ParticipatedConferenceIMCount],
		[ParticipatedConferenceAudioVideoCount],
		[ParticipatedConferenceAudioVideoMinutes],
		GETDATE(), GETDATE()
	)
	-- Update the Values
	WHEN MATCHED THEN
		UPDATE SET 
		odsm.TotalUsers = odb.TotalUsers,
		odsm.TotalActiveUsers = odb.TotalEnabledUsers,
		odsm.TotalEmployeeUsers = odb.TotalEnabledEPAUsers, 
		odsm.TotalContractorUsers = odb.TotalEnabledContractors,

		odsm.UsingPeerToPeerCount = odb.UsingPeerToPeerCount,
		odsm.UsingPeerToPeerEnabledCount = odb.UsingPeerToPeerEnabledCount,
		odsm.UsingPeerToPeerEmployeeCount = odb.UsingPeerToPeerEnabledEPACount,
		odsm.UsingPeerToPeerContractorCount = odb.UsingPeerToPeerEnabledContractorCount,

		odsm.UsingOrganizedCount = odb.UsingOrganizedCount,
		odsm.UsingOrganizedEnabledCount = odb.UsingOrganizedEnabledCount,
		odsm.UsingOrganizedEmployeeCount = odb.UsingOrganizedEnabledEPACount,
		odsm.UsingOrganizedContractorCount = odb.UsingOrganizedEnabledContractorCount,

		odsm.UsingParticipatedCount = odb.UsingParticipatedCount,
		odsm.UsingParticipatedEnabledCount = odb.UsingParticipatedEnabledCount,
		odsm.UsingParticipatedEmployeeCount = odb.UsingParticipatedEnabledEPACount,
		odsm.UsingParticipatedContractorCount = odb.UsingParticipatedEnabledContractorCount,

		odsm.UsingOrganizedOrParticipatedCount = odb.UsingOrganizedOrParticipatedCount,
		odsm.UsingOrganizedOrParticipatedEnabledCount = odb.UsingOrganizedOrParticipatedEnabledCount,
		odsm.UsingOrganizedOrParticipatedEmployeeCount = odb.UsingOrganizedOrParticipatedEnabledEPACount,
		odsm.UsingOrganizedOrParticipatedContractorCount = odb.UsingOrganizedOrParticipatedEnabledContractorCount,

		odsm.TotalAvgActiveDays = odb.TotalAvgActiveDays,
		odsm.TotalofAllActivities = odb.TotalofAllActivities,
		odsm.TotalPeerToPeerSessionCount = odb.TotalPeerToPeerSessionCount,
		odsm.TotalOrganizedConferenceCount = odb.TotalOrganizedConferenceCount,
		odsm.TotalParticipatedConferenceCount = odb.TotalParticipatedConferenceCount,
		odsm.PeerToPeerAudioMinutes = odb.PeerToPeerAudioMinutes,
		odsm.PeerToPeerVideoMinutes = odb.PeerToPeerVideoMinutes,
		odsm.PeerToPeerIMCount = odb.PeerToPeerIMCount, 
		[OrganizedIMCount]=[OrganizedConferenceIMCount],
		[OrganizedAudioCount]=[OrganizedConferenceAudioVideoCount],
		[OrganizedAudioMinutes]=[OrganizedConferenceAudioVideoMinutes],
		[ParticipatedIMCount]=[ParticipatedConferenceIMCount],
		[ParticipatedAudioCount]=[ParticipatedConferenceAudioVideoCount],
		[ParticipatedAudioMinutes]=[ParticipatedConferenceAudioVideoMinutes],
		odsm.DTUPD = GETDATE()

	-- Double Check the Query and Updates
	OUTPUT 
		'[dbo].[O365PreviewSkypeMonthlyRollup]' AS TableName,
		ISNULL(inserted.ReportingDateID, deleted.ReportingDateID) as ReportingDateID, 
		ISNULL(inserted.Office, deleted.Office) as Office, 
		SUSER_SNAME() + ' ' +$action AS logAction,
		GETDATE() AS logDate
	INTO [dbo].[O365PreviewSkype_DataBuildLog] (TableName, ReportingDateID, Office, logAction, logDate)
	;

END