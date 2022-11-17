CREATE TABLE [dbo].[TenantSiteListing]
(
	[ID] INT NOT NULL IDENTITY, 
    [Url] VARCHAR(512) NULL, 
    [SiteType] NVARCHAR(50) NULL,
    [LastModified] DATETIME NULL, 
    [DateModified] DATETIME NULL, 
    CONSTRAINT [PK_TenantSiteList_Id] PRIMARY KEY CLUSTERED ([ID] ASC)
)
