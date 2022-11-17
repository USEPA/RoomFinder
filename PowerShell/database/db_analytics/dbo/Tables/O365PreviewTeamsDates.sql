CREATE TABLE [dbo].[O365PreviewTeamsDates](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FormattedDate] [varchar](50) NULL,
	[DTSTART] [datetime] NULL,
	[DTEND] [datetime] NULL,
	[TotalMetricDays] [int] NULL,
	[RequiresCalculation] [bit] NOT NULL,
 CONSTRAINT [PK_O365PreviewTeamsDates] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[O365PreviewTeamsDates] ADD  CONSTRAINT [DF_O365PreviewTeamsDates_RequiresCalculation]  DEFAULT ((0)) FOR [RequiresCalculation]
GO


