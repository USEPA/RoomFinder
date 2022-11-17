CREATE TABLE [dbo].[GraphOneDriveUsageFileCounts] (
    [ReportRefreshDate] DATETIME      NULL,
    [SiteType]          VARCHAR (200) NOT NULL,
    [Total]             BIGINT        NULL,
    [Active]            BIGINT        NULL,
    [ReportDate]        DATETIME      NOT NULL,
    [ReportPeriod]      INT           NULL,
    CONSTRAINT [PK_GraphOneDriveUsageFileCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC, [SiteType] ASC)
);

