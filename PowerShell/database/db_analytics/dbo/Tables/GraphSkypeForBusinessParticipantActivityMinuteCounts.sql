CREATE TABLE [dbo].[GraphSkypeForBusinessParticipantActivityMinuteCounts] (
    [ReportDate]        DATETIME NOT NULL,
    [AudioVideo]        BIGINT   NULL,
    [ReportRefreshDate] DATETIME NOT NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessParticipantActivityMinuteCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

