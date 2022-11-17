CREATE VIEW [dbo].[vwO365GroupAnalytics]
AS
SELECT        aog.ID, aog.GroupId, aog.GroupName, aog.MailAddress, aog.MailEnabled, 
                         aog.AllowExternalSenders, aog.AutoSubscribeNewMembers, aog.Site, aog.PublicPrivate, aog.EPAWide, 
                         aog.Office, aog.CreatedDate, aog.CreatedDateMonth, aog.CreatedDateYear, aog.Storage_GB, 
                         aog.Storage_Used_Perct, aog.Storage_Quota_GB, 
						 aog.IsDeleted, aog.ReportRefreshDate, aog.LastActivityDate, aog.MemberCount, aog.ExchangeReceivedEmailCount,
						 aog.SharePointActiveFileCount, aog.ExchangeMailboxTotalItemCount, aog.ExchangeMailboxStorageUsed_Byte,
						 aog.SharePointTotalFileCount, aog.SharePointSiteStorageUsed_Byte, aog.ReportPeriod,
						 aog.DTADDED as GroupAddedDate, aog.DTUPD as GroupUpdatedDate, aog.PrimaryOwner, 
                         aog.Owners, aog.TeamDisplayName, aog.TeamDescription, aog.TeamInternalId, aog.TeamClassification, 
                         aog.TeamSpecialization, aog.TeamVisibility, aog.TeamDiscoverySettings, aog.TeamResponseHeaders, 
                         aog.TeamStatusCode, aog.TeamIsArchived, aog.TeamWebUrl, eba.OrgNumCode, 
                         eba.OrgAcronym, eba.OfficeCode, eba.OfficeName, eba.OrgOffice, eba.OrgOfficeBranch, 
                         eba.OrgOfficeBranchSection, eba.OrgOfficeBranchUnit, eba.UserGivenNamePreferred, eba.UserGivenName, 
                         eba.UserMiddleInitial, eba.UserSurName, 
						 eba.UserGivenName + ' ' + eba.UserSurName AS UserFullName,
						 eba.AffiliationCode, eba.UserEmailAddress, eba.UserWorkforceID, 
                         eba.UserSamAccountName, eba.UserEnabled, eba.UserIsContractor, eba.UserBuilding, eba.UserBuildingRoomNo, 
                         eba.UserAddressLine1, eba.UserAddressCity, eba.UserAddressState, eba.UserAddressZipCode,
						 CASE WHEN (aog.LastActivityDate >= DATEADD(day,-7, GETDATE()) OR aog.LastActivityDate is NULL) THEN 'Yes' ELSE 'No' END AS ActiveGroup,
						 CASE WHEN aog.CreatedDate >= DATEADD(day,-30, GETDATE()) THEN 'Yes' ELSE 'No' END AS CreatedLastThirtyDays,
						 CASE WHEN aog.CreatedDate >= DATEADD(day,-60, GETDATE()) THEN 'Yes' ELSE 'No' END AS CreatedLastSixtyDays,
						 CASE WHEN aog.CreatedDate >= DATEADD(day,-90, GETDATE()) THEN 'Yes' ELSE 'No' END AS CreatedLastNinetyDays,
						 CASE WHEN eba.OrgOffice is NULL THEN 'Site Provisioned' ELSE eba.OrgOffice END AS ProvisionedBy
FROM            dbo.AnalyticsO365Groups aog LEFT OUTER JOIN
                         dbo.eBusinessAccounts eba ON aog.PrimaryOwner = eba.UserEmailAddress and eba.IsDeactivated = 0
		
GO


