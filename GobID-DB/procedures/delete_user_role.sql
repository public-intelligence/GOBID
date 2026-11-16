CREATE   PROCEDURE [dbo].[delete_user_role]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

    DECLARE
      @user_role_id int                = JSON_VALUE(@payload,'$.user_role_id'),
      @actor_id     uniqueidentifier   = JSON_VALUE(@payload,'$.actor_id'),
      @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');

      
    /* Pre-image */
    DECLARE @before nvarchar(max);
    SELECT @before =
       (SELECT * FROM dbo.user_roles WHERE user_role_id = @user_role_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    UPDATE dbo.user_roles
    SET    is_active = 0,
           updated_at = SYSUTCDATETIME(),
           updated_by = @actor_id
    WHERE  user_role_id = @user_role_id
      AND  is_active    = 1;

      /* Post-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.user_roles WHERE user_role_id = @user_role_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

     /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_user_rol', @user_role_id, 1,
        @before, @after,
        @observation, 0, 1,
        SYSUTCDATETIME(), @actor_id
     );

    DECLARE @rows int = @@ROWCOUNT;
    COMMIT;

    SELECT rows_affected = @rows
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    DECLARE @err nvarchar(2048) =
       (SELECT ERROR_NUMBER() AS num, ERROR_MESSAGE() AS msg FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

