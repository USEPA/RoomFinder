CREATE TABLE [dbo].[O365ReportSPOConferencesWeekly] (
    [ID]                 INT          IDENTITY (1, 1) NOT NULL,
    [tblDate]            DATETIME     NULL,
    [tblDateMonth]       INT          NULL,
    [Month]              VARCHAR (25) NULL,
    [Year]               INT          NULL,
    [ApplicationSharing] BIGINT       NULL,
    [AudioVisual]        BIGINT       NULL,
    [InstantMessaging]   BIGINT       NULL,
    [Telephony]          BIGINT       NULL,
    [TotalConferences]   BIGINT       NULL,
    [WebConferences]     BIGINT       NULL,
    [ReportID]           BIGINT       NULL,
    [DTADDED]            DATETIME     CONSTRAINT [DF_EPA-O365Report-SPOConferences_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]              DATETIME     NULL,
    CONSTRAINT [PK_EPA-O365Report-SPOConferences] PRIMARY KEY CLUSTERED ([ID] ASC)
);

