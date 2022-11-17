CREATE TABLE [dbo].[GraphSharePointSiteUsageSiteCounts] (
    [ReportRefreshDate] DATETIME      NULL,
    [SiteType]          VARCHAR (200) NULL,
    [Total]             BIGINT        NULL,
    [Active]            BIGINT        NULL,
    [ReportDate]        DATETIME      NOT NULL,
    [ReportPeriod]      INT           NULL,
    CONSTRAINT [PK_GraphSharePointSiteUsageCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

