CREATE TABLE [dbo].[claims] (
    [claim_id]          INT              IDENTITY (1, 1) NOT NULL,
    [code]              NVARCHAR (50)    NOT NULL,
    [claim_name]        NVARCHAR (100)   NOT NULL,
    [is_automatic]      BIT              NOT NULL,
    [claim_description] NVARCHAR (500)   NULL,
    [is_active]         BIT              CONSTRAINT [DF_claims_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]        DATETIME2 (3)    CONSTRAINT [DF_claims_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]        UNIQUEIDENTIFIER NULL,
    [updated_at]        DATETIME2 (3)    NULL,
    [updated_by]        UNIQUEIDENTIFIER NULL,
    [application_id]    UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    CONSTRAINT [PK_claims] PRIMARY KEY CLUSTERED ([claim_id] ASC)
);


GO

CREATE NONCLUSTERED INDEX [IX_claims_app_id_code]
    ON [dbo].[claims]([application_id] ASC, [code] ASC);


GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_claims_application_code]
    ON [dbo].[claims]([application_id] ASC, [code] ASC);


GO

