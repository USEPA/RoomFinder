CREATE TABLE [dbo].[GraphTeamsDeviceUsageUserCounts](
	[ReportDate] [datetime] NOT NULL,
	[Web] [bigint] NULL,
	[Windows] [bigint] NULL,
	[WindowsPhone] [bigint] NULL,
	[AndroidPhone] [bigint] NULL,
	[iOS] [bigint] NULL,
	[Mac] [bigint] NULL,
	[ReportPeriod] [int] NULL,
	[ReportRefreshDate] [datetime] NULL,
 CONSTRAINT [PK_GraphTeamsDeviceUsageUserCounts] PRIMARY KEY CLUSTERED 
(
	[ReportDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


