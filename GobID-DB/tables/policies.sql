CREATE TABLE [dbo].[policies] (
    [policy_id]       UNIQUEIDENTIFIER NOT NULL,
    [organization_id] INT              NOT NULL,
    [policy_type_id]  INT              NOT NULL,
    [name]            VARCHAR (150)    NOT NULL,
    [description]     VARCHAR (500)    NULL,
    [effect]          VARCHAR (10)     NOT NULL,
    [is_active]       BIT              DEFAULT ((1)) NULL,
    [created_at]      DATETIME         NULL,
    [created_by]      UNIQUEIDENTIFIER NULL,
    [updated_at]      DATETIME         NULL,
    [updated_by]      UNIQUEIDENTIFIER NULL,
    [row_version]     ROWVERSION       NOT NULL,
    PRIMARY KEY CLUSTERED ([policy_id] ASC),
    CONSTRAINT [FK_Policy_Organization] FOREIGN KEY ([organization_id]) REFERENCES [dbo].[organizations] ([organization_id]),
    CONSTRAINT [FK_Policy_PolicyType] FOREIGN KEY ([policy_type_id]) REFERENCES [dbo].[policy_types] ([policy_type_id])
);


GO

