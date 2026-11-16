CREATE TABLE [dbo].[user_sessions] (
    [session_id]          UNIQUEIDENTIFIER CONSTRAINT [DF_user_sessions_id] DEFAULT (newsequentialid()) NOT NULL,
    [user_id]             UNIQUEIDENTIFIER NULL,
    [application_id]      UNIQUEIDENTIFIER NOT NULL,
    [ip]                  NVARCHAR (45)    NULL,
    [user_agent]          NVARCHAR (512)   NULL,
    [is_trusted]          BIT              CONSTRAINT [DF_user_sessions_trusted] DEFAULT ((0)) NOT NULL,
    [is_active]           BIT              CONSTRAINT [DF_user_sessions_active] DEFAULT ((1)) NOT NULL,
    [created_at]          DATETIME2 (3)    CONSTRAINT [DF_user_sessions_created] DEFAULT (sysutcdatetime()) NOT NULL,
    [last_activity_at]    DATETIME2 (3)    NULL,
    [absolute_expires_at] DATETIME2 (3)    NOT NULL,
    [revoked_at]          DATETIME2 (3)    NULL,
    [revoked_by_ip]       NVARCHAR (45)    NULL,
    [app_credential_id]   UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_user_sessions] PRIMARY KEY CLUSTERED ([session_id] ASC),
    FOREIGN KEY ([app_credential_id]) REFERENCES [dbo].[application_credentials] ([app_credential_id]),
    CONSTRAINT [FK_user_sessions_users] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id])
);


GO

CREATE NONCLUSTERED INDEX [IX_user_sessions_user_active]
    ON [dbo].[user_sessions]([user_id] ASC, [application_id] ASC, [is_active] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_user_sessions_expires]
    ON [dbo].[user_sessions]([absolute_expires_at] ASC) WHERE ([is_active]=(1));


GO

