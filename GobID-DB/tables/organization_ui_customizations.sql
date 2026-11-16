CREATE TABLE [dbo].[organization_ui_customizations] (
    [ui_custom_id]        INT              IDENTITY (1, 1) NOT NULL,
    [organization_id]     INT              NOT NULL,
    [customization_name]  NVARCHAR (100)   NOT NULL,
    [customization_value] NVARCHAR (255)   NULL,
    [is_active]           BIT              CONSTRAINT [DF_org_ui_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]          DATETIME2 (3)    CONSTRAINT [DF_org_ui_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]          UNIQUEIDENTIFIER NULL,
    [updated_at]          DATETIME2 (3)    NULL,
    [updated_by]          UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_organization_ui_customizations] PRIMARY KEY CLUSTERED ([ui_custom_id] ASC),
    CONSTRAINT [FK_org_ui_organizations] FOREIGN KEY ([organization_id]) REFERENCES [dbo].[organizations] ([organization_id])
);


GO

