CREATE TABLE [dbo].[role_claims] (
    [role_claim_id] INT              IDENTITY (1, 1) NOT NULL,
    [role_id]       INT              NOT NULL,
    [claim_id]      INT              NOT NULL,
    [is_active]     BIT              CONSTRAINT [DF_role_claims_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]    DATETIME2 (3)    CONSTRAINT [DF_role_claims_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]    UNIQUEIDENTIFIER NULL,
    [updated_at]    DATETIME2 (3)    NULL,
    [updated_by]    UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_role_claims] PRIMARY KEY CLUSTERED ([role_claim_id] ASC),
    CONSTRAINT [FK_role_claims_claims] FOREIGN KEY ([claim_id]) REFERENCES [dbo].[claims] ([claim_id]),
    CONSTRAINT [FK_role_claims_roles] FOREIGN KEY ([role_id]) REFERENCES [dbo].[roles] ([role_id])
);


GO

