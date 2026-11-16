CREATE PROCEDURE [dbo].[upsert_user_role]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

    DECLARE
      @user_role_id          int,
      @user_application_id   uniqueidentifier,
      @role_id               int,
      @user_role_description nvarchar(500),
      @is_active             bit,
      @user_id_actor         uniqueidentifier;

    SELECT
      @user_role_id          = JSON_VALUE(@payload,'$.user_role_id'),
      @user_application_id   = JSON_VALUE(@payload,'$.user_application_id'),
      @role_id               = JSON_VALUE(@payload,'$.role_id'),
      @user_role_description = JSON_VALUE(@payload,'$.user_role_description'),
      @is_active             = COALESCE(JSON_VALUE(@payload,'$.is_active'),1),
      @user_id_actor         = JSON_VALUE(@payload,'$.updated_by');

    /* 🔥 BEFORE (solo si es update) */
    DECLARE @before nvarchar(max);

    IF @user_role_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT *
         FROM dbo.user_roles
         WHERE user_role_id = @user_role_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT */
    DECLARE @out TABLE (user_role_id int);

    MERGE dbo.user_roles AS t
    USING (SELECT
              @user_role_id        AS user_role_id,
              @user_application_id AS user_application_id,
              @role_id             AS role_id) AS s
      ON t.user_role_id = s.user_role_id

    WHEN MATCHED THEN
      UPDATE SET
        user_application_id   = COALESCE(s.user_application_id ,t.user_application_id),
        role_id               = COALESCE(s.role_id             ,t.role_id),
        user_role_description = @user_role_description,
        is_active             = @is_active,
        updated_at            = SYSUTCDATETIME(),
        updated_by            = @user_id_actor

    WHEN NOT MATCHED THEN
      INSERT (user_application_id, role_id, user_role_description, is_active,
              created_at, created_by)
      VALUES (s.user_application_id, s.role_id, @user_role_description, @is_active,
              SYSUTCDATETIME(), @user_id_actor)

    OUTPUT inserted.user_role_id INTO @out;

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51000, '{"message":"No se realizó ninguna acción"}', 1;

    /* 🔥 AFTER */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT *
       FROM dbo.user_roles
       WHERE user_role_id = (SELECT TOP 1 user_role_id FROM @out)
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* 🔥 LOG SOLO UPDATE */
    IF @user_role_id IS NOT NULL
    BEGIN
      INSERT dbo.change_logs (
          log_name, entity_id, is_deleting,
          before_update_entity_json, after_update_entity_json,
          observation, did_rollback, is_active,
          created_at, created_by
      )
      VALUES (
          'upsert_user_role',
          @user_role_id,
          0,
          @before,
          @after,
          NULL,
          0,
          1,
          SYSUTCDATETIME(),
          @user_id_actor
      );
    END

    COMMIT;

    /* 🔥 RESULT */
    SELECT user_role_id
    FROM @out
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;

    DECLARE @err nvarchar(2048) =
       (SELECT ERROR_NUMBER() AS err_no,
               ERROR_MESSAGE() AS err_msg,
               ERROR_LINE() AS err_line
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    THROW 51000, @err, 1;
  END CATCH
END;

GO

