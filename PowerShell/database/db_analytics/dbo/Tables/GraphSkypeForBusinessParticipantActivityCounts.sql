CREATE TABLE [dbo].[GraphSkypeForBusinessParticipantActivityCounts] (
    [ReportDate]        DATETIME NOT NULL,
    [IM]                BIGINT   NULL,
    [AudioVideo]        BIGINT   NULL,
    [AppSharing]        BIGINT   NULL,
    [Web]               BIGINT   NULL,
    [DialInOut3rdParty] BIGINT   NULL,
    [ReportRefreshDate] DATETIME NOT NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessParticipantActivityCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

