CREATE TABLE [dbo].[login_logs] (
    [login_log_id]                INT              IDENTITY (1, 1) NOT NULL,
    [login_info]                  NVARCHAR (500)   NULL,
    [ip]                          NVARCHAR (45)    NULL,
    [header_values]               NVARCHAR (255)   NULL,
    [login_successful]            BIT              DEFAULT ((0)) NOT NULL,
    [organization_id]             INT              NOT NULL,
    [organization_application_id] INT              NOT NULL,
    [is_active]                   BIT              DEFAULT ((1)) NOT NULL,
    [created_at]                  DATETIME2 (3)    DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]                  UNIQUEIDENTIFIER NULL,
    [updated_at]                  DATETIME2 (3)    NULL,
    [updated_by]                  UNIQUEIDENTIFIER NULL,
    [correlation_id]              NVARCHAR (100)   NULL,
    CONSTRAINT [PK_login_logs] PRIMARY KEY CLUSTERED ([login_log_id] ASC),
    CONSTRAINT [FK_login_logs_org_apps] FOREIGN KEY ([organization_application_id]) REFERENCES [dbo].[organization_applications] ([organization_application_id]),
    CONSTRAINT [FK_login_logs_organizations] FOREIGN KEY ([organization_id]) REFERENCES [dbo].[organizations] ([organization_id])
);


GO

CREATE NONCLUSTERED INDEX [IX_login_logs_org_date]
    ON [dbo].[login_logs]([organization_id] ASC, [created_at] DESC) WHERE ([is_active]=(1));


GO

