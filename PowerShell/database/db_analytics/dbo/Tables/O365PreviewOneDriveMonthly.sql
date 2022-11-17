CREATE TABLE [dbo].[O365PreviewOneDriveMonthly] (
    [ReportingDateID]             INT             NOT NULL,
    [UPN]                         NVARCHAR (255)  NOT NULL,
    [LastActivityDate]            DATETIME        NULL,
    [TotalActiveDays]             INT             NULL,
    [ProductsAssigned]            VARCHAR (255)   NULL,
    [Deleted]                     VARCHAR (50)    NULL,
    [DeletedDate]                 DATETIME        NULL,
    [UsingOneDrive]               BIT             CONSTRAINT [DF_O365PreviewOneDriveStorageMonthly_NotUsingOneDrive] DEFAULT ((0)) NOT NULL,
    [UsersSharing]                BIT             CONSTRAINT [DF_O365PreviewOneDriveStorageMonthly_UsersSharing] DEFAULT ((0)) NOT NULL,
    [UsersSyncing]                BIT             CONSTRAINT [DF_O365PreviewOneDriveStorageMonthly_UsersSyncing] DEFAULT ((0)) NOT NULL,
    [ODB_TotalofAllActivities]    BIGINT          NULL,
    [ODB_CollaboratedByOthers]    BIGINT          NULL,
    [ODB_CollaboratedByOwner]     BIGINT          NULL,
    [ODB_TotalFileSharedEXT]      BIGINT          NULL,
    [ODB_TotalFileSharedINT]      BIGINT          NULL,
    [ODB_TotalFileSynched]        BIGINT          NULL,
    [ODB_TotalFileViewedModified] BIGINT          NULL,
    [TotalFiles]                  BIGINT          NULL,
    [TotalFilesViewedModified]    BIGINT          NULL,
    [Storage_Allocated_B]         BIGINT          NULL,
    [Storage_Used_B]              BIGINT          NULL,
    [Storage_Used_MB]             DECIMAL (18, 6) NOT NULL,
    [Storage_Used_GB]             DECIMAL (18, 6) NOT NULL,
    [Storage_Used_TB]             DECIMAL (18, 6) NOT NULL,
    [Storage_Used_perct]          DECIMAL (18, 6) CONSTRAINT [DF_O365PreviewOneDriveStorageMonthly_Storage_Used_perct] DEFAULT ((0)) NOT NULL,
    [DTADDED]                     DATETIME        NULL,
    [DTUPD]                       DATETIME        NULL,
    CONSTRAINT [PK_O365PreviewOneDriveStorageMonthly_1] PRIMARY KEY CLUSTERED ([ReportingDateID] ASC, [UPN] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_O365PreviewOneDriveMonthlyLastDate]
    ON [dbo].[O365PreviewOneDriveMonthly]([LastActivityDate] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_O365PreviewOneDriveMonthlyUPN]
    ON [dbo].[O365PreviewOneDriveMonthly]([UPN] ASC);

