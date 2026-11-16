CREATE PROCEDURE [dbo].[delete_org_app_map]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @org_app_id int = JSON_VALUE(@payload,'$.organization_application_id'),
    @deleted_by   uniqueidentifier = JSON_VALUE(@payload,'$.deleted_by'),
    @observation  nvarchar(255)    = JSON_VALUE(@payload,'$.observation');

  IF @org_app_id IS NULL
     THROW 51000, '{"msg":"organization_application_id is required"}', 1;

  BEGIN TRY
    BEGIN TRAN;

      /* Pre-image */
     DECLARE @before nvarchar(max);
     SELECT @before =
       (SELECT * FROM dbo.organization_applications WHERE  organization_application_id =  @org_app_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    UPDATE dbo.organization_applications
       SET is_active = 0
     WHERE organization_application_id = @org_app_id;

    IF @@ROWCOUNT = 0
       THROW 51000, '{"msg":"Not found"}', 1;

     /* Post-image */
     DECLARE @after nvarchar(max);
     SELECT @after =
       (SELECT * FROM dbo.organization_applications WHERE  organization_application_id =  @org_app_id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
   
    /* Change-log */
     INSERT dbo.change_logs (
        log_name, entity_id, is_deleting,
        before_update_entity_json, after_update_entity_json,
        observation, did_rollback, is_active,
        created_at, created_by
     )
     VALUES (
        'delete_org_app', @org_app_id, 1,
        @before, @after,
        @observation, 0, 1,
        SYSUTCDATETIME(), @deleted_by
     );


   SELECT @org_app_id AS organization_application_id
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    COMMIT;
  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    DECLARE @err nvarchar(2048) = (
      SELECT ERROR_NUMBER() AS num,
             ERROR_MESSAGE() AS msg
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    THROW 51000, @err, 1;
  END CATCH
END;

GO

