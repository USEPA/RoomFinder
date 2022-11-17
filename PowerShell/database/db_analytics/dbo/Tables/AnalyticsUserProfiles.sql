CREATE TABLE [dbo].[AnalyticsUserProfiles] (
    [ID]                  INT             IDENTITY (1, 1) NOT NULL,
    [Username]            NVARCHAR (255)  NOT NULL,
    [ODFBSite]            NVARCHAR (255)  NULL,
    [ODFBSiteProvisioned] BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_ODFBSiteProvisioned] DEFAULT ((0)) NOT NULL,
    [ODFBSiteHits]        BIGINT          NULL,
    [ODFBSiteVisits]      BIGINT          NULL,
    [ExternalUserFlag]    BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_ExternalUserFlag] DEFAULT ((0)) NOT NULL,
    [WorkPhone]           BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_WorkPhone] DEFAULT ((0)) NOT NULL,
    [ZipCode]             BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_ZipCode] DEFAULT ((0)) NOT NULL,
    [Manager]             BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_Manager] DEFAULT ((0)) NOT NULL,
    [Office]              BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_Office] DEFAULT ((0)) NOT NULL,
    [OfficeName]          VARCHAR (255)   NULL,
    [OfficeP1]            VARCHAR (100)   NULL,
    [OfficeP2]            VARCHAR (100)   NULL,
    [OfficeP3]            VARCHAR (100)   NULL,
    [OfficeP4]            VARCHAR (100)   NULL,
    [ProfilePicture]      BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_ProfilePicture] DEFAULT ((0)) NOT NULL,
    [AboutMe]             BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_AboutMe] DEFAULT ((0)) NOT NULL,
    [Skills]              BIT             CONSTRAINT [DF_EPA-Analytics-UserProfiles_Skills] DEFAULT ((0)) NOT NULL,
    [TotalFiles]          BIGINT          CONSTRAINT [DF_EPA-Analytics-UserProfiles_TotalFiles] DEFAULT ((0)) NOT NULL,
    [Storage_Allocated]   DECIMAL (18, 6) NULL,
    [Storage_Used]        DECIMAL (18, 6) NULL,
    [Storage_Used_Perct]  DECIMAL (18, 4) NULL,
    [DTADDED]             DATETIME        CONSTRAINT [DF_EPA-Analytics-UserProfiles_DTADDED] DEFAULT (getdate()) NOT NULL,
    [DTUPD]               DATETIME        NULL,
    CONSTRAINT [PK_EPA-Analytics-UserProfiles] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_EPA-Analytics-UserProfiles-Username]
    ON [dbo].[AnalyticsUserProfiles]([Username] ASC);

