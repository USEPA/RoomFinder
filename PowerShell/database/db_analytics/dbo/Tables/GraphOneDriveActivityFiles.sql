CREATE TABLE [dbo].[GraphOneDriveActivityFiles] (
    [LastActivityDate]            DATETIME NOT NULL,
    [ReportingPeriodInDays]       INT      NULL,
    [ODB_TotalFileViewedModified] BIGINT   NULL,
    [ODB_TotalFileSynched]        BIGINT   NULL,
    [ODB_TotalFileSharedEXT]      BIGINT   NULL,
    [ODB_TotalFileSharedINT]      BIGINT   NULL,
    CONSTRAINT [PK_PreviewOneDriveActivityFiles_1] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC)
);

