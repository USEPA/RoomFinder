CREATE TABLE [dbo].[GraphSharePointSiteUsagePages] (
    [ReportRefreshDate] DATETIME      NULL,
    [SiteType]          VARCHAR (200) NULL,
    [PageViewCount]     BIGINT        NULL,
    [ReportDate]        DATETIME      NOT NULL,
    [ReportPeriod]      INT           NULL,
    CONSTRAINT [PK_GraphSharePointSiteUsagePages] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

