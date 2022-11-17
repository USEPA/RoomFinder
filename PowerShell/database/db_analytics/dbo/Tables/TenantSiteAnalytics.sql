CREATE TABLE [dbo].[TenantSiteAnalytics] (
    [ID]                   INT              IDENTITY (1, 1) NOT NULL,
    [tenantSiteId]         INT              NULL,
    [fkTenantDateId]       INT              NULL,
    [SiteGuid]             UNIQUEIDENTIFIER NULL,
    [TotalSiteUsers]       INT              NOT NULL,
    [SubSiteCount]         INT              NOT NULL,
    [TrackedSite]          BIT              NOT NULL,
    [SiteOwners]           VARCHAR (6000)   NULL,
    [Storage_Usage_MB]     DECIMAL (18, 6)  NULL,
    [Storage_Usage_GB]     DECIMAL (18, 6)  NULL,
    [Storage_Used_Perct]   DECIMAL (18, 4)  NULL,
    [Storage_Allocated_MB] DECIMAL (18, 6)  NULL,
    [Storage_Allocated_GB] DECIMAL (18, 6)  NULL,
    [DTMETRIC]             DATETIME         NOT NULL,
    [ScanCompleted]        BIT              CONSTRAINT [DF_TenantSiteAnalytics_ScanCompleted] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_TenantSiteAnalytics] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_TenantSiteAnalytics_TenantDates] FOREIGN KEY ([fkTenantDateId]) REFERENCES [dbo].[TenantDates] ([ID]),
    CONSTRAINT [FK_TenantSiteAnalytics_TenantSite] FOREIGN KEY ([tenantSiteId]) REFERENCES [dbo].[TenantSite] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_TenantSiteAnalytics]
    ON [dbo].[TenantSiteAnalytics]([SiteGuid] ASC);

