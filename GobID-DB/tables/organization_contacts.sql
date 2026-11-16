CREATE TABLE [dbo].[organization_contacts] (
    [contact_id]                INT              IDENTITY (1, 1) NOT NULL,
    [organization_id]           INT              NOT NULL,
    [organization_contact_name] NVARCHAR (100)   NOT NULL,
    [position]                  NVARCHAR (100)   NULL,
    [email]                     NVARCHAR (320)   NULL,
    [phone]                     NVARCHAR (20)    NULL,
    [is_active]                 BIT              CONSTRAINT [DF_org_contacts_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]                DATETIME2 (3)    CONSTRAINT [DF_org_contacts_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]                UNIQUEIDENTIFIER NULL,
    [updated_at]                DATETIME2 (3)    NULL,
    [updated_by]                UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_organization_contacts] PRIMARY KEY CLUSTERED ([contact_id] ASC),
    CONSTRAINT [FK_org_contacts_organizations] FOREIGN KEY ([organization_id]) REFERENCES [dbo].[organizations] ([organization_id])
);


GO

