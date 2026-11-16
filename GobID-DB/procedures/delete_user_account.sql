CREATE PROCEDURE [dbo].[delete_user_account]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @now datetime2(3) = SYSUTCDATETIME();

  /*— 1. De-serialize ------------------------------------------------*/
  DECLARE
    @user_id      uniqueidentifier = JSON_VALUE(@payload,'$.user_id'),
    @deleted_by   uniqueidentifier = JSON_VALUE(@payload,'$.deleted_by'),
    @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');

  IF @user_id IS NULL
     THROW 51000, '{"message":"user_id is required"}', 1;

  /*— 2. Soft-delete + audit ----------------------------------------*/
  BEGIN TRY
     BEGIN TRAN;

     /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.users WHERE user_id = @user_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

     /* Delete (soft) sin row_version */
     UPDATE dbo.users
        SET discharge_date = CAST(@now AS date),
            updated_at     = @now,
            updated_by     = @deleted_by,
            deleted_at     = @now,
            deleted_by     = @deleted_by,
            is_active      = 0
      WHERE user_id = @user_id;

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
        'delete_user_account', @user_id, 1,
        @before, @after,
        @observation, 0, 1,
        @now, @deleted_by
     );

     /* Return confirmation */
     SELECT
       user_id        = @user_id,
       discharge_date = CAST(@now AS date)
     FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

     COMMIT;
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

