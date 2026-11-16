CREATE TABLE [dbo].[policy_evaluation_logs] (
    [policy_evaluation_log_id] UNIQUEIDENTIFIER NOT NULL,
    [user_id]                  UNIQUEIDENTIFIER NOT NULL,
    [policy_id]                UNIQUEIDENTIFIER NOT NULL,
    [organization_id]          INT              NOT NULL,
    [evaluation_result]        VARCHAR (10)     NOT NULL,
    [failure_reason]           VARCHAR (500)    NULL,
    [ip_address]               VARCHAR (45)     NULL,
    [user_agent]               VARCHAR (500)    NULL,
    [evaluated_at]             DATETIME         NULL,
    PRIMARY KEY CLUSTERED ([policy_evaluation_log_id] ASC),
    CONSTRAINT [FK_PolicyEvaluationLog_Organization] FOREIGN KEY ([organization_id]) REFERENCES [dbo].[organizations] ([organization_id]),
    CONSTRAINT [FK_PolicyEvaluationLog_Policy] FOREIGN KEY ([policy_id]) REFERENCES [dbo].[policies] ([policy_id]),
    CONSTRAINT [FK_PolicyEvaluationLog_User] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id])
);


GO

