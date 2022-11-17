CREATE TABLE [dbo].[GraphSkypeForBusinessDeviceUsageDistributionUserCounts] (
    [ReportRefreshDate] DATETIME NOT NULL,
    [Windows]           BIGINT   NULL,
    [WindowsPhone]      BIGINT   NULL,
    [AndroidPhone]      BIGINT   NULL,
    [iPhone]            BIGINT   NULL,
    [iPad]              BIGINT   NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessDeviceUsageDistributionUserCounts] PRIMARY KEY CLUSTERED ([ReportRefreshDate] ASC)
);

