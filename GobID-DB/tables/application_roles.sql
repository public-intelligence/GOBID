CREATE TABLE [dbo].[application_roles] (
    [application_role_id]         INT              IDENTITY (1, 1) NOT NULL,
    [organization_application_id] INT              NOT NULL,
    [role_id]                     INT              NOT NULL,
    [description]                 NVARCHAR (500)   NULL,
    [is_active]                   BIT              DEFAULT ((1)) NOT NULL,
    [created_at]                  DATETIME2 (3)    DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]                  UNIQUEIDENTIFIER NULL,
    [updated_at]                  DATETIME2 (3)    NULL,
    [updated_by]                  UNIQUEIDENTIFIER NULL,
    [app_credential_id]           UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY CLUSTERED ([application_role_id] ASC),
    FOREIGN KEY ([organization_application_id]) REFERENCES [dbo].[organization_applications] ([organization_application_id]),
    FOREIGN KEY ([role_id]) REFERENCES [dbo].[roles] ([role_id]),
    CONSTRAINT [FK_app_roles_app_creds] FOREIGN KEY ([app_credential_id]) REFERENCES [dbo].[application_credentials] ([app_credential_id])
);


GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_app_roles]
    ON [dbo].[application_roles]([app_credential_id] ASC, [organization_application_id] ASC, [role_id] ASC);


GO

