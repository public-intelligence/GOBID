CREATE   PROCEDURE [dbo].[delete_app_access]
  @application_access_id int
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  BEGIN TRY
    BEGIN TRAN;

     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.application_access WHERE application_access_id = @application_access_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    UPDATE dbo.application_access
    SET    is_active  = 0,
           updated_at = SYSUTCDATETIME()
    WHERE  application_access_id = @application_access_id;

      /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_app_access', @application_access_id, 1,
        @before, '{"status":"deleted"}',
        NULL, 0, 1,
        SYSUTCDATETIME(), NULL
     );

    COMMIT;

    SELECT application_access_id = @application_access_id
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
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

