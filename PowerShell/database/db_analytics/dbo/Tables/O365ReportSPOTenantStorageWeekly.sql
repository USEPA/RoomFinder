CREATE TABLE [dbo].[O365ReportSPOTenantStorageWeekly] (
    [ID]                   INT             IDENTITY (1, 1) NOT NULL,
    [tblDate]              DATETIME        NULL,
    [tblDateMonth]         INT             NULL,
    [Month]                VARCHAR (25)    NULL,
    [Year]                 INT             NULL,
    [Storage_Used_MB]      DECIMAL (18, 6) NULL,
    [Storage_Used_GB]      DECIMAL (18, 6) NULL,
    [Storage_Used_TB]      DECIMAL (18, 6) NULL,
    [Storage_Allocated_MB] DECIMAL (18, 6) NULL,
    [Storage_Allocated_GB] DECIMAL (18, 6) NULL,
    [Storage_Allocated_TB] DECIMAL (18, 6) NULL,
    [Storage_Total]        DECIMAL (18, 6) NULL,
    [ReportID]             BIGINT          NULL,
    [DTADDED]              DATETIME        CONSTRAINT [DF_EPA-O365Report-SPOTenantStorageWeekly_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]                DATETIME        NULL,
    CONSTRAINT [PK_EPA-O365Report-SPOTenantStorageWeekly] PRIMARY KEY CLUSTERED ([ID] ASC)
);

