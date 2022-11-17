CREATE TABLE [dbo].[O365ReportSPOActiveUsersWeekly] (
    [ID]               INT          IDENTITY (1, 1) NOT NULL,
    [tblDate]          DATETIME     NULL,
    [tblDateMonth]     INT          NULL,
    [Month]            VARCHAR (25) NULL,
    [Year]             INT          NULL,
    [UniqueUsers]      BIGINT       NULL,
    [LicenseAssigned]  BIGINT       NULL,
    [LicensesAcquired] BIGINT       NULL,
    [TotalUsers]       BIGINT       NULL,
    [ReportID]         BIGINT       NULL,
    [DTADDED]          DATETIME     CONSTRAINT [DF_EPA-O365Report-SPOActiveUsersWeekly_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]            DATETIME     NULL,
    CONSTRAINT [PK_EPA-O365Report-SPOActiveUsersWeekly] PRIMARY KEY CLUSTERED ([ID] ASC)
);

