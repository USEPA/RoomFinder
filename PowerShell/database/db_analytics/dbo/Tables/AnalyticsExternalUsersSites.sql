CREATE TABLE [dbo].[AnalyticsExternalUsersSites] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [exID]           INT            NULL,
    [Site]           NVARCHAR (255) NULL,
    [Region]         NVARCHAR (255) NULL,
    [SiteType]       NVARCHAR (255) NULL,
    [ExternalUserID] NVARCHAR (100) NULL,
    [LoginName]      NVARCHAR (255) NULL,
    [DisplayName]    NVARCHAR (255) NULL,
    [EmailAddress]   NVARCHAR (255) NULL,
    [CreatedDate]    DATETIME       NULL,
    [DTADDED]        DATETIME       NULL,
    [DTUPD]          DATETIME       NULL,
    CONSTRAINT [PK_EPAAnalyticsExternalUsersSites] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_EPAAnalyticsExternalUsersSites_EPAAnalyticsExternalUsers] FOREIGN KEY ([exID]) REFERENCES [dbo].[AnalyticsExternalUsers] ([ID])
);

