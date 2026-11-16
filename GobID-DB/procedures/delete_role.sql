CREATE   PROCEDURE [dbo].[delete_role]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @role_id   int              = JSON_VALUE(@payload,'$.role_id'),
    @updated_by uniqueidentifier = JSON_VALUE(@payload,'$.updated_by'),
    @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');

  BEGIN TRY
    BEGIN TRANSACTION;

      /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.roles WHERE role_id = @role_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    UPDATE dbo.roles
       SET is_active = 0,
           updated_at = SYSUTCDATETIME(),
           updated_by = @updated_by
     WHERE role_id = @role_id
       AND is_active = 1;

    DECLARE @ra int = @@ROWCOUNT;

     /* Post-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.roles WHERE role_id = @role_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

     /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_role', NULL, 1,
        @before, @after,
        @observation, 0, 1,
        SYSUTCDATETIME(), @updated_by
     );
    COMMIT;

    SELECT rows_affected = @ra
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    DECLARE @err nvarchar(2048) =
      (SELECT ERROR_NUMBER() AS number, ERROR_MESSAGE() AS message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

