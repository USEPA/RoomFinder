CREATE TABLE [dbo].[GraphSharePointActivityFileCounts] (
    [ReportRefreshDate] DATETIME NULL,
    [ViewedOrEdited]    BIGINT   NULL,
    [Synced]            BIGINT   NULL,
    [SharedInternally]  BIGINT   NULL,
    [SharedExternally]  BIGINT   NULL,
    [ReportDate]        DATETIME NOT NULL,
    [ReportPeriod]      INT      NULL,
    CONSTRAINT [PK_GraphSharePointActivityFileCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

