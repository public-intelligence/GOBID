CREATE TABLE [dbo].[user_applications] (
    [user_application_id]          UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [user_id]                      UNIQUEIDENTIFIER NOT NULL,
    [user_application_description] NVARCHAR (500)   NULL,
    [is_application_admin]         BIT              CONSTRAINT [DF_user_apps_admin] DEFAULT ((0)) NOT NULL,
    [internal_app_user_code]       NVARCHAR (50)    NULL,
    [is_active]                    BIT              CONSTRAINT [DF_user_apps_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]                   DATETIME2 (3)    CONSTRAINT [DF_user_apps_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]                   UNIQUEIDENTIFIER NULL,
    [updated_at]                   DATETIME2 (3)    NULL,
    [updated_by]                   UNIQUEIDENTIFIER NULL,
    [row_version]                  ROWVERSION       NOT NULL,
    [organization_application_id]  INT              NOT NULL,
    CONSTRAINT [PK_user_applications] PRIMARY KEY CLUSTERED ([user_application_id] ASC),
    CONSTRAINT [UQ_usr_apps_usrid_appid] UNIQUE NONCLUSTERED ([user_id] ASC, [organization_application_id] ASC)
);


GO

