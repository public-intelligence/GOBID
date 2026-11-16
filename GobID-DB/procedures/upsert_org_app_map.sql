CREATE PROCEDURE [dbo].[upsert_org_app_map]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @org_app_id  int = TRY_CAST(JSON_VALUE(@payload,'$.organization_application_id') AS int),
    @org_id      int = TRY_CAST(JSON_VALUE(@payload,'$.organization_id') AS int),
    @app_id      uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.application_id')),
    @is_active   bit = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')),1);

  BEGIN TRY
    BEGIN TRAN;

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    IF @org_app_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT * 
         FROM dbo.organization_applications 
         WHERE organization_application_id = @org_app_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT con acción */
    DECLARE @out table (
      action nvarchar(10),
      id int,
      rowver varbinary(8)
    );

    MERGE dbo.organization_applications WITH (HOLDLOCK) AS tgt
    USING (SELECT @org_app_id AS organization_application_id,
                  @org_id     AS organization_id,
                  @app_id     AS application_id,
                  @is_active  AS is_active) AS src
      ON tgt.organization_application_id = src.organization_application_id

    WHEN MATCHED THEN
      UPDATE SET
        organization_id = src.organization_id,
        application_id  = src.application_id,
        is_active       = src.is_active

    WHEN NOT MATCHED THEN
      INSERT (organization_id, application_id, is_active)
      VALUES (src.organization_id, src.application_id, src.is_active)

    OUTPUT 
      $action,
      inserted.organization_application_id,
      inserted.row_version
    INTO @out(action, id, rowver);

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51000, '{"message":"No se realizó ninguna acción"}', 1;

    /* 🔥 AFTER (usar ID real) */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * 
       FROM dbo.organization_applications 
       WHERE organization_application_id = (SELECT TOP 1 id FROM @out)
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* 🔥 LOG SOLO UPDATE REAL */
    IF EXISTS (SELECT 1 FROM @out WHERE action = 'UPDATE')
    BEGIN
      INSERT dbo.change_logs (
          log_name, entity_id, is_deleting,
          before_update_entity_json, after_update_entity_json,
          observation, did_rollback, is_active,
          created_at, created_by
      )
      VALUES (
          'update_org_app_map',
          (SELECT TOP 1 id FROM @out),
          0,
          @before,
          @after,
          NULL,
          0,
          1,
          SYSUTCDATETIME(),
          NULL
      );
    END

    /* RESULT */
    SELECT  
        id     AS organization_application_id,
        rowver AS row_version
    FROM @out
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    COMMIT;

  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;

    DECLARE @err nvarchar(2048) = (
      SELECT
        num      = ERROR_NUMBER(),
        severity = ERROR_SEVERITY(),
        state    = ERROR_STATE()
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    THROW 51000, @err, 1;
  END CATCH
END;

GO

