CREATE TABLE [dbo].[AnalyticsExternalUsers] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [ExternalUserID] NVARCHAR (100) NULL,
    [InvitedAs]      VARCHAR (250)  NULL,
    [AcceptedAs]     VARCHAR (255)  NULL,
    [DisplayName]    NVARCHAR (255) NULL,
    [EmailAddress]   NVARCHAR (255) NULL,
    [CreatedDate]    DATETIME       NULL,
    [AcceptedDate]   DATETIME       NULL,
    [DTADDED]        DATETIME       CONSTRAINT [DF_EPA-Analytics-ExternalUsers_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]          DATETIME       NULL,
    CONSTRAINT [PK_EPA-Analytics-ExternalUsers] PRIMARY KEY CLUSTERED ([ID] ASC)
);

