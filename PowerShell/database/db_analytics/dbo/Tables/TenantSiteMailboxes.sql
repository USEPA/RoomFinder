CREATE TABLE [dbo].[TenantSiteMailboxes]
(
	[ID] INT NOT NULL IDENTITY, 
    [ParentSiteUrl] VARCHAR(512) NULL, 
    [SiteOwnerEmail] VARCHAR(1024) NULL, 
    [MailboxAddresses] VARCHAR(512) NULL, 
    [UserName] VARCHAR(255) NULL
    CONSTRAINT [PK_TenantSiteMailboxes_ID] PRIMARY KEY CLUSTERED ([ID] ASC), 
    [DateRemoved] DATETIME NULL
)
