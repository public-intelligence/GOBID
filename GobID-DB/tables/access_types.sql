CREATE TABLE [dbo].[access_types] (
    [access_type_id]          INT              IDENTITY (1, 1) NOT NULL,
    [access_type_name]        NVARCHAR (100)   NOT NULL,
    [access_type_description] NVARCHAR (500)   NULL,
    [is_active]               BIT              CONSTRAINT [DF_access_types_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]              DATETIME2 (3)    CONSTRAINT [DF_access_types_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]              UNIQUEIDENTIFIER NULL,
    [updated_at]              DATETIME2 (3)    NULL,
    [updated_by]              UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_access_types] PRIMARY KEY CLUSTERED ([access_type_id] ASC)
);


GO

