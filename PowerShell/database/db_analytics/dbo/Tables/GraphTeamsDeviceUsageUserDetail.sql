CREATE TABLE [dbo].[GraphTeamsDeviceUsageUserDetail](
	[UPN] [varchar](255) NOT NULL,
	[LastActivityDate] [datetime] NOT NULL,
	[Deleted] [varchar](255) NULL,
	[DeletedDate] [datetime] NULL,
	[UsedWeb] [bit] NULL,
	[UsedWebLastDate] [datetime] NULL,
	[UsedWindows] [bit] NULL,
	[UsedWindowsLastDate] [datetime] NULL,
	[UsedWindowsPhone] [bit] NULL,
	[UsedWindowsPhoneLastDate] [datetime] NULL,
	[UsedAndroidPhone] [bit] NULL,
	[UsedAndroidPhoneLastDate] [datetime] NULL,
	[UsediOS] [bit] NULL,
	[UsediOSLastDate] [datetime] NULL,
	[UsedMac] [bit] NULL,
	[UsedMacLastDate] [datetime] NULL,
	[ReportPeriod] [int] NULL,
	[ReportRefreshDate] [datetime] NULL, 
    CONSTRAINT [PK_GraphTeamsDeviceUsageUserDetail] PRIMARY KEY ([UPN])
) ON [PRIMARY]
GO


