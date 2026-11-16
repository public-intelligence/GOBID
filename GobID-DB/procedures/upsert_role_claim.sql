CREATE PROCEDURE [dbo].[upsert_role_claim]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @role_id     int              = TRY_CAST(JSON_VALUE(@payload,'$.role_id') AS int),
    @claim_id    int              = TRY_CAST(JSON_VALUE(@payload,'$.claim_id') AS int),
    @is_active   bit              = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')),1),
    @created_by  uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.created_by')),
    @updated_by  uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.updated_by'));

  BEGIN TRY
    BEGIN TRANSACTION;

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    SELECT @before =
      (SELECT * 
       FROM dbo.role_claims
       WHERE role_id = @role_id AND claim_id = @claim_id
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* 🔥 OUTPUT con acción */
    DECLARE @out table(
      action nvarchar(10),
      role_claim_id int
    );

    MERGE dbo.role_claims WITH (HOLDLOCK) AS tgt
    USING (SELECT @role_id AS role_id, @claim_id AS claim_id) AS src
      ON tgt.role_id = src.role_id AND tgt.claim_id = src.claim_id

    WHEN MATCHED THEN
      UPDATE SET
         is_active = @is_active,
         updated_at = SYSUTCDATETIME(),
         updated_by = @updated_by

    WHEN NOT MATCHED THEN
      INSERT (role_id, claim_id, is_active, created_at, created_by)
      VALUES (@role_id, @claim_id, @is_active, SYSUTCDATETIME(), @created_by)

    OUTPUT 
      $action,
      INSERTED.role_claim_id
    INTO @out;

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51000, '{"message":"No se realizó ninguna acción"}', 1;

    /* 🔥 AFTER */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * 
       FROM dbo.role_claims
       WHERE role_claim_id = (SELECT TOP 1 role_claim_id FROM @out)
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
          'update_role_claim',
          (SELECT TOP 1 role_claim_id FROM @out),
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

    COMMIT;

    SELECT role_claim_id 
    FROM @out
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

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

