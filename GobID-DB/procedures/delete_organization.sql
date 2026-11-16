CREATE   PROCEDURE [dbo].[delete_organization]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

    DECLARE
      @organization_id    int          = JSON_VALUE(@payload,'$.organization_id'),
	  @deleted_by         uniqueidentifier  = JSON_VALUE(@payload, '$.deleted_by'),
      @row_version_base64 nvarchar(50) = JSON_VALUE(@payload,'$.row_version'),
      @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation'),
      @expected_row_version varbinary(8);

    IF @organization_id IS NULL
      THROW 51000, '{"message":"organization_id is required"}', 1;

    IF @row_version_base64 IS NOT NULL
      SET @expected_row_version = CAST('' AS xml)
                                   .value('xs:base64Binary(sql:variable("@row_version_base64"))',
                                          'varbinary(8)');
    
     /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.organizations WHERE organization_id = @organization_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* Delete (soft) sin row_version */
    UPDATE dbo.organizations
       SET is_active = 0,
           updated_at = SYSUTCDATETIME(),
		   deleted_at = SYSUTCDATETIME(),
		   deleted_by = @deleted_by
     WHERE organization_id = @organization_id
       AND (@expected_row_version IS NULL OR row_version = @expected_row_version);

    IF @@ROWCOUNT = 0
      THROW 51000, '{"message":"organization not found or concurrency conflict"}', 1;

    /* Post-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.organizations WHERE organization_id = @organization_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

     /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_organization', @organization_id, 1,
        @before, @after,
        @observation, 0, 1,
        SYSUTCDATETIME(), @deleted_by
     );
    COMMIT;

    SELECT @organization_id AS organization_id,
           'deleted'        AS status
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

