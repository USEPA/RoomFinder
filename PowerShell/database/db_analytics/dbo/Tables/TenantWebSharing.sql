CREATE TABLE [dbo].[TenantWebSharing] (
    [ID]             INT              IDENTITY (1, 1) NOT NULL,
    [WebGuid]        UNIQUEIDENTIFIER NULL,
    [WebUrl]         NVARCHAR (255)   NULL,
    [WebTitle]       NVARCHAR (255)   NULL,
    [Region]         NVARCHAR (100)   NULL,
    [SiteType]       NVARCHAR (100)   NULL,
    [SiteTemplateId] NVARCHAR (100)   NULL,
    [CanMemberShare] BIT              CONSTRAINT [DF_TenantWebSharing_CanMemberShare] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_TenantWebSharing] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_TenantWebSharing]
    ON [dbo].[TenantWebSharing]([WebGuid] ASC);

