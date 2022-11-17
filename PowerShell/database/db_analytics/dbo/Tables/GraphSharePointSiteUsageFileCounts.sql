CREATE TABLE [dbo].[GraphSharePointSiteUsageFileCounts] (
    [ReportRefreshDate] DATETIME      NULL,
    [SiteType]          VARCHAR (200) NOT NULL,
    [Total]             BIGINT        NULL,
    [Active]            BIGINT        NULL,
    [ReportDate]        DATETIME      NOT NULL,
    [ReportPeriod]      INT           NULL,
    CONSTRAINT [PK_GraphSharePointSiteFileCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC, [SiteType] ASC)
);

