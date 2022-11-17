CREATE VIEW [dbo].[vwSPOStats-Employee-Sharing]
AS
SELECT 
	  DATEADD(m, DATEDIFF(m, 0, gsaud.LastActivityDate), 0) as MonthActivityDate
	  ,odss.FormattedDate
	  ,odss.TotalActiveUsers
	  ,odss.TotalEmployeeUsers
--	  ,odss.TotalContractorUsers
      ,count(DISTINCT gsaud.UserPrincipalName) as TotalSPOUsers
	  ,convert(decimal(18,6), count(DISTINCT gsaud.UserPrincipalName))/convert(decimal(18,6), odss.TotalEmployeeUsers) as TotalEmployeeUsersPerct
--	  ,convert(decimal(18,6), count(DISTINCT gsaud.UserPrincipalName))/convert(decimal(18,6), odss.TotalContractorUsers) as TotalContractorUsersPerct
      ,sum(gsaud.SharedInternallyFileCount) as TotalSharedInternallyFileCount
      ,sum(gsaud.SharedExternallyFileCount) as TotalSharedExternallyFileCount
      ,eba.UserIsContractor
FROM 
	[dbo].[GraphSharePointActivityUserDetail] gsaud
	LEFT OUTER JOIN [dbo].eBusinessAccounts eba on eba.UserEmailAddress = gsaud.UserPrincipalName
	LEFT OUTER JOIN [dbo].[zzOneDriveStats-Syncing] odss on CONVERT(varchar, DATEADD(m, DATEDIFF(m, 0, gsaud.LastActivityDate), 0), 101) = odss.FormattedDate
WHERE eba.IsDeactivated = 0 and eba.UserEnabled = 1 and eba.UserIsContractor = 0 and (gsaud.SharedInternallyFileCount > 0 or gsaud.SharedExternallyFileCount > 0)
GROUP BY 
		DATEADD(m, DATEDIFF(m, 0, gsaud.LastActivityDate), 0)
		,odss.FormattedDate
		,odss.TotalActiveUsers
		,odss.TotalEmployeeUsers
--		,odss.TotalContractorUsers
		,eba.UserIsContractor
GO


