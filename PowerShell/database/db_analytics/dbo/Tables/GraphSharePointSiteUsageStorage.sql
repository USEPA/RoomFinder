CREATE TABLE [dbo].[GraphSharePointSiteUsageStorage] (
    [ReportDate]        DATETIME      NOT NULL,
    [ReportPeriod]      INT           NULL,
    [SiteType]          VARCHAR (200) NULL,
    [StorageUsed_Byte]  BIGINT        NULL,
    [ReportRefreshDate] DATETIME      NULL,
    CONSTRAINT [PK_GraphSharePointSiteUsageStorage] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

