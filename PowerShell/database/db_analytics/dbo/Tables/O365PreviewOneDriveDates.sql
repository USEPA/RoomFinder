CREATE TABLE [dbo].[O365PreviewOneDriveDates] (
    [ID]                  INT          IDENTITY (1, 1) NOT NULL,
    [FormattedDate]       VARCHAR (50) NULL,
    [DTSTART]             DATETIME     NULL,
    [DTEND]               DATETIME     NULL,
    [TotalMetricDays]     INT          NULL,
    [RequiresCalculation] BIT          CONSTRAINT [DF_O365PreviewOneDriveDates_RequiresCalculation] DEFAULT ((0)) NOT NULL,
    [LastDateActivity]    DATETIME     NULL,
    [LastDateUsage]       DATETIME     NULL,
    CONSTRAINT [PK_O365PreviewOneDriveDates] PRIMARY KEY CLUSTERED ([ID] ASC)
);

