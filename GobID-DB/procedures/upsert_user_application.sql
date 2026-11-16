CREATE PROCEDURE [dbo].[upsert_user_application]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  /* ---- Parse JSON payload ------------------------------------------------- */
  DECLARE
    @user_application_id      uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.user_application_id')),
    @user_id                  uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.user_id')),
    @org_app_id               int              = TRY_CAST(JSON_VALUE(@payload,'$.organization_application_id') AS int),
    @descr                    nvarchar(500)    = JSON_VALUE(@payload,'$.user_application_description'),
    @is_admin                 bit              = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_application_admin')),0),
    @is_active                bit              = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')),1),
    @updated_by               uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.updated_by')),
    @expected_row_version_b64 nvarchar(200)    = JSON_VALUE(@payload,'$.expected_row_version');

  DECLARE @expected_row_version varbinary(8);
  IF @expected_row_version_b64 IS NOT NULL
    SELECT @expected_row_version =
           CAST('' AS xml).value(
             'xs:base64Binary(sql:variable("@expected_row_version_b64"))',
             'varbinary(8)'
           );

  BEGIN TRY
    BEGIN TRAN;

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    SELECT @before =
      (SELECT TOP 1 *
       FROM dbo.user_applications
       WHERE 
         (user_application_id = @user_application_id)
         OR (@user_application_id IS NULL 
             AND user_id = @user_id 
             AND organization_application_id = @org_app_id)
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* 🔥 OUTPUT con acción */
    DECLARE @out table
    (
      user_application_id uniqueidentifier,
      action nvarchar(10),
      was_created bit
    );

    MERGE dbo.user_applications WITH (HOLDLOCK) AS tgt
    USING (SELECT
             @user_application_id      AS user_application_id,
             @user_id                  AS user_id,
             @org_app_id               AS organization_application_id,
             @descr                    AS user_application_description,
             @is_admin                 AS is_application_admin,
             @is_active                AS is_active,
             @updated_by               AS updated_by,
             SYSUTCDATETIME()          AS stamp
          ) AS src
      ON (
            tgt.user_application_id = src.user_application_id
            OR (src.user_application_id IS NULL
                AND tgt.user_id = src.user_id
                AND tgt.organization_application_id = src.organization_application_id)
         )
         AND (@expected_row_version IS NULL
              OR tgt.row_version = @expected_row_version)

    WHEN MATCHED THEN
      UPDATE SET
        user_application_description = src.user_application_description,
        is_application_admin         = src.is_application_admin,
        is_active                    = src.is_active,
        updated_by                   = src.updated_by,
        updated_at                   = src.stamp

    WHEN NOT MATCHED THEN
      INSERT (
        user_id,
        organization_application_id,
        user_application_description,
        is_application_admin,
        is_active,
        created_by,
        created_at
      )
      VALUES (
        src.user_id,
        src.organization_application_id,
        src.user_application_description,
        src.is_application_admin,
        src.is_active,
        src.updated_by,
        src.stamp
      )

    OUTPUT
       INSERTED.user_application_id,
       $action,
       CASE WHEN $action = 'INSERT' THEN 1 ELSE 0 END
    INTO @out (user_application_id, action, was_created);

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51000, '{"message":"No se realizó ninguna acción"}', 1;

    /* 🔥 AFTER */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT *
       FROM dbo.user_applications
       WHERE user_application_id = (SELECT TOP 1 user_application_id FROM @out)
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
          'update_user_application',
          @user_application_id,
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

    /* ---- RESULT ----------------------------------------------------------- */
    SELECT TOP (1)
      user_application_id,
      was_created
    FROM @out
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;

    DECLARE @err nvarchar(2048) =
      (SELECT
         error_number   = ERROR_NUMBER(),
         error_severity = ERROR_SEVERITY(),
         error_state    = ERROR_STATE(),
         error_proc     = ERROR_PROCEDURE(),
         error_line     = ERROR_LINE(),
         error_message  = ERROR_MESSAGE()
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    THROW 51000, @err, 1;
  END CATCH
END;

GO

