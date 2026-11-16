CREATE TABLE [dbo].[organizations] (
    [organization_id]          INT              IDENTITY (1, 1) NOT NULL,
    [organization_name]        NVARCHAR (100)   NOT NULL,
    [acronym]                  NVARCHAR (10)    NULL,
    [organization_description] NVARCHAR (255)   NULL,
    [slogan]                   NVARCHAR (255)   NULL,
    [website]                  NVARCHAR (200)   NULL,
    [email]                    NVARCHAR (320)   NULL,
    [organization_address]     NVARCHAR (255)   NULL,
    [phone]                    NVARCHAR (20)    NULL,
    [is_active]                BIT              CONSTRAINT [DF_organizations_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]               DATETIME2 (3)    CONSTRAINT [DF_organizations_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]               UNIQUEIDENTIFIER NULL,
    [updated_at]               DATETIME2 (3)    NULL,
    [updated_by]               UNIQUEIDENTIFIER NULL,
    [row_version]              ROWVERSION       NOT NULL,
    [deleted_at]               DATETIME2 (3)    NULL,
    [deleted_by]               UNIQUEIDENTIFIER NULL,
    [legal_entity_id]          UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_organizations] PRIMARY KEY CLUSTERED ([organization_id] ASC)
);


GO

CREATE NONCLUSTERED INDEX [IX_organizations_active]
    ON [dbo].[organizations]([is_active] ASC)
    INCLUDE([organization_name], [acronym]);


GO

