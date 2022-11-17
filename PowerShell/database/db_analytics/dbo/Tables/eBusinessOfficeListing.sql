CREATE TABLE [dbo].[eBusinessOfficeListing] (
    [ID]              INT           IDENTITY (1, 1) NOT NULL,
    [Title]           VARCHAR (255) NULL,
    [OrgName]         VARCHAR (255) NULL,
    [OrgCode]         VARCHAR (250) NULL,
    [AlphaCode]       VARCHAR (250) NULL,
    [ReportsTo]       VARCHAR (250) NULL,
    [CurrentRow]      BIT           CONSTRAINT [DF_OfficeListing_CurrentRow] DEFAULT ((0)) NOT NULL,
    [ChangeFromRowId] INT           NULL,
    CONSTRAINT [PK_OfficeListing] PRIMARY KEY CLUSTERED ([ID] ASC)
);

