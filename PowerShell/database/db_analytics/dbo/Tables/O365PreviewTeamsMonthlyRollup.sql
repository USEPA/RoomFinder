CREATE TABLE [dbo].[O365PreviewTeamsMonthlyRollup](
	[ReportingDateID] [int] NOT NULL,
	[Office] [varchar](50) NOT NULL,
	[TotalUsers] [int] NULL,
	[TotalActiveUsers] [int] NULL,
	[TotalEmployeeUsers] [int] NULL,
	[TotalContractorUsers] [int] NULL,
	[TotalTeamChatMessageCountCount] [int] NULL,
	[TotalTeamChatMessageCountEnabledCount] [int] NULL,
	[TotalTeamChatMessageCountEmployeeCount] [int] NULL,
	[TotalTeamChatMessageCountContractorCount] [int] NULL,
	[TotalPrivateChatMessageCountCount] [int] NULL,
	[TotalPrivateChatMessageCountEnabledCount] [int] NULL,
	[TotalPrivateChatMessageCountEmployeeCount] [int] NULL,
	[TotalPrivateChatMessageCountContractorCount] [int] NULL,
	[TotalCallCountCount] [int] NULL,
	[TotalCallCountEnabledCount] [int] NULL,
	[TotalCallCountEmployeeCount] [int] NULL,
	[TotalCallCountContractorCount] [int] NULL,
	[TotalAvgActiveDays] [int] NULL,
	[TotalofAllActivities] [int] NULL,
	[TotalMeetingCountCount] [int] NULL,
	[TotalMeetingCountEnabledCount] [int] NULL,
	[TotalMeetingCountEmployeeCount] [int] NULL,
	[TotalMeetingCountContractorCount] [int] NULL,
	[ORGTotalTeamChatMessageCount] [int] NULL,
	[ORGTotalPrivateChatMessageCount] [int] NULL,
	[ORGTotalCallCount] [int] NULL,
	[ORGTotalMeetingCount] [int] NULL,
	[DTADDED] [datetime] NULL,
	[DTUPD] [datetime] NULL,
 CONSTRAINT [PK_O365PreviewTeamsMonthlyRollup] PRIMARY KEY CLUSTERED 
(
	[ReportingDateID] ASC,
	[Office] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


