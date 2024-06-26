﻿CREATE TABLE [dbo].[TenantWeb] (
    [ID]                        INT              IDENTITY (1, 1) NOT NULL,
    [tenantSiteId]              INT              NULL,
    [WebGuid]                   UNIQUEIDENTIFIER NULL,
    [WebUrl]                    NVARCHAR (255)   NULL,
    [WebTitle]                  NVARCHAR (255)   NULL,
    [SiteTemplateId]            VARCHAR (100)    NULL,
    [SiteCreatedDate]           DATETIME         NULL,
    [SiteLastModified]          DATETIME         NULL,
    [SiteIsAddIn]               BIT              NOT NULL,
    [SiteActivity]              DECIMAL (18, 6)  NULL,
    [SiteOwners]                VARCHAR (MAX)    NULL,
    [SiteMetadata]              BIT              NOT NULL,
    [SiteMetadataCount]         INT              NOT NULL,
    [SiteMetadataPermissions]   BIT              NOT NULL,
    [SiteMasterPage]            BIT              NOT NULL,
    [PermUnique]                BIT              CONSTRAINT [DF_TenantWeb_PermUnique] DEFAULT ((0)) NOT NULL,
    [PermAssociatedOwner]       BIT              CONSTRAINT [DF_TenantWeb_PermAssociatedOwner] DEFAULT ((0)) NOT NULL,
    [PermAssociatedMember]      BIT              CONSTRAINT [DF_TenantWeb_PermAssociatedMember] DEFAULT ((0)) NOT NULL,
    [PermAssociatedVisitor]     BIT              CONSTRAINT [DF_TenantWeb_PermAssociatedVisitor] DEFAULT ((0)) NOT NULL,
    [WelcomePage]               VARCHAR (255)    NULL,
    [WelcomePageError]          BIT              CONSTRAINT [DF_TenantWeb_WelcomePageError] DEFAULT ((0)) NOT NULL,
    [Total_Lists_Calculated]    INT              CONSTRAINT [DF_TenantWeb_Total_Lists_Calculated] DEFAULT ((0)) NOT NULL,
    [Total_Pages_Json]          VARCHAR (MAX)    NULL,
    [Total_URLs_Json]           NVARCHAR (4000)  NULL,
    [Total_Hits]                BIGINT           NULL,
    [Total_UniqueVisitors]      BIGINT           NULL,
    [Total_Hits_HomePage]       BIGINT           NULL,
    [Unique_Visitors_Home_Page] BIGINT           NULL,
    [DocumentActivityStatus]    DECIMAL (18, 6)  NULL,
    [DocumentCount]             BIGINT           NULL,
    [DocumentLastEditedDate]    DATETIME         NULL,
    [HasCommunity]              BIT              NOT NULL,
    [MemberJoinCount]           BIGINT           NOT NULL,
    [MemberJoinLastDate]        DATETIME         NULL,
    [MembersCanShare]           BIT              CONSTRAINT [DF_TenantWeb_MembersCanShare] DEFAULT ((0)) NOT NULL,
    [DiscussionCount]           BIGINT           NOT NULL,
    [DiscussionLastDate]        DATETIME         NULL,
    [DiscussionReplyCount]      BIGINT           NOT NULL,
    [DiscussionReplyLastDate]   DATETIME         NULL,
    [DiscussionJSON]            VARCHAR (MAX)    NULL,
    [DTADDED]                   DATETIME         NOT NULL,
    [DTUPD]                     DATETIME         NULL,
    CONSTRAINT [PK_TenantWeb] PRIMARY KEY CLUSTERED ([ID] ASC)
);

