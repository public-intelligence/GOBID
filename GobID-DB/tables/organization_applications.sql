CREATE TABLE [dbo].[organization_applications] (
    [organization_application_id] INT              IDENTITY (1, 1) NOT NULL,
    [organization_id]             INT              NOT NULL,
    [is_active]                   BIT              CONSTRAINT [DF_org_apps_is_active] DEFAULT ((1)) NOT NULL,
    [row_version]                 ROWVERSION       NOT NULL,
    [application_id]              UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    CONSTRAINT [PK_organization_applications] PRIMARY KEY CLUSTERED ([organization_application_id] ASC),
    CONSTRAINT [FK_org_apps_organizations] FOREIGN KEY ([organization_id]) REFERENCES [dbo].[organizations] ([organization_id])
);


GO

CREATE NONCLUSTERED INDEX [IX_org_apps_org_id]
    ON [dbo].[organization_applications]([organization_id] ASC);


GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_org_apps_orgid_appid]
    ON [dbo].[organization_applications]([organization_id] ASC, [application_id] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_org_apps_app_id]
    ON [dbo].[organization_applications]([application_id] ASC);


GO

