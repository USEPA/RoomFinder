CREATE TABLE [dbo].[GraphSkypeForBusinessPeerToPeerActivityUserCounts] (
    [ReportDate]        DATETIME NOT NULL,
    [IM]                BIGINT   NULL,
    [Audio]             BIGINT   NULL,
    [Video]             BIGINT   NULL,
    [AppSharing]        BIGINT   NULL,
    [FileTransfer]      BIGINT   NULL,
    [ReportRefreshDate] DATETIME NOT NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessPeerToPeerActivityUserCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

