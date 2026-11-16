CREATE PROCEDURE [dbo].[delete_user_application]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @user_application_id      uniqueidentifier = JSON_VALUE(@payload,'$.user_application_id'),
    @updated_by               uniqueidentifier = JSON_VALUE(@payload,'$.updated_by'),
    @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');

  BEGIN TRY
    BEGIN TRAN;

      /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.user_applications WHERE user_application_id = @user_application_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    UPDATE dbo.user_applications
      SET is_active  = 0,
          updated_at = SYSUTCDATETIME(),
          updated_by = @updated_by
    WHERE user_application_id = @user_application_id;

     IF @@ROWCOUNT = 0
        THROW 51000, '{"message":"Row not found"}', 1;

     /* Post-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.user_applications WHERE user_application_id = @user_application_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

     /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_user_account', @user_application_id, 1,
        @before, @after,
        @observation, 0, 1,
        SYSUTCDATETIME(), @updated_by
     );

    COMMIT;

    SELECT
      @user_application_id AS user_application_id,
      1                    AS deleted
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    DECLARE @err nvarchar(2048) =
      (SELECT
         error_number  = ERROR_NUMBER(),
         error_message = ERROR_MESSAGE()
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END

GO

