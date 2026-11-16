CREATE TABLE [dbo].[user_roles] (
    [user_role_id]          INT              IDENTITY (1, 1) NOT NULL,
    [user_application_id]   UNIQUEIDENTIFIER NOT NULL,
    [role_id]               INT              NOT NULL,
    [user_role_description] NVARCHAR (500)   NULL,
    [is_active]             BIT              CONSTRAINT [DF_user_roles_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]            DATETIME2 (3)    CONSTRAINT [DF_user_roles_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]            UNIQUEIDENTIFIER NULL,
    [updated_at]            DATETIME2 (3)    NULL,
    [updated_by]            UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_user_roles] PRIMARY KEY CLUSTERED ([user_role_id] ASC),
    CONSTRAINT [FK_user_roles_roles] FOREIGN KEY ([role_id]) REFERENCES [dbo].[roles] ([role_id]),
    CONSTRAINT [FK_user_roles_user_apps] FOREIGN KEY ([user_application_id]) REFERENCES [dbo].[user_applications] ([user_application_id])
);


GO

