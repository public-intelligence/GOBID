CREATE   PROCEDURE [dbo].[delete_access_type]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  DECLARE @access_type_id int = JSON_VALUE(@payload,'$.access_type_id');
  DECLARE @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');
  DECLARE @deleted_by   uniqueidentifier = JSON_VALUE(@payload,'$.deleted_by');

  BEGIN TRY
    BEGIN TRAN;

    /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.access_types WHERE access_type_id = @access_type_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* Delete (soft) sin row_version */
    UPDATE dbo.access_types
    SET    is_active  = 0,
           updated_at = SYSUTCDATETIME()
    WHERE  access_type_id = @access_type_id;

      IF @@ROWCOUNT = 0
        THROW 51000, '{"message":"Row not found"}', 1;

     /* Post-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.access_types WHERE access_type_id = @access_type_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_access_type', @access_type_id, 1,
        @before, @after,
        @observation, 0, 1,
       SYSUTCDATETIME(), @deleted_by
     );

     /* Return confirmation */
    SELECT access_type_id = @access_type_id
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    COMMIT;

  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    DECLARE @err nvarchar(2048) =
      (SELECT ERROR_NUMBER() AS number, ERROR_MESSAGE() AS message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

