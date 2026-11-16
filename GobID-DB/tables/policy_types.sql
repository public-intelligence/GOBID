CREATE TABLE [dbo].[policy_types] (
    [policy_type_id] INT              IDENTITY (1, 1) NOT NULL,
    [name]           VARCHAR (100)    NOT NULL,
    [description]    VARCHAR (500)    NULL,
    [is_active]      BIT              DEFAULT ((1)) NULL,
    [created_at]     DATETIME         NULL,
    [created_by]     UNIQUEIDENTIFIER NULL,
    PRIMARY KEY CLUSTERED ([policy_type_id] ASC)
);


GO

