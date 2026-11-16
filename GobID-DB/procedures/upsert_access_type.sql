CREATE PROCEDURE [dbo].[upsert_access_type]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  DECLARE
    @access_type_id          int,
    @access_type_name        nvarchar(100),
    @access_type_description nvarchar(500),
    @is_active               bit = 1;

  SELECT
    @access_type_id          = TRY_CAST(JSON_VALUE(@payload,'$.access_type_id') AS int),
    @access_type_name        = JSON_VALUE(@payload,'$.access_type_name'),
    @access_type_description = JSON_VALUE(@payload,'$.access_type_description'),
    @is_active               = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')),1);

  BEGIN TRY
    BEGIN TRAN;

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    IF @access_type_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT * 
         FROM dbo.access_types
         WHERE access_type_id = @access_type_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT con acción */
    DECLARE @out TABLE (
      action nvarchar(10),
      access_type_id int
    );

    MERGE dbo.access_types WITH (HOLDLOCK) AS tgt
    USING (SELECT @access_type_id AS access_type_id) AS src
      ON tgt.access_type_id = src.access_type_id

    WHEN MATCHED THEN
      UPDATE SET
        access_type_name        = @access_type_name,
        access_type_description = @access_type_description,
        is_active               = @is_active,
        updated_at              = SYSUTCDATETIME()

    WHEN NOT MATCHED THEN
      INSERT (access_type_name, access_type_description, is_active, created_at)
      VALUES (@access_type_name, @access_type_description, @is_active, SYSUTCDATETIME())

    OUTPUT 
      $action,
      INSERTED.access_type_id
    INTO @out;

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51001, '{"message":"Concurrency violation or invalid ID."}', 1;

    /* 🔥 AFTER */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * 
       FROM dbo.access_types
       WHERE access_type_id = (SELECT TOP 1 access_type_id FROM @out)
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
          'update_access_type',
          (SELECT TOP 1 access_type_id FROM @out),
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
      SELECT
        ERROR_NUMBER()   AS number,
        ERROR_SEVERITY() AS severity,
        ERROR_LINE()     AS line,
        ERROR_MESSAGE()  AS message
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    THROW 51000, @err, 1;
  END CATCH
END;

GO

