CREATE TABLE [dbo].[GraphSkypeForBusinessPeerToPeerActivityMinuteCounts] (
    [ReportDate]        DATETIME NOT NULL,
    [Audio]             BIGINT   NULL,
    [Video]             BIGINT   NULL,
    [ReportRefreshDate] DATETIME NOT NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessPeerToPeerActivityMinuteCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

