CREATE TABLE [dbo].[GraphSkypeForBusinessDeviceUsageUserDetail] (
    [UPN]                      NVARCHAR (128) NOT NULL,
    [LastActivityDate]         DATETIME       NOT NULL,
    [UsedWindows]              BIT            CONSTRAINT [DF_GraphSkypeForBusinessDeviceUsageUserDetail_UsedWindows] DEFAULT ((0)) NOT NULL,
    [UsedWindowsLastDate]      DATETIME       NULL,
    [UsedWindowsPhone]         BIT            CONSTRAINT [DF_GraphSkypeForBusinessDeviceUsageUserDetail_UsedWindowsPhone] DEFAULT ((0)) NOT NULL,
    [UsedWindowsPhoneLastDate] DATETIME       NULL,
    [UsedAndroidPhone]         BIT            CONSTRAINT [DF_GraphSkypeForBusinessDeviceUsageUserDetail_UsedAndroidPhone] DEFAULT ((0)) NOT NULL,
    [UsedAndroidPhoneLastDate] DATETIME       NULL,
    [UsediPhone]               BIT            CONSTRAINT [DF_GraphSkypeForBusinessDeviceUsageUserDetail_UsediPhone] DEFAULT ((0)) NOT NULL,
    [UsediPhoneLastDate]       DATETIME       NULL,
    [UsediPad]                 BIT            CONSTRAINT [DF_GraphSkypeForBusinessDeviceUsageUserDetail_UsediPad] DEFAULT ((0)) NOT NULL,
    [UsediPadLastDate]         DATETIME       NULL,
    [ReportRefreshDate]        DATETIME       NOT NULL,
    [ReportPeriod]             INT            NOT NULL,
    CONSTRAINT [PK_dbo.SkypeForBusinessDeviceUsageUserDetail] PRIMARY KEY CLUSTERED ([UPN] ASC)
);

