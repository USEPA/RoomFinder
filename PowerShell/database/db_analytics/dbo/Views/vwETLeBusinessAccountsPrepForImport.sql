CREATE VIEW [dbo].[vwETLeBusinessAccountsPrepForImport]
AS
SELECT     SAMACCOUNTNAME, ORGANIZATION, ACRONYM, OFFICECODE, OFFICENAME, REPLACE(REPLACE(OFFICENAME, ',', '-'), 'REG-', 'R') AS CleanOfficeName, 
                      dbo.UFN_SEPARATES_COLUMNS(REPLACE(REPLACE(OFFICENAME, ',', '-'), 'REG-', 'R'), 1, '-') AS OrgOffice, 
                      dbo.UFN_SEPARATES_COLUMNS(REPLACE(REPLACE(OFFICENAME, ',', '-'), 'REG-', 'R'), 2, '-') AS OrgOfficeBranch, 
                      dbo.UFN_SEPARATES_COLUMNS(REPLACE(REPLACE(OFFICENAME, ',', '-'), 'REG-', 'R'), 3, '-') AS OrgOfficeBranchSection, 
                      dbo.UFN_SEPARATES_COLUMNS(REPLACE(REPLACE(OFFICENAME, ',', '-'), 'REG-', 'R'), 4, '-') AS OrgOfficeBranchUnit, PREFERRED_FIRSTNAME, FIRSTNAME, 
                      MIDDLE_INITIAL, LASTNAME, AFFLIATION_CODE, EMAIL, WORKFORCEID, CASE WHEN PATINDEX('99%', eba.WORKFORCEID) 
                      > 0 THEN 1 ELSE 0 END AS ContractorFlag, DISTINGUISHEDNAME, ENABLED, BUILDING, ADDRESS_LINE1, CITY, STATE, ZIPCODE, ROOM_NUMBER, 
                      DT_IMPORT
FROM         etl.eBusinessAccounts AS eba
GO