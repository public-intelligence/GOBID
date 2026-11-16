CREATE PROCEDURE [dbo].[upsert_organization_contact]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

    DECLARE
      @contact_id                int           = TRY_CAST(JSON_VALUE(@payload,'$.contact_id') AS int),
      @organization_id           int           = TRY_CAST(JSON_VALUE(@payload,'$.organization_id') AS int),
      @organization_contact_name nvarchar(100) = JSON_VALUE(@payload,'$.organization_contact_name'),
      @position                  nvarchar(100) = JSON_VALUE(@payload,'$.position'),
      @email                     nvarchar(320) = JSON_VALUE(@payload,'$.email'),
      @phone                     nvarchar(20)  = JSON_VALUE(@payload,'$.phone'),
      @is_active                 bit           = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')),1);

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    IF @contact_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT * 
         FROM dbo.organization_contacts
         WHERE contact_id = @contact_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT con acción */
    DECLARE @output TABLE (
      id int,
      action nvarchar(10)
    );

    MERGE dbo.organization_contacts WITH (HOLDLOCK) AS tgt
    USING (SELECT
             @contact_id                AS contact_id,
             @organization_id           AS organization_id,
             @organization_contact_name AS organization_contact_name,
             @position                  AS position,
             @email                     AS email,
             @phone                     AS phone,
             @is_active                 AS is_active
           ) AS src
      ON tgt.contact_id = src.contact_id

    WHEN MATCHED THEN
      UPDATE SET
        organization_id           = src.organization_id,
        organization_contact_name = src.organization_contact_name,
        position                  = src.position,
        email                     = src.email,
        phone                     = src.phone,
        is_active                 = src.is_active,
        updated_at                = SYSUTCDATETIME()

    WHEN NOT MATCHED THEN
      INSERT (organization_id, organization_contact_name, position, email, phone, is_active, created_at)
      VALUES (src.organization_id, src.organization_contact_name, src.position,
              src.email, src.phone, src.is_active, SYSUTCDATETIME())

    OUTPUT 
      INSERTED.contact_id,
      $action
    INTO @output(id, action);

    IF NOT EXISTS (SELECT 1 FROM @output)
      THROW 51000, '{"message":"No rows affected"}', 1;

    /*DECLARE @new_id int = (SELECT TOP 1 id FROM @output);*/

    /* 🔥 AFTER */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * 
       FROM dbo.organization_contacts
       WHERE contact_id = @contact_id
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* 🔥 LOG SOLO UPDATE */
    IF EXISTS (SELECT 1 FROM @output WHERE action = 'UPDATE')
    BEGIN
      INSERT dbo.change_logs (
          log_name, entity_id, is_deleting,
          before_update_entity_json, after_update_entity_json,
          observation, did_rollback, is_active,
          created_at, created_by
      )
      VALUES (
          'update_organization_contact',
          @contact_id,
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

    SELECT id AS contact_id
    FROM @output
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;

    DECLARE @err nvarchar(2048) = (
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

