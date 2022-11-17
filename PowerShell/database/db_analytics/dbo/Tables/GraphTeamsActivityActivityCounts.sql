CREATE TABLE [dbo].[GraphTeamsActivityActivityCounts](
	[ReportDate] [datetime] NOT NULL,
	[TeamChatMessages] [bigint] NULL,
	[PrivateChatMessages] [bigint] NULL,
	[Calls] [bigint] NULL,
	[Meetings] [bigint] NULL,
	[ReportPeriod] [int] NULL,
	[ReportRefreshDate] [datetime] NULL,
 CONSTRAINT [PK_GraphTeamsActivityActivityCounts] PRIMARY KEY CLUSTERED 
(
	[ReportDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


