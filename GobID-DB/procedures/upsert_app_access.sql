CREATE PROCEDURE [dbo].[upsert_app_access]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  DECLARE
    @application_access_id       int,
    @organization_application_id int,
    @access_type_id              int,
    @configuration_json          nvarchar(max),
    @is_active                   bit = 1;

  SELECT
    @application_access_id       = TRY_CAST(JSON_VALUE(@payload,'$.application_access_id') AS int),
    @organization_application_id = TRY_CAST(JSON_VALUE(@payload,'$.organization_application_id') AS int),
    @access_type_id              = TRY_CAST(JSON_VALUE(@payload,'$.access_type_id') AS int),
    @configuration_json          = JSON_VALUE(@payload,'$.configuration_json'),
    @is_active                   = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')),1);

  BEGIN TRY
    BEGIN TRAN;

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    IF @application_access_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT * 
         FROM dbo.application_access
         WHERE application_access_id = @application_access_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT con acción */
    DECLARE @out TABLE (
      action nvarchar(10),
      application_access_id int
    );

    MERGE dbo.application_access WITH (HOLDLOCK) AS tgt
    USING (SELECT @application_access_id AS application_access_id) AS src
    ON tgt.application_access_id = src.application_access_id

    WHEN MATCHED THEN
      UPDATE SET
        organization_application_id = @organization_application_id,
        access_type_id              = @access_type_id,
        configuration_json          = @configuration_json,
        is_active                   = @is_active,
        updated_at                  = SYSUTCDATETIME()

    WHEN NOT MATCHED THEN
      INSERT (organization_application_id, access_type_id,
              configuration_json, is_active, created_at)
      VALUES (@organization_application_id, @access_type_id,
              @configuration_json, @is_active, SYSUTCDATETIME())

    OUTPUT 
      $action,
      INSERTED.application_access_id
    INTO @out;

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51001, '{"message":"No se pudo insertar o actualizar"}', 1;

    /* 🔥 AFTER (usar ID real) */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * 
       FROM dbo.application_access
       WHERE application_access_id = (SELECT TOP 1 application_access_id FROM @out)
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
          'update_app_access',
          (SELECT TOP 1 application_access_id FROM @out),
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

    COMMIT;

    SELECT * FROM @out
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;

    DECLARE @err nvarchar(2048) = (
      SELECT ERROR_NUMBER()  AS number,
             ERROR_SEVERITY()AS severity,
             ERROR_STATE()   AS state,
             ERROR_LINE()    AS line,
             ERROR_MESSAGE() AS message
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    THROW 51000, @err, 1;
  END CATCH
END;

GO

