CREATE TABLE [dbo].[change_logs] (
    [change_log_id]             INT              IDENTITY (1, 1) NOT NULL,
    [log_name]                  NVARCHAR (100)   NOT NULL,
    [entity_id]                 NVARCHAR (255)   NULL,
    [is_deleting]               BIT              DEFAULT ((0)) NOT NULL,
    [before_update_entity_json] NVARCHAR (MAX)   NULL,
    [after_update_entity_json]  NVARCHAR (MAX)   NULL,
    [observation]               NVARCHAR (255)   NULL,
    [did_rollback]              BIT              DEFAULT ((0)) NOT NULL,
    [rollback_date]             DATETIME2 (3)    NULL,
    [is_active]                 BIT              DEFAULT ((1)) NOT NULL,
    [created_at]                DATETIME2 (3)    DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]                UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_change_logs] PRIMARY KEY CLUSTERED ([change_log_id] ASC)
);


GO

