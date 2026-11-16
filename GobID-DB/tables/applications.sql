CREATE TABLE [dbo].[applications] (
    [application_name]        NVARCHAR (255)   NOT NULL,
    [code]                    NVARCHAR (50)    NOT NULL,
    [redirect_url]            NVARCHAR (255)   NULL,
    [application_description] NVARCHAR (500)   NULL,
    [is_active]               BIT              CONSTRAINT [DF_applications_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]              DATETIME2 (3)    CONSTRAINT [DF_applications_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]              UNIQUEIDENTIFIER NULL,
    [updated_at]              DATETIME2 (3)    NULL,
    [updated_by]              UNIQUEIDENTIFIER NULL,
    [row_version]             ROWVERSION       NOT NULL,
    [deleted_at]              DATETIME2 (3)    NULL,
    [deleted_by]              UNIQUEIDENTIFIER NULL,
    [random_string]           NVARCHAR (255)   NULL,
    [application_id]          UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [is_public]               BIT              CONSTRAINT [DF_applications_is_public] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_applications] PRIMARY KEY CLUSTERED ([application_id] ASC),
    CONSTRAINT [UQ_applications_code] UNIQUE NONCLUSTERED ([code] ASC)
);


GO

