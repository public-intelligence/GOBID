CREATE PROCEDURE [dbo].[upsert_user_organization]
@payload NVARCHAR(MAX)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @user_id UNIQUEIDENTIFIER = JSON_VALUE(@payload,'$.user_id'),
    @organization_id INT      = JSON_VALUE(@payload,'$.organization_id'),
    @email NVARCHAR(320)      = JSON_VALUE(@payload,'$.email'),
    @position NVARCHAR(100)   = JSON_VALUE(@payload,'$.position');

  -- 🔒 Validaciones
  IF @user_id IS NULL
    THROW 51001, '{"message":"user_id es obligatorio"}', 1;

  IF @organization_id IS NULL
    THROW 51002, '{"message":"organization_id es obligatorio"}', 1;

  BEGIN TRY
    BEGIN TRANSACTION;

    -- ✅ Insertar SOLO si no existe
    IF NOT EXISTS (
      SELECT 1
      FROM dbo.user_organizations
      WHERE user_id = @user_id
        AND organization_id = @organization_id
    )
    BEGIN
      INSERT dbo.user_organizations
        (user_id, organization_id, email, position)
      VALUES
        (@user_id, @organization_id, @email, @position);
    END

    -- ✅ Retornar el registro (exista o recién insertado)
    SELECT
      user_id,
      organization_id,
      email,
      position
    FROM dbo.user_organizations
    WHERE user_id = @user_id
      AND organization_id = @organization_id
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    COMMIT;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;

    DECLARE @err NVARCHAR(2048) =
      (SELECT
        error_number   = ERROR_NUMBER(),
        error_message  = ERROR_MESSAGE()
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    THROW 51000, @err, 1;
  END CATCH
END;

GO

