﻿CREATE TABLE [dbo].[GraphO365ActiveUserDetails] (
    [UPN]                                    VARCHAR (255)  NOT NULL,
    [ReportRefreshDate]                      DATETIME       NOT NULL,
    [DisplayName]                            VARCHAR (500)  NULL,
    [Deleted]                                BIT            NULL,
    [DeletedDate]                            DATETIME       NULL,
    [LicenseForExchange]                     BIT            NULL,
    [LicenseForOneDrive]                     BIT            NULL,
    [LicenseForSharePoint]                   BIT            NULL,
    [LicenseForSkypeForBusiness]             BIT            NULL,
    [LicenseForYammer]                       BIT            NULL,
    [LicenseForTeams]                        BIT            CONSTRAINT [DF_GraphO365ActiveUserDetails_LicenseForTeams] DEFAULT ((0)) NULL,
    [LastActivityDateForExchange]            DATETIME       NULL,
    [LastActivityDateForOneDrive]            DATETIME       NULL,
    [LastActivityDateForSharePoint]          DATETIME       NULL,
    [LastActivityDateForSkypeForBusiness]    DATETIME       NULL,
    [LastActivityDateForYammer]              DATETIME       NULL,
    [LastActivityDateForTeams]               DATETIME       NULL,
    [LicenseAssignedDateForExchange]         DATETIME       NULL,
    [LicenseAssignedDateForOneDrive]         DATETIME       NULL,
    [LicenseAssignedDateForSharePoint]       DATETIME       NULL,
    [LicenseAssignedDateForSkypeForBusiness] DATETIME       NULL,
    [LicenseAssignedDateForYammer]           DATETIME       NULL,
    [LicenseAssignedDateForTeams]            DATETIME       NULL,
    [ProductsAssigned]                       VARCHAR (MAX) NULL,
    CONSTRAINT [PK_O365PreviewActiveUserDetails] PRIMARY KEY CLUSTERED ([UPN] ASC)
);

