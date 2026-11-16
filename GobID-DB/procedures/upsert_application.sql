CREATE PROCEDURE [dbo].[upsert_application]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @application_id        uniqueidentifier,
    @application_name      nvarchar(255),
    @code                  nvarchar(50),
    @redirect_url          nvarchar(255),
    @application_desc      nvarchar(500),
    @is_active             bit           = 1,
    @created_by            uniqueidentifier,
    @updated_by            uniqueidentifier,
    @row_version_hex       nvarchar(64),
    @row_version           varbinary(8),
    @is_public             bit  = 0;

  /* Parse JSON */
  SELECT
     @application_id   = JSON_VALUE(@payload,'$.application_id'),
     @application_name = JSON_VALUE(@payload,'$.application_name'),
     @code             = JSON_VALUE(@payload,'$.code'),
     @redirect_url     = JSON_VALUE(@payload,'$.redirect_url'),
     @application_desc = JSON_VALUE(@payload,'$.application_description'),
     @is_active        = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')), 1),
     @created_by       = TRY_CONVERT(uniqueidentifier,JSON_VALUE(@payload,'$.created_by')),
     @updated_by       = TRY_CONVERT(uniqueidentifier,JSON_VALUE(@payload,'$.updated_by')),
     @row_version_hex  = JSON_VALUE(@payload,'$.row_version'),
     @is_public        = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_public')), 0);

  IF @row_version_hex IS NOT NULL
     SET @row_version = CONVERT(varbinary(8),@row_version_hex,1);

  BEGIN TRY
    BEGIN TRANSACTION;

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    IF @application_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT * 
         FROM dbo.applications 
         WHERE application_id = @application_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT con acción */
    DECLARE @out table(
      action nvarchar(10),
      application_id uniqueidentifier, 
      row_version varbinary(8)
    );

    MERGE dbo.applications WITH (HOLDLOCK) AS T
    USING (VALUES(@application_id)) AS S(application_id)
    ON T.application_id = S.application_id

    WHEN MATCHED AND
         (@row_version IS NULL OR T.row_version = @row_version)
    THEN UPDATE SET
         application_name        = @application_name,
         code                    = @code,
         redirect_url            = @redirect_url,
         application_description = @application_desc,
         is_active               = @is_active,
         updated_at              = SYSUTCDATETIME(),
         updated_by              = @updated_by,
         is_public               = @is_public

    WHEN NOT MATCHED BY TARGET THEN
         INSERT (application_name, code, redirect_url,
                 application_description, is_active,
                 random_string, created_at, created_by, is_public)
         VALUES(@application_name, @code, @redirect_url,
                @application_desc, @is_active,
                SUBSTRING(CONVERT(varchar(40), NEWID()), 0, 13), 
                SYSUTCDATETIME(), @created_by, @is_public)

    OUTPUT 
      $action,
      inserted.application_id,
      inserted.row_version
    INTO @out;

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51001, '{"message":"Concurrency violation or invalid ID."}', 1;

    /* 🔥 AFTER */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * 
       FROM dbo.applications 
       WHERE application_id = (SELECT TOP 1 application_id FROM @out)
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
          'update_application',
          @application_id,
          0,
          @before,
          @after,
          NULL,
          0,
          1,
          SYSUTCDATETIME(),
          ISNULL(@updated_by, @created_by)
      );
    END

    /* OUTPUT final */
    SELECT
       application_id,
       row_version = '0x'+LOWER(CONVERT(char(16),row_version,2))
    FROM @out
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    COMMIT;

  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;

    DECLARE @err nvarchar(2048) = (
        SELECT ERROR_NUMBER()      AS error_number,
               ERROR_SEVERITY()    AS error_severity,
               ERROR_STATE()       AS error_state,
               ERROR_PROCEDURE()   AS error_proc,
               ERROR_LINE()        AS error_line,
               ERROR_MESSAGE()     AS error_message
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    THROW 51000, @err, 1;
  END CATCH
END;

GO

