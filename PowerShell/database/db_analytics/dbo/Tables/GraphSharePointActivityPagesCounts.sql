CREATE TABLE [dbo].[GraphSharePointActivityPagesCounts] (
    [ReportRefreshDate] DATETIME NULL,
    [VisitedPageCount]  BIGINT   NULL,
    [ReportDate]        DATETIME NOT NULL,
    [ReportPeriod]      INT      NULL,
    CONSTRAINT [PK_GraphSharePointActivityPagesCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

