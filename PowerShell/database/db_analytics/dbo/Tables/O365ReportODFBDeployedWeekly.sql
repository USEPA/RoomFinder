CREATE TABLE [dbo].[O365ReportODFBDeployedWeekly] (
    [ID]           INT           IDENTITY (1, 1) NOT NULL,
    [tblDate]      DATETIME      NULL,
    [tblDateMonth] INT           NULL,
    [Month]        VARCHAR (255) NULL,
    [Year]         INT           NULL,
    [Active]       BIGINT        NULL,
    [Inactive]     BIGINT        NULL,
    [ReportID]     BIGINT        NULL,
    [DTADDED]      DATETIME      CONSTRAINT [DF_O365Report_ODFBDeployedWeekly_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]        DATETIME      NULL,
    CONSTRAINT [PK_O365Report_ODFBDeployedWeekly] PRIMARY KEY CLUSTERED ([ID] ASC)
);

