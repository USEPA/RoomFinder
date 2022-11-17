CREATE TABLE [dbo].[O365ReportSPOClientSoftwareBrowser] (
    [ID]             INT           IDENTITY (1, 1) NOT NULL,
    [tblDate]        DATETIME      NULL,
    [tblDateMonth]   INT           NULL,
    [Month]          VARCHAR (25)  NULL,
    [Year]           INT           NULL,
    [TotalCount]     BIGINT        NULL,
    [DisplayName]    VARCHAR (255) NULL,
    [LastAccessTime] DATETIME      NULL,
    [ObjectId]       VARCHAR (100) NULL,
    [Username]       VARCHAR (255) NULL,
    [ClientName]     VARCHAR (255) NULL,
    [ClientVersion]  VARCHAR (255) NULL,
    [DTADDED]        DATETIME      CONSTRAINT [DF_EPA-O365Report-SPOClientSoftwareBrowser_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]          DATETIME      NULL,
    CONSTRAINT [PK_EPA-O365Report-SPOClientSoftwareBrowser] PRIMARY KEY CLUSTERED ([ID] ASC)
);

