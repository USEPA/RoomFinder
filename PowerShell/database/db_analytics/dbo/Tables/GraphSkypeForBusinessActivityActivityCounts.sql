CREATE TABLE [dbo].[GraphSkypeForBusinessActivityActivityCounts] (
    [ReportDate]        DATETIME NOT NULL,
    [PeerToPeer]        BIGINT   NULL,
    [Organized]         BIGINT   NULL,
    [Participated]      BIGINT   NULL,
    [ReportRefreshDate] DATETIME NOT NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessActivityActivityCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

