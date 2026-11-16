CREATE TABLE [dbo].[authorization_codes] (
    [code_id]        UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [code_hash]      NVARCHAR (256)   NOT NULL,
    [user_id]        UNIQUEIDENTIFIER NOT NULL,
    [application_id] UNIQUEIDENTIFIER NOT NULL,
    [redirect_uri]   NVARCHAR (500)   NOT NULL,
    [auth_data]      NVARCHAR (MAX)   NOT NULL,
    [is_used]        BIT              DEFAULT ((0)) NOT NULL,
    [expires_at]     DATETIME2 (7)    NOT NULL,
    [created_at]     DATETIME2 (3)    DEFAULT (sysutcdatetime()) NOT NULL,
    CONSTRAINT [PK_authorization_codes] PRIMARY KEY CLUSTERED ([code_id] ASC),
    CONSTRAINT [FK_auth_codes_users] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [UQ_authorization_codes_hash] UNIQUE NONCLUSTERED ([code_hash] ASC)
);


GO

