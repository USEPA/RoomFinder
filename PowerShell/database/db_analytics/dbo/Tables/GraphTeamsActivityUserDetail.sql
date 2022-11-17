CREATE TABLE [dbo].[GraphTeamsActivityUserDetail](
	[UPN] [varchar](255) NOT NULL,
	[LastActivityDate] [datetime] NOT NULL,
	[Deleted] [varchar](255) NULL,
	[DeletedDate] [datetime] NULL,
	[TeamChatMessageCount] [bigint] NULL,
	[PrivateChatMessageCount] [bigint] NULL,
	[CallCount] [bigint] NULL,
	[MeetingCount] [bigint] NULL,
	[HasOtherAction] [varchar](255) NULL,
	[ProductsAssigned] [varchar](MAX) NULL,
	[ReportPeriod] [int] NULL,
	[ReportRefreshDate] [datetime] NULL,
 CONSTRAINT [PK_GraphTeamsActivityUserDetail] PRIMARY KEY CLUSTERED 
(
	[UPN] ASC,
	[LastActivityDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


