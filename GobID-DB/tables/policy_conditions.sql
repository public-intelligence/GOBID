CREATE TABLE [dbo].[policy_conditions] (
    [policy_condition_id] INT              IDENTITY (1, 1) NOT NULL,
    [policy_id]           UNIQUEIDENTIFIER NOT NULL,
    [condition_key]       VARCHAR (100)    NOT NULL,
    [condition_value]     VARCHAR (255)    NOT NULL,
    PRIMARY KEY CLUSTERED ([policy_condition_id] ASC),
    CONSTRAINT [FK_PolicyCondition_Policy] FOREIGN KEY ([policy_id]) REFERENCES [dbo].[policies] ([policy_id]) ON DELETE CASCADE
);


GO

