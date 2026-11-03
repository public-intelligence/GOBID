-- =============================================
-- Agrega campos de confirmación de email a la tabla users
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.users') AND name = 'email_confirmed'
)
BEGIN
    ALTER TABLE dbo.users
    ADD email_confirmed BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.users') AND name = 'email_confirmation_token'
)
BEGIN
    ALTER TABLE dbo.users
    ADD email_confirmation_token NVARCHAR(255) NULL;
END
GO

-- =============================================
-- SP: confirm_user_email
-- Valida el token de confirmación y marca el email como confirmado.
-- Payload JSON esperado: { "token": "..." }
-- =============================================

IF OBJECT_ID('dbo.confirm_user_email', 'P') IS NOT NULL
    DROP PROCEDURE dbo.confirm_user_email;
GO

CREATE PROCEDURE dbo.confirm_user_email
    @payload NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @token NVARCHAR(255);

    SELECT @token = JSON_VALUE(@payload, '$.token');

    IF @token IS NULL OR LEN(LTRIM(RTRIM(@token))) = 0
    BEGIN
        RAISERROR('{"message":"Token de confirmación inválido"}', 16, 1) WITH SETERROR;
        RETURN;
    END

    DECLARE @userId UNIQUEIDENTIFIER;

    SELECT @userId = user_id
    FROM dbo.users
    WHERE email_confirmation_token = @token
      AND is_active = 1;

    IF @userId IS NULL
    BEGIN
        RAISERROR('{"message":"El token de confirmación no es válido o ya fue utilizado"}', 16, 1) WITH SETERROR;
        RETURN;
    END

    UPDATE dbo.users
    SET email_confirmed            = 1,
        email_confirmation_token   = NULL,
        updated_at                 = SYSUTCDATETIME()
    WHERE user_id = @userId;

    SELECT (
        SELECT LOWER(CONVERT(NVARCHAR(36), @userId)) AS user_id
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS result;
END
GO
