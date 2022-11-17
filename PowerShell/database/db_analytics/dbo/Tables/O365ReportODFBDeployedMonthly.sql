CREATE TABLE [dbo].[O365ReportODFBDeployedMonthly] (
    [ID]           INT           IDENTITY (1, 1) NOT NULL,
    [tblDate]      DATETIME      NULL,
    [tblDateMonth] INT           NULL,
    [Month]        VARCHAR (255) NULL,
    [Year]         INT           NULL,
    [Active]       BIGINT        NULL,
    [Inactive]     BIGINT        NULL,
    [ReportID]     BIGINT        NULL,
    [DTADDED]      DATETIME      CONSTRAINT [DF_O365Report_ODFBDeployedMonthly_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]        DATETIME      NULL,
    CONSTRAINT [PK_O365Report_ODFBDeployedMonthly] PRIMARY KEY CLUSTERED ([ID] ASC)
);

