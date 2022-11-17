CREATE TABLE [dbo].[GraphSkypeForBusinessActivityUserCounts] (
    [ReportDate]        DATETIME NOT NULL,
    [PeerToPeer]        BIGINT   NULL,
    [Organized]         BIGINT   NULL,
    [Participated]      BIGINT   NULL,
    [ReportRefreshDate] DATETIME NOT NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessActivityUserCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

