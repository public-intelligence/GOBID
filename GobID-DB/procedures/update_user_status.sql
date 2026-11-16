CREATE PROCEDURE [dbo].[update_user_status]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  /*----------- JSON → local variables --------------------------------*/
  DECLARE
      @user_id       uniqueidentifier
                     = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.user_id')),
      @is_active   bit = JSON_VALUE(@payload, '$.is_active'),
      @updated_by      uniqueidentifier        = JSON_VALUE(@payload,'$.updated_by'),
      @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');

  /*----------- error & transaction wrapper ---------------------------*/
  BEGIN TRY
     BEGIN TRAN;

      DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.users WHERE user_id = @user_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

       UPDATE dbo.users
          SET is_active        = @is_active,
              updated_at           = SYSUTCDATETIME(),
              updated_by         = @updated_by
        WHERE user_id    = @user_id;

         /* Post-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.users WHERE user_id = @user_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

     /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'update_user_status', @user_id, 0,
        @before, @after,
        @observation, 0, 1,
        SYSUTCDATETIME(), @updated_by
     );

     COMMIT;
  END TRY
  BEGIN CATCH
     IF @@TRANCOUNT > 0 ROLLBACK;
     DECLARE @err nvarchar(2048) =
       (SELECT error_number   = ERROR_NUMBER(),
               error_severity = ERROR_SEVERITY(),
               error_state    = ERROR_STATE(),
               error_proc     = ERROR_PROCEDURE(),
               error_line     = ERROR_LINE(),
               error_message  = ERROR_MESSAGE()
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
     THROW 51000, @err, 1;
  END CATCH
END;

GO

