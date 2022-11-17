CREATE TABLE [dbo].[TenantSite] (
    [ID]       INT              IDENTITY (1, 1) NOT NULL,
    [SiteGuid] UNIQUEIDENTIFIER NOT NULL,
    [SiteUrl]  NVARCHAR (255)   NOT NULL,
    [Region]   NVARCHAR (255)   NULL,
    [SiteType] NVARCHAR (255)   NULL,
    [DTADDED]  DATETIME         NOT NULL,
    [DTUPD]    DATETIME         NULL,
    CONSTRAINT [PK_TenantSite] PRIMARY KEY CLUSTERED ([ID] ASC)
);

