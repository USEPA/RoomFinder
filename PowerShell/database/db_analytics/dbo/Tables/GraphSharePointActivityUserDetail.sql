CREATE TABLE [dbo].[GraphSharePointActivityUserDetail] (
    [LastActivityDate]          DATETIME       NOT NULL,
    [UserPrincipalName]         VARCHAR (255)  NOT NULL,
    [IsDeleted]                 BIT            NULL,
    [DeletedDate]               DATETIME       NULL,
    [ViewedOrEditedFileCount]   BIGINT         NULL,
    [SyncedFileCount]           BIGINT         NULL,
    [SharedInternallyFileCount] BIGINT         NULL,
    [SharedExternallyFileCount] BIGINT         NULL,
    [VisitedPageCount]          BIGINT         NULL,
    [ProductsAssigned]          VARCHAR (MAX) NULL,
    [ReportPeriod]              INT            NULL,
    [ReportRefreshDate]         DATETIME       NULL,
    CONSTRAINT [PK_GraphSharePointActivityUserDetail] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC, [UserPrincipalName] ASC)
);

