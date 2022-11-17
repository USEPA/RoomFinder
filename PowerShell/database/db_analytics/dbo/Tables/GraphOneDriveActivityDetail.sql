CREATE TABLE [dbo].[GraphOneDriveActivityDetail] (
    [LastActivityDate]            DATETIME       NOT NULL,
    [UPN]                         NVARCHAR (255) NOT NULL,
    [ReportingPeriodInDays]       INT            NULL,
    [ProductsAssigned]            VARCHAR (MAX)  NULL,
    [Deleted]                     VARCHAR (50)   NULL,
    [DeletedDate]                 DATETIME       NULL,
    [ODB_TotalofAllActivities]    BIGINT         NULL,
    [ODB_CollaboratedByOthers]    BIGINT         NULL,
    [ODB_CollaboratedByOwner]     BIGINT         NULL,
    [ODB_TotalFileSharedEXT]      BIGINT         NULL,
    [ODB_TotalFileSharedINT]      BIGINT         NULL,
    [ODB_TotalFileSynched]        BIGINT         NULL,
    [ODB_TotalFileViewedModified] BIGINT         NULL,
    CONSTRAINT [PK_PreviewOneDriveActivityDetail_1] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC, [UPN] ASC)
);

