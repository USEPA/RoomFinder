CREATE TABLE [dbo].[AnalyticsUserProfilesSubsites]
(
	[ID] INT NOT NULL IDENTITY, 
    [Title] VARCHAR(255) NULL, 
    [SiteOwner] VARCHAR(255) NULL, 
    [SiteTemplate] VARCHAR(100) NULL, 
    [ListCount] INT NULL, 
    [Url] VARCHAR(255) NULL, 
    [DateCreated] DATETIME NOT NULL, 
    [Exception] BIT NOT NULL DEFAULT 0, 
    [SiteOwnerComment] NVARCHAR(1024) NULL, 
    [AdminComment] NVARCHAR(1024) NULL, 
    [LastModified] DATETIME NULL, 
    [Status] NVARCHAR(100) NULL, 
    [FlaggedCount] INT NULL, 
    [NotificationCount] INT NULL, 
    [FirstNotification] DATETIME NULL, 
    [SecondNotification] DATETIME NULL, 
    [LastNotification] DATETIME NULL, 
    [LastExpiredNotification] DATETIME NULL, 
    CONSTRAINT [PK_AnalyticsUserProfilesSubsites_Id] PRIMARY KEY CLUSTERED ([ID] ASC)
)
