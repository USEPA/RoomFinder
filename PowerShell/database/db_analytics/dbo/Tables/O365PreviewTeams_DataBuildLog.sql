CREATE TABLE [dbo].[O365PreviewTeams_DataBuildLog](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[TableName] [varchar](100) NULL,
	[ReportingDateID] [int] NULL,
	[Office] [varchar](50) NULL,
	[UPN] [varchar](255) NULL,
	[logAction] [varchar](250) NULL,
	[logDate] [datetime] NULL
) ON [PRIMARY]
GO


