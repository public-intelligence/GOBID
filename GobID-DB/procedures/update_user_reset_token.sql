CREATE PROCEDURE [dbo].[update_user_reset_token]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
      @user_id             uniqueidentifier = JSON_VALUE(@payload, '$.user_id'),
      @token               nvarchar(255)    = JSON_VALUE(@payload, '$.reset_token'),
      @expected_rowver_raw nvarchar(50)     = JSON_VALUE(@payload, '$.expected_row_version'),
      @updated_by          uniqueidentifier = JSON_VALUE(@payload, '$.updated_by');

  DECLARE @expected_rowver varbinary(8) =
      CASE WHEN @expected_rowver_raw IS NULL THEN NULL
           ELSE CONVERT(varbinary(8), @expected_rowver_raw, 1) END;

  BEGIN TRY
    BEGIN TRAN;

      IF @user_id IS NULL OR @token IS NULL
        THROW 51000, '{"error":"user_id and reset_token are required"}', 1;

      /* 🔥 BEFORE */
      DECLARE @before nvarchar(max);

      SELECT @before =
        (SELECT *
         FROM dbo.users
         WHERE user_id = @user_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

      /* 🔥 UPDATE */
      DECLARE @out table(row_version varbinary(8));

      UPDATE dbo.users
         SET password_reset_token = @token,
             must_change_password = 1,
             updated_at           = SYSUTCDATETIME(),
             updated_by           = @updated_by
      OUTPUT INSERTED.row_version INTO @out
       WHERE user_id   = @user_id
         AND is_active = 1
         AND (@expected_rowver IS NULL OR row_version = @expected_rowver);

      IF @@ROWCOUNT = 0
        THROW 51000, '{"error":"Concurrency conflict or user not found"}', 1;

      /* 🔥 AFTER */
      DECLARE @after nvarchar(max);

      SELECT @after =
        (SELECT *
         FROM dbo.users
         WHERE user_id = @user_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

      /* 🔥 LOG (solo si update ocurrió — aquí siempre que pasa @@ROWCOUNT) */
      INSERT dbo.change_logs (
          log_name, entity_id, is_deleting,
          before_update_entity_json, after_update_entity_json,
          observation, did_rollback, is_active,
          created_at, created_by
      )
      VALUES (
          'update_user_reset_token',
          @user_id,
          0,
          @before,
          @after,
          NULL,
          0,
          1,
          SYSUTCDATETIME(),
          @updated_by
      );

      COMMIT;

      /* 🔥 RESULT */
      SELECT
        @user_id                              AS user_id,
        (SELECT TOP 1 row_version FROM @out)  AS row_version,
        1                                     AS rows_affected
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;

    DECLARE @err nvarchar(2048) =
      (SELECT ERROR_NUMBER() AS number,
              ERROR_MESSAGE() AS message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    THROW 51000, @err, 1;
  END CATCH
END;

GO

