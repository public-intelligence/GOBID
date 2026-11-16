-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE dbo.confirm_user_email
    @payload NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @user_id UNIQUEIDENTIFIER = JSON_VALUE(@payload, '$.user_id');

    IF @user_id IS NULL
    BEGIN
        RAISERROR('{"message":"Identificador de usuario inválido"}', 16, 1) WITH SETERROR;
        RETURN;
    END

    -- Verifica que la cuenta aún no fue confirmada y tiene token pendiente
    IF NOT EXISTS (
        SELECT 1 FROM dbo.users
        WHERE user_id                  = @user_id
          AND is_active                = 1
          AND email_confirmed          = 0
          AND email_confirmation_token IS NOT NULL
    )
    BEGIN
        RAISERROR('{"message":"El token de confirmación no es válido o ya fue utilizado"}', 16, 1) WITH SETERROR;
        RETURN;
    END

    UPDATE dbo.users
    SET email_confirmed          = 1,
        email_confirmation_token = NULL,
        updated_at               = SYSUTCDATETIME()
    WHERE user_id = @user_id;

    SELECT (
        SELECT LOWER(CONVERT(NVARCHAR(36), @user_id)) AS user_id
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS result;
END

GO

