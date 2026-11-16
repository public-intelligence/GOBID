CREATE TABLE [dbo].[personnel] (
    [personnel_id]         UNIQUEIDENTIFIER CONSTRAINT [DF_personnel_personnel_id] DEFAULT (newsequentialid()) NOT NULL,
    [position_id]          UNIQUEIDENTIFIER NULL,
    [organization_id]      INT              NOT NULL,
    [is_active]            BIT              CONSTRAINT [DF_personnel_is_active] DEFAULT ((1)) NOT NULL,
    [created_at]           DATETIME2 (3)    CONSTRAINT [DF_personnel_created_at] DEFAULT (sysutcdatetime()) NOT NULL,
    [created_by]           UNIQUEIDENTIFIER NULL,
    [updated_at]           DATETIME2 (3)    NULL,
    [updated_by]           UNIQUEIDENTIFIER NULL,
    [person_id]            UNIQUEIDENTIFIER NOT NULL,
    [locality_id]          UNIQUEIDENTIFIER NULL,
    [position_nominate_id] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_personnel] PRIMARY KEY CLUSTERED ([personnel_id] ASC),
    CONSTRAINT [FK_personnel_organizations] FOREIGN KEY ([organization_id]) REFERENCES [dbo].[organizations] ([organization_id])
);


GO

CREATE NONCLUSTERED INDEX [IX_personnel_org_id]
    ON [dbo].[personnel]([organization_id] ASC) WHERE ([is_active]=(1));


GO

CREATE NONCLUSTERED INDEX [IX_personnel_position_org]
    ON [dbo].[personnel]([position_id] ASC, [organization_id] ASC) WHERE ([is_active]=(1));


GO

CREATE NONCLUSTERED INDEX [IX_personnel_position_id]
    ON [dbo].[personnel]([position_id] ASC) WHERE ([is_active]=(1));


GO

