CREATE PROCEDURE [dbo].[delete_organization_contact]
	@contact_id int
AS
BEGIN
  SET NOCOUNT ON;

  IF @contact_id IS NULL
     THROW 51000, '{"message":"user_id is required"}', 1;

  BEGIN TRY
    BEGIN TRAN;

      /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.organization_contacts WHERE contact_id = @contact_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    DELETE FROM dbo.organization_contacts
    WHERE contact_id = @contact_id;

    IF @@ROWCOUNT = 0
      THROW 51000, '{"message":"No rows deleted – contact_id not found."}', 1;

    /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_organization_contact', @contact_id, 1,
        @before, '{"status":"deleted"}',
        NULL, 0, 1,
        SYSUTCDATETIME(), NULL
     );
    COMMIT;

    SELECT '{"message":"Contact deleted successfully."}' AS result;
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

