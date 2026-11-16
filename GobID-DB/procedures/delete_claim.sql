CREATE   PROCEDURE [dbo].[delete_claim]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

    DECLARE
      @claim_id int              = JSON_VALUE(@payload,'$.claim_id'),
      @updated_by    uniqueidentifier = TRY_CONVERT(uniqueidentifier,JSON_VALUE(@payload,'$.updated_by')),
      @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');

     /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.claims WHERE claim_id = @claim_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    UPDATE dbo.claims
      SET is_active = 0,
          updated_at = SYSUTCDATETIME(),
          updated_by = @updated_by
      WHERE claim_id = @claim_id
        AND is_active = 1;

    DECLARE @rows int = @@ROWCOUNT;


     /* Post-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.claims WHERE claim_id = @claim_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    
    /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_claims', @claim_id, 1,
        @before, @after,
        @observation, 0, 1,
        SYSUTCDATETIME(), @updated_by
     );

    COMMIT;

    SELECT deleted = @rows
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
  END TRY
  BEGIN CATCH
    ROLLBACK;
    DECLARE @err nvarchar(2048) =
      ( SELECT ERROR_NUMBER() AS number, ERROR_MESSAGE() AS message FOR JSON PATH, WITHOUT_ARRAY_WRAPPER );
    THROW 51000, @err, 1;
  END CATCH
END;

GO

