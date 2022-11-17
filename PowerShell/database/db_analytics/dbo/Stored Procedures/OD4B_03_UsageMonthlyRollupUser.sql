CREATE PROCEDURE [OD4B_03_UsageMonthlyRollupUser] AS
BEGIN
-- compiles the collection of results into Office separation


	WITH TotalUserDetail AS (
	SELECT 
		odd.ID ReportingID,
		odd.FormattedDate AS ReportingDate,
		max(pod.LastActivityDate) AS LastActivityDate,
		pod.UPN
		,COUNT(pod.UPN) AS TotalActiveDays
		,ISNULL(sum(ODB_CollaboratedByOthers),0) +ISNULL(sum(ODB_CollaboratedByOwner),0) 
			+ ISNULL(sum([ODB_TotalFileSharedEXT]),0) + ISNULL(sum([ODB_TotalFileSharedINT]),0) 
			+ ISNULL(sum([ODB_TotalFileSynched]),0) + ISNULL(sum(ODB_TotalFileViewedModified),0) AS ODB_TotalofAllActivities
		,SUM([ODB_CollaboratedByOthers]) AS ODB_CollaboratedByOthers
		,SUM([ODB_CollaboratedByOwner]) AS ODB_CollaboratedByOwner
		,SUM([ODB_TotalFileSharedEXT]) AS ODB_TotalFileSharedEXT
		,SUM([ODB_TotalFileSharedINT]) AS ODB_TotalFileSharedINT
		,SUM([ODB_TotalFileSynched]) AS ODB_TotalFileSynched
		,SUM(ODB_TotalFileViewedModified) AS ODB_TotalFileViewedModified
		,CASE when sum([ODB_TotalFileSynched]) > 0 then 1 else 0 end AS UsingOneDrive
		,case when sum(ODB_TotalFileSharedEXT) + sum(ODB_TotalFileSharedINT) > 0 then 1 else 0 end  AS UsersSharing
		,case when sum(ODB_TotalFileSynched) > 0 then 1 else 0 end  AS UsersSyncing
	  FROM [dbo].GraphOneDriveActivityDetail pod 
	  inner join dbo.O365PreviewOneDriveDates odd on pod.LastActivityDate between odd.DTSTART and odd.DTEND
	  where odd.RequiresCalculation = 1
	  GROUP BY odd.ID, odd.FormattedDate, POD.UPN
	),
	TotalUserDetailSpec AS (

	SELECT
		tud.ReportingDate,
		tud.ReportingID,
		tud.LastActivityDate,
		tud.UPN,
		gad.ProductsAssigned,
		gad.Deleted,
		gad.DeletedDate,
		tud.TotalActiveDays,
		tud.UsingOneDrive,
		tud.UsersSharing,
		tud.UsersSyncing,
		tud.ODB_TotalofAllActivities,
		tud.ODB_CollaboratedByOthers,
		tud.ODB_CollaboratedByOwner,
		tud.ODB_TotalFileSharedEXT,
		tud.ODB_TotalFileSharedINT,
		tud.ODB_TotalFileSynched,
		tud.ODB_TotalFileViewedModified
	from TotalUserDetail tud 
		inner join dbo.GraphOneDriveActivityDetail gad on tud.UPN = gad.UPN AND tud.LastActivityDate = gad.LastActivityDate
	)

	-- establish the target or destination table
	MERGE INTO [dbo].[O365PreviewOneDriveMonthly] AS odsm

	-- MERGE key/logic
	USING TotalUserDetailSpec ON TotalUserDetailSpec.ReportingID = odsm.ReportingDateID and TotalUserDetailSpec.UPN = odsm.UPN

	-- IF No Row, Add
	WHEN NOT MATCHED BY TARGET THEN
	INSERT (
		[ReportingDateID],[UPN],[LastActivityDate],[ProductsAssigned],[Deleted],[DeletedDate]
		,[UsingOneDrive],[UsersSharing],[UsersSyncing]
		,[ODB_TotalofAllActivities],[TotalActiveDays]
		,[ODB_CollaboratedByOthers],[ODB_CollaboratedByOwner]
		,[ODB_TotalFileSharedEXT],[ODB_TotalFileSharedINT],[ODB_TotalFileSynched],[ODB_TotalFileViewedModified]
		,[TotalFiles]
		,[TotalFilesViewedModified]
		,[Storage_Allocated_B],[Storage_Used_B],[Storage_Used_MB],[Storage_Used_GB],[Storage_Used_TB],[Storage_Used_perct],
		DTADDED, DTUPD
	)
	VALUES (
		ReportingID,[UPN],[LastActivityDate],[ProductsAssigned],[Deleted],[DeletedDate]
		,[UsingOneDrive],[UsersSharing],[UsersSyncing]
		,[ODB_TotalofAllActivities],[TotalActiveDays]
		,[ODB_CollaboratedByOthers],[ODB_CollaboratedByOwner]
		,[ODB_TotalFileSharedEXT],[ODB_TotalFileSharedINT],[ODB_TotalFileSynched],[ODB_TotalFileViewedModified],
		0,
		0,
		0,0,0,0,0,0,
		GETDATE(), GETDATE()
	)
	-- Update the Values
	WHEN MATCHED THEN
		UPDATE SET odsm.LastActivityDate = TotalUserDetailSpec.LastActivityDate,
		odsm.[ProductsAssigned] = TotalUserDetailSpec.[ProductsAssigned],
		odsm.[Deleted] = TotalUserDetailSpec.[Deleted],
		odsm.[DeletedDate] = TotalUserDetailSpec.[DeletedDate],
		odsm.[UsingOneDrive] = TotalUserDetailSpec.[UsingOneDrive],
		odsm.[UsersSharing] = TotalUserDetailSpec.[UsersSharing],
		odsm.[UsersSyncing] = TotalUserDetailSpec.[UsersSyncing],
		odsm.[TotalActiveDays] = TotalUserDetailSpec.[TotalActiveDays],
		odsm.[ODB_TotalofAllActivities] = TotalUserDetailSpec.[ODB_TotalofAllActivities],
		odsm.[ODB_CollaboratedByOthers] = TotalUserDetailSpec.[ODB_CollaboratedByOthers],
		odsm.[ODB_CollaboratedByOwner] = TotalUserDetailSpec.[ODB_CollaboratedByOwner],
		odsm.[ODB_TotalFileSharedEXT] = TotalUserDetailSpec.[ODB_TotalFileSharedEXT],
		odsm.[ODB_TotalFileSharedINT] = TotalUserDetailSpec.[ODB_TotalFileSharedINT],
		odsm.[ODB_TotalFileSynched] = TotalUserDetailSpec.[ODB_TotalFileSynched],
		odsm.[ODB_TotalFileViewedModified] = TotalUserDetailSpec.[ODB_TotalFileViewedModified],
		odsm.DTUPD = GETDATE()

	-- Double Check the Query and Updates
	OUTPUT 
		'[dbo].[O365PreviewOneDriveMonthly]' AS TableName,
		ISNULL(inserted.ReportingDateID, deleted.ReportingDateID) as ReportingDateID, 
		ISNULL(inserted.[UPN], deleted.[UPN]) as Office, 
		SUSER_SNAME() + ' ' +$action AS logAction,
		GETDATE() AS logDate
	INTO [dbo].[O365PreviewOneDrive_DataBuildLog] (TableName, ReportingDateID, UPN, logAction, logDate)
	;

END