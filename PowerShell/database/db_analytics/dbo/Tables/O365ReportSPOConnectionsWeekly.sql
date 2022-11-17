CREATE TABLE [dbo].[O365ReportSPOConnectionsWeekly] (
    [ID]           INT          IDENTITY (1, 1) NOT NULL,
    [tblDate]      DATETIME     NULL,
    [tblDateMonth] INT          NULL,
    [Month]        VARCHAR (25) NULL,
    [Year]         INT          NULL,
    [POP3]         BIGINT       NULL,
    [MAPI]         BIGINT       NULL,
    [OWA]          BIGINT       NULL,
    [EAS]          BIGINT       NULL,
    [EWS]          BIGINT       NULL,
    [IMAP]         BIGINT       NULL,
    [ReportID]     BIGINT       NULL,
    [DTADDED]      DATETIME     CONSTRAINT [DF_EPA-O365Report-SPOConnections_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]        DATETIME     NULL,
    CONSTRAINT [PK_EPA-O365Report-SPOConnections] PRIMARY KEY CLUSTERED ([ID] ASC)
);

