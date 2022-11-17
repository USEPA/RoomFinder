CREATE TABLE [dbo].[GraphOneDriveUsageDetail] (
    [LastActivityDate]         DATETIME        NOT NULL,
    [SiteURL]                  VARCHAR (255)   NOT NULL,
    [ReportingPeriodInDays]    INT             NULL,
    [UPN]                      VARCHAR (255)   NULL,
    [SiteOwner]                VARCHAR (255)   NULL,
    [Deleted]                  VARCHAR (50)    NULL,
    [TotalFiles]               BIGINT          NULL,
    [TotalFilesViewedModified] BIGINT          NULL,
    [Storage_Allocated_B]      BIGINT          NULL,
    [Storage_Used_B]           BIGINT          NULL,
    [Storage_Used_MB]          DECIMAL (18, 6) CONSTRAINT [DF_PreviewOneDriveUsageDetail_Storage_Used_MB] DEFAULT ((0)) NOT NULL,
    [Storage_Used_GB]          DECIMAL (18, 6) CONSTRAINT [DF_PreviewOneDriveUsageDetail_Storage_Used_GB] DEFAULT ((0)) NOT NULL,
    [Storage_Used_TB]          DECIMAL (18, 6) CONSTRAINT [DF_PreviewOneDriveUsageDetail_Storage_Used_TB] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_PreviewOneDriveUsageDetail_1] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC, [SiteURL] ASC)
);

