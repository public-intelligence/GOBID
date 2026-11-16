CREATE TABLE [dbo].[application_access] (
    [application_access_id]       INT              IDENTITY (1, 1) NOT NULL,
    [organization_application_id] INT              NOT NULL,
    [access_type_id]              INT              NOT NULL,
    [configuration_json]          NVARCHAR (MAX)   NULL,
    [is_active]                   BIT              CONSTRAINT [DF_app_access_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]                  DATETIME2 (3)    CONSTRAINT [DF_app_access_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]                  UNIQUEIDENTIFIER NULL,
    [updated_at]                  DATETIME2 (3)    NULL,
    [updated_by]                  UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_application_access] PRIMARY KEY CLUSTERED ([application_access_id] ASC),
    CONSTRAINT [FK_app_access_access_types] FOREIGN KEY ([access_type_id]) REFERENCES [dbo].[access_types] ([access_type_id]),
    CONSTRAINT [FK_app_access_org_apps] FOREIGN KEY ([organization_application_id]) REFERENCES [dbo].[organization_applications] ([organization_application_id])
);


GO

