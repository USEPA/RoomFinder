CREATE TABLE [dbo].[TenantDates] (
    [ID]            INT           IDENTITY (1, 1) NOT NULL,
    [AnalyticType]  INT           CONSTRAINT [DF_TenantDates_AnalyticType] DEFAULT ((0)) NOT NULL,
    [FormattedDate] VARCHAR (50)  NULL,
    [DTSTART]       DATETIME2 (7) NOT NULL,
    [DTEND]         DATETIME2 (7) NOT NULL,
    [IsCurrent]     BIT           CONSTRAINT [DF_TenantDates_IsCurrent] DEFAULT ((0)) NOT NULL,
    [IsComplete]    BIT           CONSTRAINT [DF_TenantDates_IsComplete] DEFAULT ((0)) NOT NULL,
    [TotalSites]    INT           NULL,
    [TotalWebs]     INT           NULL,
    CONSTRAINT [PK_TenantDates] PRIMARY KEY CLUSTERED ([ID] ASC)
);

