CREATE TABLE [dbo].[security_audit_logs] (
    [audit_log_id]   BIGINT           IDENTITY (1, 1) NOT NULL,
    [event_type]     NVARCHAR (50)    NOT NULL,
    [user_id]        UNIQUEIDENTIFIER NULL,
    [application_id] UNIQUEIDENTIFIER NULL,
    [correlation_id] NVARCHAR (100)   NULL,
    [ip]             NVARCHAR (45)    NULL,
    [user_agent]     NVARCHAR (512)   NULL,
    [details]        NVARCHAR (MAX)   NULL,
    [success]        BIT              NOT NULL,
    [created_at]     DATETIME2 (3)    CONSTRAINT [DF_security_audit_logs_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    CONSTRAINT [PK_security_audit_logs] PRIMARY KEY CLUSTERED ([audit_log_id] ASC)
);


GO

CREATE NONCLUSTERED INDEX [IX_sec_audit_app_event]
    ON [dbo].[security_audit_logs]([application_id] ASC, [event_type] ASC, [created_at] DESC) WHERE ([application_id] IS NOT NULL);


GO

CREATE NONCLUSTERED INDEX [IX_sec_audit_correlation]
    ON [dbo].[security_audit_logs]([correlation_id] ASC) WHERE ([correlation_id] IS NOT NULL);


GO

CREATE NONCLUSTERED INDEX [IX_audit_rate_limit]
    ON [dbo].[security_audit_logs]([application_id] ASC, [event_type] ASC, [ip] ASC, [created_at] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_sec_audit_user_event]
    ON [dbo].[security_audit_logs]([user_id] ASC, [event_type] ASC, [created_at] DESC) WHERE ([user_id] IS NOT NULL);


GO

