CREATE TABLE [dbo].[application_role_claims] (
    [application_role_claim_id] INT              IDENTITY (1, 1) NOT NULL,
    [application_role_id]       INT              NOT NULL,
    [claim_id]                  INT              NOT NULL,
    [is_active]                 BIT              CONSTRAINT [DF_app_role_claims_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]                DATETIME2 (3)    CONSTRAINT [DF_app_role_claims_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]                UNIQUEIDENTIFIER NULL,
    [updated_at]                DATETIME2 (3)    NULL,
    [updated_by]                UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_application_role_claims] PRIMARY KEY CLUSTERED ([application_role_claim_id] ASC),
    CONSTRAINT [FK_app_role_claims_app_roles] FOREIGN KEY ([application_role_id]) REFERENCES [dbo].[application_roles] ([application_role_id]),
    CONSTRAINT [FK_app_role_claims_claims] FOREIGN KEY ([claim_id]) REFERENCES [dbo].[claims] ([claim_id]),
    CONSTRAINT [UQ_app_role_claims] UNIQUE NONCLUSTERED ([application_role_id] ASC, [claim_id] ASC)
);


GO

CREATE NONCLUSTERED INDEX [IX_app_role_claims_role_id]
    ON [dbo].[application_role_claims]([application_role_id] ASC) WHERE ([is_active]=(1));


GO

