CREATE TABLE [dbo].[O365PreviewOneDrive_DataBuildLog] (
    [ID]              INT           IDENTITY (1, 1) NOT NULL,
    [TableName]       VARCHAR (100) NULL,
    [ReportingDateID] INT           NULL,
    [Office]          VARCHAR (50)  NULL,
    [UPN]             VARCHAR (255) NULL,
    [logAction]       VARCHAR (250)  NULL,
    [logDate]         DATETIME      NULL
);

