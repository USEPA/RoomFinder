CREATE TABLE [dbo].[GraphO365ActiveUsers] (
    [LastActivityDate]    DATETIME NOT NULL,
    [Office365]           BIGINT   NULL,
    [Exchange]            BIGINT   NULL,
    [OneDrive]            BIGINT   NULL,
    [SharePoint]          BIGINT   NULL,
    [SkypeForBusiness]    BIGINT   NULL,
    [Yammer]              BIGINT   NULL,
    [Teams]               BIGINT   NULL,
    [ReportingPeriodDays] INT      NULL,
    [ReportRefreshDate]   DATETIME NULL,
    CONSTRAINT [PK_PreviewActiveUsers] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC)
);

