CREATE PROCEDURE [dbo].[upsert_claim]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

    /*── parse JSON input ───────────────────────────────────────*/
    DECLARE
      @claim_id          int,
      @application_id    uniqueidentifier,
      @code              nvarchar(50),
      @claim_name        nvarchar(100),
      @claim_description nvarchar(500),
      @is_automatic      bit,
      @is_active         bit,
      @updated_by        uniqueidentifier;

    SELECT
      @claim_id          = TRY_CAST(JSON_VALUE(@payload,'$.claim_id') AS int),
      @application_id    = TRY_CAST(JSON_VALUE(@payload,'$.application_id') AS uniqueidentifier),
      @code              = JSON_VALUE(@payload,'$.code'),
      @claim_name        = JSON_VALUE(@payload,'$.claim_name'),
      @claim_description = JSON_VALUE(@payload,'$.claim_description'),
      @is_automatic      = TRY_CAST(JSON_VALUE(@payload,'$.is_automatic') AS bit),
      @is_active         = COALESCE(TRY_CAST(JSON_VALUE(@payload,'$.is_active') AS bit),1),
      @updated_by        = TRY_CAST(JSON_VALUE(@payload,'$.updated_by') AS uniqueidentifier);

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    IF @claim_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT * 
         FROM dbo.claims 
         WHERE claim_id = @claim_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT con acción */
    DECLARE @out table(
      action nvarchar(10),
      claim_id int
    );

    /*── MERGE ───────────────────────────────────────────────*/
    MERGE dbo.claims WITH (HOLDLOCK) AS tgt
    USING (SELECT @claim_id AS claim_id) AS src
      ON (
           (src.claim_id IS NOT NULL AND tgt.claim_id = src.claim_id)
        OR (src.claim_id IS NULL AND tgt.application_id = @application_id AND tgt.code = @code)
         )

    WHEN MATCHED THEN
      UPDATE SET
        claim_name        = @claim_name,
        is_automatic      = @is_automatic,
        claim_description = @claim_description,
        is_active         = @is_active,
        updated_at        = SYSUTCDATETIME(),
        updated_by        = @updated_by

    WHEN NOT MATCHED THEN
      INSERT (
        code,
        claim_name,
        is_automatic,
        claim_description,
        is_active,
        created_at,
        created_by,
        application_id
      )
      VALUES (
        @code,
        @claim_name,
        @is_automatic,
        @claim_description,
        @is_active,
        SYSUTCDATETIME(),
        @updated_by,
        @application_id
      )

    OUTPUT 
      $action,
      inserted.claim_id
    INTO @out;

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51001, '{"message":"No se pudo insertar o actualizar"}', 1;

    /* 🔥 AFTER (usar ID real) */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * 
       FROM dbo.claims 
       WHERE claim_id = (SELECT TOP 1 claim_id FROM @out)
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
          'update_claim',
          (SELECT TOP 1 claim_id FROM @out),
          0,
          @before,
          @after,
          NULL,
          0,
          1,
          SYSUTCDATETIME(),
          @updated_by
      );
    END

    COMMIT;

    /*── RESULT ───────────────────────────────────────────────*/
    SELECT *
    FROM @out
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;

    DECLARE @err nvarchar(2048) =
      (
        SELECT
          error_number   = ERROR_NUMBER(),
          error_severity = ERROR_SEVERITY(),
          error_state    = ERROR_STATE(),
          error_proc     = ERROR_PROCEDURE(),
          error_line     = ERROR_LINE(),
          error_message  = ERROR_MESSAGE()
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
      );

    THROW 51000, @err, 1;
  END CATCH
END;

GO

