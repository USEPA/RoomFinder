CREATE TABLE [etl].[ODFBStorageListing] (
    [ID]           INT             IDENTITY (1, 1) NOT NULL,
    [tblDate]      DATETIME        NULL,
    [tblDateMonth] INT             NULL,
    [Month]        VARCHAR (25)    NULL,
    [Year]         INT             NULL,
    [Storage_Mb]   DECIMAL (18, 6) NULL,
    [Storage_Gb]   DECIMAL (18, 6) NULL,
    [Storage_Tb]   DECIMAL (18, 6) NULL,
    [Allocated_Mb] DECIMAL (18, 6) NULL,
    [Allocated_Gb] DECIMAL (18, 6) NULL,
    [Allocated_Tb] DECIMAL (18, 6) NULL,
    [DTADDED]      DATETIME        CONSTRAINT [DF_ODFB-Storage-Listing_DTADDED] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ODFB-Storage-Listing] PRIMARY KEY CLUSTERED ([ID] ASC)
);

