CREATE TABLE [dbo].[refresh_tokens] (
    [token_id]          UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [token_hash]        NVARCHAR (256)   NOT NULL,
    [user_id]           UNIQUEIDENTIFIER NULL,
    [application_id]    UNIQUEIDENTIFIER NOT NULL,
    [auth_data]         NVARCHAR (MAX)   NOT NULL,
    [is_revoked]        BIT              DEFAULT ((0)) NOT NULL,
    [expires_at]        DATETIME2 (7)    NOT NULL,
    [created_at]        DATETIME2 (3)    DEFAULT (sysutcdatetime()) NOT NULL,
    [replaced_by]       UNIQUEIDENTIFIER NULL,
    [correlation_id]    NVARCHAR (100)   NULL,
    [session_id]        UNIQUEIDENTIFIER NULL,
    [app_credential_id] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_refresh_tokens] PRIMARY KEY CLUSTERED ([token_id] ASC),
    FOREIGN KEY ([app_credential_id]) REFERENCES [dbo].[application_credentials] ([app_credential_id]),
    CONSTRAINT [FK_refresh_tokens_sessions] FOREIGN KEY ([session_id]) REFERENCES [dbo].[user_sessions] ([session_id]),
    CONSTRAINT [FK_refresh_tokens_users] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [UQ_refresh_tokens_hash] UNIQUE NONCLUSTERED ([token_hash] ASC)
);


GO

CREATE NONCLUSTERED INDEX [IX_refresh_tokens_session]
    ON [dbo].[refresh_tokens]([session_id] ASC) WHERE ([session_id] IS NOT NULL);


GO

