CREATE VIEW [dbo].[vwGraphOneDriveUsageDetail]
AS
SELECT        LastActivityDate, SiteURL, ReportingPeriodInDays, UPN, SiteOwner, Deleted, TotalFiles, TotalFilesViewedModified, Storage_Allocated_B, Storage_Used_B
FROM            dbo.GraphOneDriveUsageDetail AS odud
WHERE        (LastActivityDate IN
                             (SELECT        MAX(LastActivityDate) AS Expr1
                               FROM            dbo.GraphOneDriveUsageDetail
                               WHERE        (SiteOwner = odud.SiteOwner)))
GO