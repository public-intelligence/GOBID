CREATE TABLE [dbo].[application_credentials] (
    [app_credential_id]        UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [application_id]           UNIQUEIDENTIFIER NOT NULL,
    [client_secret_hash]       NVARCHAR (100)   NOT NULL,
    [description]              NVARCHAR (200)   NULL,
    [is_active]                BIT              DEFAULT ((1)) NOT NULL,
    [created_at]               DATETIME2 (3)    DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]               UNIQUEIDENTIFIER NULL,
    [expires_at]               DATETIME2 (3)    NULL,
    [last_used_at]             DATETIME2 (3)    NULL,
    [revoked_at]               DATETIME2 (3)    NULL,
    [revoked_by]               UNIQUEIDENTIFIER NULL,
    [rotated_by_credential_id] UNIQUEIDENTIFIER NULL,
    PRIMARY KEY CLUSTERED ([app_credential_id] ASC),
    FOREIGN KEY ([application_id]) REFERENCES [dbo].[applications] ([application_id])
);


GO

CREATE NONCLUSTERED INDEX [IX_app_credentials_app_active]
    ON [dbo].[application_credentials]([application_id] ASC, [is_active] ASC);


GO

