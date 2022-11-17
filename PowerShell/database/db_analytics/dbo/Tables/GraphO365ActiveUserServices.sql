CREATE TABLE [dbo].[GraphO365ActiveUserServices] (
    [LastActivityDate]    DATETIME NOT NULL,
    [ReportRefreshDate]   DATETIME NULL,
    [ExchangeActive]      BIGINT   NULL,
    [ExchangeInActive]    BIGINT   NULL,
    [OneDriveActive]      BIGINT   NULL,
    [OneDriveInActive]    BIGINT   NULL,
    [SharePointActive]    BIGINT   NULL,
    [SharePointInActive]  BIGINT   NULL,
    [SkypeActive]         BIGINT   NULL,
    [SkypeInActive]       BIGINT   NULL,
    [YammerActive]        BIGINT   NULL,
    [YammerInActive]      BIGINT   NULL,
    [TeamsActive]         BIGINT   NULL,
    [TeamsInActive]       BIGINT   NULL,
    [ReportingPeriodDays] INT      NULL,
    CONSTRAINT [PK_O365PreviewActiveUserServices] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC)
);

