﻿CREATE TABLE [dbo].[O365PreviewOneDriveMonthlyRollup] (
    [ReportingDateID]                INT             NOT NULL,
    [Office]                         VARCHAR (50)    NOT NULL,
    [TotalUsers]                     INT             NULL,
    [TotalActiveUsers]               INT             NULL,
    [TotalActiveUsersPerct]          DECIMAL (18, 6) NULL,
    [TotalEmployeeUsers]             INT             NULL,
    [TotalEmployeeUsersPerct]        DECIMAL (18, 6) NULL,
    [TotalContractorUsers]           INT             NULL,
    [TotalContractorUsersPerct]      DECIMAL (18, 6) NULL,
    [UsingOneDriveCount]             INT             NULL,
    [UsingOneDrivePerct]             DECIMAL (18, 6) NULL,
    [UsingOneDriveEnabledCount]      INT             NULL,
    [UsingOneDriveEnabledPerct]      DECIMAL (18, 6) NULL,
    [UsingOneDriveEmployeeCount]     INT             NULL,
    [UsingOneDriveEmployeePerct]     DECIMAL (18, 6) NULL,
    [UsingOneDriveContractorCount]   INT             NULL,
    [UsingOneDriveContractorPerct]   DECIMAL (18, 6) NULL,
    [SyncingOneDriveCount]           INT             NULL,
    [SyncingOneDrivePerct]           DECIMAL (18, 6) NULL,
    [SyncingOneDriveEnabledCount]    INT             NULL,
    [SyncingOneDriveEnabledPerct]    DECIMAL (18, 6) NULL,
    [SyncingOneDriveEmployeeCount]   INT             NULL,
    [SyncingOneDriveEmployeePerct]   DECIMAL (18, 6) NULL,
    [SyncingOneDriveContractorCount] INT             NULL,
    [SyncingOneDriveContractorPerct] DECIMAL (18, 6) NULL,
    [SharingOneDriveCount]           INT             NULL,
    [SharingOneDrivePerct]           DECIMAL (18, 6) NULL,
    [SharingOneDriveEnabledCount]    INT             NULL,
    [SharingOneDriveEnabledPerct]    DECIMAL (18, 6) NULL,
    [SharingOneDriveEmployeeCount]   INT             NULL,
    [SharingOneDriveEmployeePerct]   DECIMAL (18, 6) NULL,
    [SharingOneDriveContractorCount] INT             NULL,
    [SharingOneDriveContractorPerct] DECIMAL (18, 6) NULL,
    [DTADDED]                        DATETIME        NULL,
    [DTUPD]                          DATETIME        NULL,
    CONSTRAINT [PK_O365PreviewOneDriveUsageMonthly] PRIMARY KEY CLUSTERED ([ReportingDateID] ASC, [Office] ASC)
);
