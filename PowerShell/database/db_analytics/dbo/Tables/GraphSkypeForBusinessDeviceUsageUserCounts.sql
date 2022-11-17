CREATE TABLE [dbo].[GraphSkypeForBusinessDeviceUsageUserCounts] (
    [ReportDate]        DATETIME NOT NULL,
    [Windows]           BIGINT   NULL,
    [WindowsPhone]      BIGINT   NULL,
    [AndroidPhone]      BIGINT   NULL,
    [iPhone]            BIGINT   NULL,
    [iPad]              BIGINT   NULL,
    [ReportRefreshDate] DATETIME NOT NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessDeviceUsageUserCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

