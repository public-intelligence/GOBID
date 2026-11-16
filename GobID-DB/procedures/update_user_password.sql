CREATE   PROCEDURE [dbo].[update_user_password]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  /*----------- JSON → local variables --------------------------------*/
  DECLARE
      @user_id       uniqueidentifier
                     = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.user_id')),
      @new_pwd       nvarchar(128) = JSON_VALUE(@payload,'$.new_password'),
      @must_change   bit           = JSON_VALUE(@payload,'$.must_change_password');

  /*----------- error & transaction wrapper ---------------------------*/
  BEGIN TRY
     BEGIN TRAN;

     /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.users WHERE user_id = @user_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

       /* temp table to catch OUTPUT */
       DECLARE @rv TABLE (row_version varbinary(8));

       UPDATE dbo.users
          SET user_password        = @new_pwd,
              must_change_password = @must_change,
              updated_at           = SYSUTCDATETIME()
          OUTPUT INSERTED.row_version INTO @rv
        WHERE user_id    = @user_id;

        IF @@ROWCOUNT = 0
        THROW 51000, '{"message":"Row not found"}', 1;

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
        'update_user_password', @user_id, 0,
        @before, @after,
        NULL, 0, 1,
        SYSUTCDATETIME(), @user_id
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

