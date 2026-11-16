CREATE TABLE [dbo].[user_application_policies] (
    [user_application_policy_id] UNIQUEIDENTIFIER NOT NULL,
    [user_application_id]        UNIQUEIDENTIFIER NOT NULL,
    [policy_id]                  UNIQUEIDENTIFIER NOT NULL,
    [is_active]                  BIT              DEFAULT ((1)) NULL,
    [created_at]                 DATETIME         NULL,
    [created_by]                 UNIQUEIDENTIFIER NULL,
    [updated_at]                 DATETIME         NULL,
    [updated_by]                 UNIQUEIDENTIFIER NULL,
    PRIMARY KEY CLUSTERED ([user_application_policy_id] ASC),
    CONSTRAINT [FK_UserApplicationPolicy_Policy] FOREIGN KEY ([policy_id]) REFERENCES [dbo].[policies] ([policy_id]),
    CONSTRAINT [FK_UserApplicationPolicy_UserApplication] FOREIGN KEY ([user_application_id]) REFERENCES [dbo].[user_applications] ([user_application_id])
);


GO

