CREATE TABLE [dbo].[user_organizations] (
    [user_organizations_id] UNIQUEIDENTIFIER CONSTRAINT [DF__user_orga__user___1C873BEC] DEFAULT (newid()) NOT NULL,
    [organization_id]       INT              NOT NULL,
    [user_id]               UNIQUEIDENTIFIER NOT NULL,
    [email]                 NVARCHAR (320)   NULL,
    [position]              NVARCHAR (100)   NULL,
    [created_at]            DATETIME         CONSTRAINT [DF__user_orga__creat__1D7B6025] DEFAULT (getdate()) NULL,
    CONSTRAINT [PK__user_org__AABF18F7470BC6B1] PRIMARY KEY CLUSTERED ([user_organizations_id] ASC),
    CONSTRAINT [FK__user_orga__organ__1E6F845E] FOREIGN KEY ([organization_id]) REFERENCES [dbo].[organizations] ([organization_id]),
    CONSTRAINT [FK__user_orga__user___1F63A897] FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([user_id]),
    CONSTRAINT [uq_user_organization] UNIQUE NONCLUSTERED ([organization_id] ASC, [user_id] ASC)
);


GO

