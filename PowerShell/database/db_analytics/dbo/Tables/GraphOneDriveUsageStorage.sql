CREATE TABLE [dbo].[GraphOneDriveUsageStorage] (
    [LastActivityDate]      DATETIME        NOT NULL,
    [SiteType]              VARCHAR (50)    NOT NULL,
    [ReportingPeriodInDays] INT             NULL,
    [Storage_Used_B]        BIGINT          NULL,
    [Storage_Used_MB]       DECIMAL (18, 6) CONSTRAINT [DF_PreviewOneDriveUsageStorage_Storage_Used_MB] DEFAULT ((0)) NOT NULL,
    [Storage_Used_GB]       DECIMAL (18, 6) CONSTRAINT [DF_PreviewOneDriveUsageStorage_Storage_Used_GB] DEFAULT ((0)) NOT NULL,
    [Storage_Used_TB]       DECIMAL (18, 6) CONSTRAINT [DF_PreviewOneDriveUsageStorage_Storage_Used_TB] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_PreviewOneDriveUsageStorage_1] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC, [SiteType] ASC)
);

