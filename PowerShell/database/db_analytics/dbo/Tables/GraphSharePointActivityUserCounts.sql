CREATE TABLE [dbo].[GraphSharePointActivityUserCounts] (
    [ReportRefreshDate] DATETIME NULL,
    [VisitedPage]       BIGINT   NULL,
    [ViewedOrEdited]    BIGINT   NULL,
    [Synced]            BIGINT   NULL,
    [SharedInternally]  BIGINT   NULL,
    [SharedExternally]  BIGINT   NULL,
    [ReportDate]        DATETIME NOT NULL,
    [ReportPeriod]      INT      NULL,
    CONSTRAINT [PK_GraphSharePointActivityUserCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

