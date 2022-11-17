CREATE TABLE [dbo].[GraphSkypeForBusinessOrganizerActivityMinuteCounts] (
    [ReportDate]        DATETIME NOT NULL,
    [AudioVideo]        BIGINT   NULL,
    [DialInMicrosoft]   BIGINT   NULL,
    [DialOutMicrosoft]  BIGINT   NULL,
    [ReportRefreshDate] DATETIME NOT NULL,
    [ReportPeriod]      INT      NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessOrganizerActivityMinuteCounts] PRIMARY KEY CLUSTERED ([ReportDate] ASC)
);

