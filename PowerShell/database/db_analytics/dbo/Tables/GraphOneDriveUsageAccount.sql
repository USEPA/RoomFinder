CREATE TABLE [dbo].[GraphOneDriveUsageAccount] (
    [LastActivityDate]      DATETIME     NOT NULL,
    [SiteType]              VARCHAR (50) NOT NULL,
    [ReportingPeriodInDays] INT          NULL,
    [Total_Accounts]        BIGINT       NULL,
    [Total_ActiveAccounts]  BIGINT       NULL,
    CONSTRAINT [PK_PreviewOneDriveUsageAccount_1] PRIMARY KEY CLUSTERED ([LastActivityDate] ASC, [SiteType] ASC)
);

