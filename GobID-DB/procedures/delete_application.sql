CREATE PROCEDURE [dbo].[delete_application]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @application_id  uniqueidentifier,
    @updated_by      uniqueidentifier,
    @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');

  -- Obtenemos solo los campos necesarios
  SELECT
    @application_id = TRY_CONVERT(uniqueidentifier,JSON_VALUE(@payload,'$.application_id')),
    @updated_by     = TRY_CONVERT(uniqueidentifier,JSON_VALUE(@payload,'$.updated_by'));


  -- Validar que se envió application_id
  IF @application_id IS NULL
     THROW 51002, '{"message":"application_id is required."}', 1;
 

  BEGIN TRY
    BEGIN TRANSACTION;

       /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.applications WHERE application_id = @application_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

      UPDATE dbo.applications
      SET    is_active = 0,
             updated_at = SYSUTCDATETIME(),
             updated_by = @updated_by,
			 deleted_at = SYSUTCDATETIME(),
			 deleted_by = @updated_by
      WHERE  application_id = @application_id;

      IF @@ROWCOUNT = 0
         THROW 51003, '{"message":"Record not found."}', 1;

    /* Pre-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.applications WHERE application_id = @application_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_application', @application_id, 1,
        @before, @after,
        @observation, 0, 1,
        SYSUTCDATETIME(), @updated_by
     );
      -- row_version se actualizará automáticamente en la tabla
      SELECT
        application_id = @application_id,
        deleted        = 1
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    COMMIT;
  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;

    DECLARE @err nvarchar(2048) = (
       SELECT ERROR_NUMBER()   AS number,
              ERROR_MESSAGE()  AS message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    THROW 51000, @err, 1;
  END CATCH
END;

GO

