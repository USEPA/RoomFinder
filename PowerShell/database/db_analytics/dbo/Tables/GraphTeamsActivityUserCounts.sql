CREATE TABLE [dbo].[GraphTeamsActivityUserCounts](
	[ReportDate] [datetime] NOT NULL,
	[TeamChatMessages] [bigint] NULL,
	[PrivateChatMessages] [bigint] NULL,
	[Calls] [bigint] NULL,
	[Meetings] [bigint] NULL,
	[OtherActions] [bigint] NULL,
	[ReportPeriod] [int] NULL,
	[ReportRefreshDate] [datetime] NULL,
 CONSTRAINT [PK_GraphTeamsActivityUserCounts] PRIMARY KEY CLUSTERED 
(
	[ReportDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


