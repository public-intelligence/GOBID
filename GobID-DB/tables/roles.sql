CREATE TABLE [dbo].[roles] (
    [role_id]                     INT              IDENTITY (1, 1) NOT NULL,
    [role_name]                   NVARCHAR (255)   NOT NULL,
    [role_description]            NVARCHAR (500)   NULL,
    [set_automatic_claims]        BIT              CONSTRAINT [DF_roles_auto_claims] DEFAULT ((0)) NOT NULL,
    [organization_application_id] INT              NOT NULL,
    [is_active]                   BIT              CONSTRAINT [DF_roles_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]                  DATETIME2 (3)    CONSTRAINT [DF_roles_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]                  UNIQUEIDENTIFIER NULL,
    [updated_at]                  DATETIME2 (3)    NULL,
    [updated_by]                  UNIQUEIDENTIFIER NULL,
    [is_system_role]              BIT              DEFAULT ((0)) NOT NULL,
    [position_id]                 UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_roles] PRIMARY KEY CLUSTERED ([role_id] ASC),
    CONSTRAINT [FK_roles_org_apps] FOREIGN KEY ([organization_application_id]) REFERENCES [dbo].[organization_applications] ([organization_application_id])
);


GO

CREATE NONCLUSTERED INDEX [IX_roles_position_id]
    ON [dbo].[roles]([position_id] ASC);


GO

