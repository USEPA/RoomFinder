-- =============================================================================
-- Author:		James Hunter
-- Create date: 11/6/2019
-- Description:	Import eBusinessAccount ETL data into dbo.eBusinessAccounts
-- =============================================================================
CREATE PROCEDURE [dbo].[sp_ins_dboEBusinessAccounts] 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	--DEACTIVATE PRIOR IMPORT
	WITH ActiveEBusinessAccounts AS
	(
		SELECT [ID]
			,[UserEmailAddress]
			,[UserSamAccountName]
			,[UserEnabled]
			,[UserIsContractor]
			,[DT_IMPORT]
			,[DT_DEACTIVATED]
			,[IsDeactivated]
		FROM [dbo].[eBusinessAccounts]
		WHERE IsDeactivated = 0 and DT_DEACTIVATED is null
	)

	UPDATE eba
	set eba.DT_DEACTIVATED = GETDATE(), 
	eba.IsDeactivated = 1
	FROM ActiveEBusinessAccounts aeba inner join dbo.eBusinessAccounts eba on aeba.ID = eba.ID
	WHERE eba.IsDeactivated = 0;

	--IMPORT ETL INTO DBO
	WITH ebacte AS (
	SELECT 
		eba.SAMACCOUNTNAME,
		eba.ORGANIZATION,
		eba.ACRONYM,
		eba.OFFICECODE,
		eba.OFFICENAME, 
		replace( replace(eba.OFFICENAME, ',', '-'), 'REG-', 'R') CleanOfficeName,
		eba.PREFERRED_FIRSTNAME,
		eba.FIRSTNAME,
		eba.MIDDLE_INITIAL,
		eba.LASTNAME,
		eba.AFFLIATION_CODE,
		eba.EMAIL, 
		eba.WORKFORCEID, 
		case when eba.AFFLIATION_CODE <> 'EPA' then 1 else 0 end ContractorFlag,
		eba.DISTINGUISHEDNAME,
		eba.ENABLED,
		eba.BUILDING,
		eba.ADDRESS_LINE1,
		eba.CITY,
		eba.STATE,
		eba.ZIPCODE,
		eba.ROOM_NUMBER,
		eba.DT_IMPORT
	FROM etl.eBusinessAccounts eba
	WHERE eba.OFFICENAME is not null
	),

	ebacteoffice AS (
	SELECT
		eba.SAMACCOUNTNAME,
		eba.ORGANIZATION,
		eba.ACRONYM,
		eba.OFFICECODE,
		eba.OFFICENAME, 
		eba.CleanOfficeName,
		dbo.UFN_SEPARATES_COLUMNS(eba.CleanOfficeName, 1, '-') as OrgOffice,
		dbo.UFN_SEPARATES_COLUMNS(eba.CleanOfficeName, 2, '-') as OrgOfficeBranch,
		dbo.UFN_SEPARATES_COLUMNS(eba.CleanOfficeName, 3, '-') as OrgOfficeBranchSection,
		dbo.UFN_SEPARATES_COLUMNS(eba.CleanOfficeName, 4, '-') as OrgOfficeBranchUnit,
		eba.PREFERRED_FIRSTNAME,
		eba.FIRSTNAME,
		eba.MIDDLE_INITIAL,
		eba.LASTNAME,
		eba.AFFLIATION_CODE,
		eba.EMAIL, 
		eba.WORKFORCEID, 
		eba.ContractorFlag,
		eba.DISTINGUISHEDNAME,
		eba.[ENABLED],
		eba.BUILDING,
		eba.ADDRESS_LINE1,
		eba.CITY,
		eba.[STATE],
		eba.ZIPCODE,
		eba.ROOM_NUMBER,
		eba.DT_IMPORT
	FROM
		ebacte eba
	WHERE dbo.UFN_SEPARATES_COLUMNS(eba.CleanOfficeName, 1, '-') in ('AO', 'OAR', 'OCSPP', 'OCFO', 'OECA', 'OGC', 'OIG', 'OITA', 'OLEM', 'OMS', 'ORD', 'OW', 'R01', 'R02', 'R03', 'R04', 'R05', 'R06', 'R07', 'R08', 'R09', 'R10')
	)

	-- ESTABLISH THE TARGET OR DESTINATION TABLE
	MERGE INTO [dbo].eBusinessAccounts AS odsm

	-- MERGE KEY/LOGIC
	USING ebacteoffice on ebacteoffice.samaccountname = odsm.UserSamAccountName AND ebacteoffice.DT_IMPORT = odsm.DT_IMPORT

	-- IF NO ROW, ADD
	WHEN NOT MATCHED BY TARGET THEN
	INSERT 
	( 
		OrgNumCode, OrgAcronym, OfficeCode, OfficeName, 
		OrgOffice, OrgOfficeBranch, OrgOfficeBranchSection, OrgOfficeBranchUnit,
		UserGivenNamePreferred, UserGivenName, UserMiddleInitial, UserSurName,
		AffiliationCode, UserEmailAddress, UserWorkforceID, UserIsContractor, 
		UserSamAccountName, UserEnabled,
		UserBuilding, UserBuildingRoomNo, UserAddressLine1, UserAddressCity, UserAddressState, UserAddressZipCode, 
		DT_IMPORT, IsDeactivated
	)
	VALUES 
	(
		ORGANIZATION, OrgOffice, OFFICECODE, OFFICENAME, 
		OrgOffice, OrgOfficeBranch, OrgOfficeBranchSection, OrgOfficeBranchUnit,
		PREFERRED_FIRSTNAME, FIRSTNAME, MIDDLE_INITIAL, LASTNAME,
		AFFLIATION_CODE, EMAIL, WORKFORCEID, ContractorFlag,
		SAMACCOUNTNAME, [ENABLED],
		BUILDING, ROOM_NUMBER, ADDRESS_LINE1, CITY,	[STATE], ZIPCODE,
		DT_IMPORT, 0
	)
	-- UPDATE THE VALUES
	WHEN MATCHED THEN
	UPDATE SET 
	odsm.OrgNumCode = ebacteoffice.ORGANIZATION,
	odsm.OrgAcronym = ebacteoffice.ACRONYM,
	odsm.OfficeCode = ebacteoffice.OFFICECODE,
	odsm.OfficeName = ebacteoffice.OFFICENAME,
	odsm.OrgOffice = ebacteoffice.ACRONYM,
	odsm.OrgOfficeBranch = ebacteoffice.OrgOfficeBranch,
	odsm.OrgOfficeBranchSection = ebacteoffice.OrgOfficeBranchSection,
	odsm.OrgOfficeBranchUnit = ebacteoffice.OrgOfficeBranchUnit,
	odsm.UserEmailAddress = ebacteoffice.EMAIL,
	odsm.UserGivenNamePreferred = ebacteoffice.PREFERRED_FIRSTNAME,
	odsm.UserGivenName = ebacteoffice.FIRSTNAME,
	odsm.UserMiddleInitial = ebacteoffice.MIDDLE_INITIAL,
	odsm.UserSurName = ebacteoffice.LASTNAME,
	odsm.UserEnabled = ebacteoffice.[ENABLED],
	odsm.UserIsContractor = ebacteoffice.ContractorFlag,
	odsm.UserBuilding = ebacteoffice.BUILDING,
	odsm.UserBuildingRoomNo = ebacteoffice.ROOM_NUMBER,
	odsm.UserAddressCity = ebacteoffice.CITY,
	odsm.UserAddressState = ebacteoffice.[STATE]

	-- DOUBLE CHECK THE QUERY AND UPDATES
	OUTPUT 
	'[dbo].[eBusinessAccounts]' AS TableName,
	ISNULL(inserted.UserEmailAddress, deleted.UserEmailAddress) as UserEmailAddressID, 
	SUSER_SNAME() + ' ' +$action AS logAction,
	GETDATE() AS logDate
	;

END
GO


