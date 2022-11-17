CREATE TABLE [dbo].[eBusinessAccounts] (
    [ID]                     INT           IDENTITY (1, 1) NOT NULL,
    [OrgNumCode]             VARCHAR (10)  NULL,
    [OrgAcronym]             VARCHAR (10)  NULL,
    [OfficeCode]             VARCHAR (50)  NULL,
    [OfficeName]             VARCHAR (50)  NULL,
    [OrgOffice]              VARCHAR (50)  NULL,
    [OrgOfficeBranch]        VARCHAR (50)  NULL,
    [OrgOfficeBranchSection] VARCHAR (50)  NULL,
    [OrgOfficeBranchUnit]    VARCHAR (50)  NULL,
    [UserGivenNamePreferred] VARCHAR (75)  NULL,
    [UserGivenName]          VARCHAR (75)  NULL,
    [UserMiddleInitial]      VARCHAR (10)  NULL,
    [UserSurName]            VARCHAR (75)  NULL,
    [AffiliationCode]        VARCHAR (10)  NULL,
    [UserEmailAddress]       VARCHAR (255) NULL,
    [UserWorkforceID]        VARCHAR (25)  NULL,
    [UserSamAccountName]     VARCHAR (125) NULL,
    [UserEnabled]            BIT           NULL,
    [UserIsContractor]       BIT           CONSTRAINT [DF_eBusinessAccounts_UserIsContractor] DEFAULT ((0)) NOT NULL,
    [UserBuilding]           VARCHAR (250) NULL,
    [UserBuildingRoomNo]     VARCHAR (150) NULL,
    [UserAddressLine1]       VARCHAR (175) NULL,
    [UserAddressCity]        VARCHAR (75)  NULL,
    [UserAddressState]       VARCHAR (75)  NULL,
    [UserAddressZipCode]     VARCHAR (15)  NULL,
    [DT_IMPORT]              DATETIME      NULL,
    [DT_DEACTIVATED]         DATETIME      NULL,
    [IsDeactivated]          BIT           CONSTRAINT [DF_eBusinessAccounts_IsDeactivated] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_eBusinessAccounts] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_eBusinessAccountsEmailAddress]
    ON [dbo].[eBusinessAccounts]([UserEmailAddress] ASC);

