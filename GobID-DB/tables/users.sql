CREATE TABLE [dbo].[users] (
    [user_id]                  UNIQUEIDENTIFIER CONSTRAINT [DF__users__user_id__395884C4] DEFAULT (newid()) NOT NULL,
    [personnel_id]             UNIQUEIDENTIFIER NULL,
    [email]                    NVARCHAR (320)   NULL,
    [user_password]            NVARCHAR (128)   NULL,
    [password_reset_token]     NVARCHAR (255)   NULL,
    [must_change_password]     BIT              CONSTRAINT [DF_users_change_pwd] DEFAULT ((0)) NOT NULL,
    [admission_date]           DATE             NULL,
    [discharge_date]           DATE             NULL,
    [created_at]               DATETIME2 (3)    CONSTRAINT [DF_users_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]               UNIQUEIDENTIFIER NULL,
    [updated_at]               DATETIME2 (3)    NULL,
    [updated_by]               UNIQUEIDENTIFIER NULL,
    [row_version]              ROWVERSION       NOT NULL,
    [is_active]                BIT              DEFAULT ((1)) NOT NULL,
    [deleted_at]               DATETIME2 (3)    NULL,
    [deleted_by]               UNIQUEIDENTIFIER NULL,
    [email_confirmed]          BIT              DEFAULT ((0)) NOT NULL,
    [email_confirmation_token] NVARCHAR (2048)  NULL,
    [is_external]              BIT              DEFAULT ((0)) NOT NULL,
    [person_position_id]       UNIQUEIDENTIFIER NULL,
    [phone_number]             NCHAR (13)       NULL,
    CONSTRAINT [PK_users] PRIMARY KEY CLUSTERED ([user_id] ASC),
    CONSTRAINT [FK_users_personnel_person_position_id] FOREIGN KEY ([person_position_id]) REFERENCES [dbo].[personnel] ([personnel_id])
);


GO

CREATE NONCLUSTERED INDEX [IX_users_person_position_id]
    ON [dbo].[users]([person_position_id] ASC);


GO

