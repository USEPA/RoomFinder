CREATE TABLE [dbo].[GraphSharePointSiteUsageDetail] (
    [LastActivityDate]      DATETIME      NOT NULL,
    [SiteURL]               VARCHAR (255) NOT NULL,
    [OwnerDisplayName]      VARCHAR (255) NULL,
    [IsDeleted]             BIT           NULL,
    [FileCount]             BIGINT        NULL,
    [ActiveFileCount]       BIGINT        NULL,
    [PageViewCount]         BIGINT        NULL,
    [VisitedPageCount]      BIGINT        NULL,
    [StorageUsed_Byte]      BIGINT        NULL,
    [StorageAllocated_Byte] BIGINT        NULL,
    [RootWebTemplate]       VARCHAR (125) NULL,
    [ReportPeriod]          INT           NULL,
    [ReportRefreshDate]     DATETIME      NULL,
    CONSTRAINT [PK_GraphSharePointSiteUsageDetail] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC, [SiteURL] ASC)
);

