CREATE TABLE [dbo].[O365PreviewSkypeDates] (
    [ID]                  INT          IDENTITY (1, 1) NOT NULL,
    [FormattedDate]       VARCHAR (50) NULL,
    [DTSTART]             DATETIME     NULL,
    [DTEND]               DATETIME     NULL,
    [TotalMetricDays]     INT          NULL,
    [RequiresCalculation] BIT          CONSTRAINT [DF_O365PreviewSkypeDates_RequiresCalculation] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_O365PreviewSkypeDates] PRIMARY KEY CLUSTERED ([ID] ASC)
);

