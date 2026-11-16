CREATE PROCEDURE [dbo].[upsert_role]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @role_id                     int             = JSON_VALUE(@payload,'$.role_id'),
    @role_name                   nvarchar(255)   = JSON_VALUE(@payload,'$.role_name'),
    @role_description            nvarchar(500)   = JSON_VALUE(@payload,'$.role_description'),
    @set_automatic_claims        bit             = TRY_CONVERT(bit, JSON_VALUE(@payload,'$.set_automatic_claims')),
    @organization_application_id int             = JSON_VALUE(@payload,'$.organization_application_id'),
    @is_active                   bit             = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')),1),
    @created_by                  uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.created_by')),
    @updated_by                  uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.updated_by')),
    @is_system_role              bit              = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_system_role')),0),
    @position_id                 uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.position_id'));

  BEGIN TRY
    BEGIN TRANSACTION;

    -- 🔹 Validación de duplicado
    IF EXISTS (
        SELECT 1
        FROM dbo.roles
        WHERE role_name = @role_name
          AND organization_application_id = @organization_application_id
          AND is_active = 1
          AND (@role_id IS NULL OR role_id <> @role_id)
    )
        THROW 51001, '{"error":"Ya existe un rol con ese nombre en esta aplicación"}', 1;

    /* 🔥 BEFORE (solo si viene ID) */
    DECLARE @before nvarchar(max);

    IF @role_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT *
         FROM dbo.roles
         WHERE role_id = @role_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT con acción */
    DECLARE @out table(
      action nvarchar(10),
      role_id int
    );

    MERGE dbo.roles WITH (HOLDLOCK) AS tgt
    USING (SELECT
              @role_id                     AS role_id,
              @role_name                   AS role_name,
              @role_description            AS role_description,
              @set_automatic_claims        AS set_automatic_claims,
              @organization_application_id AS organization_application_id,
              @is_active                   AS is_active,
              @created_by                  AS created_by,
              @updated_by                  AS updated_by,
              @is_system_role              AS is_system_role,
              @position_id                 AS position_id) AS src
      ON tgt.role_id = src.role_id

    WHEN MATCHED THEN
      UPDATE SET
         role_name                   = src.role_name,
         role_description            = src.role_description,
         set_automatic_claims        = src.set_automatic_claims,
         organization_application_id = src.organization_application_id,
         is_active                   = src.is_active,
         updated_at                  = SYSUTCDATETIME(),
         updated_by                  = src.updated_by,
         is_system_role              = src.is_system_role,
         position_id                 = src.position_id

    WHEN NOT MATCHED THEN
      INSERT (role_name, role_description, set_automatic_claims,
              organization_application_id, is_active, created_at, created_by, is_system_role, position_id)
      VALUES (src.role_name, src.role_description, src.set_automatic_claims,
              src.organization_application_id, src.is_active,
              SYSUTCDATETIME(), src.created_by, src.is_system_role, src.position_id)

    OUTPUT
      $action,
      INSERTED.role_id
    INTO @out(action, role_id);

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51001, '{"message":"Concurrency violation or invalid ID."}', 1;

    DECLARE @saved_role_id int = (SELECT TOP 1 role_id FROM @out);

    IF @set_automatic_claims = 1
    BEGIN
      DECLARE @automatic_claims table (claim_id int PRIMARY KEY);

      INSERT @automatic_claims (claim_id)
      SELECT c.claim_id
      FROM dbo.claims c
      INNER JOIN dbo.organization_applications oa
        ON oa.application_id = c.application_id
      WHERE oa.organization_application_id = @organization_application_id
        AND c.is_automatic = 1
        AND c.is_active = 1;

      UPDATE rc
         SET is_active  = 1,
             updated_at = SYSUTCDATETIME(),
             updated_by = ISNULL(@updated_by, @created_by)
      FROM dbo.role_claims rc
      INNER JOIN @automatic_claims ac ON ac.claim_id = rc.claim_id
      WHERE rc.role_id = @saved_role_id
        AND rc.is_active = 0
        AND NOT EXISTS (
            SELECT 1 FROM dbo.role_claims x
            WHERE x.role_id = @saved_role_id
              AND x.claim_id = rc.claim_id
              AND x.is_active = 1
        );

      INSERT dbo.role_claims (role_id, claim_id, is_active, created_at, created_by)
      SELECT @saved_role_id, ac.claim_id, 1, SYSUTCDATETIME(), ISNULL(@created_by, @updated_by)
      FROM @automatic_claims ac
      WHERE NOT EXISTS (
          SELECT 1 FROM dbo.role_claims rc
          WHERE rc.role_id = @saved_role_id
            AND rc.claim_id = ac.claim_id
      );
    END

    /* 🔥 AFTER */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT *
       FROM dbo.roles
       WHERE role_id = @saved_role_id
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* 🔥 LOG SOLO CUANDO ES UPDATE REAL */
    IF EXISTS (SELECT 1 FROM @out WHERE action = 'UPDATE')
    BEGIN
      INSERT dbo.change_logs (
          log_name, entity_id, is_deleting,
          before_update_entity_json, after_update_entity_json,
          observation, did_rollback, is_active,
          created_at, created_by
      )
      VALUES (
          'update_role',
          @saved_role_id,
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

    SELECT role_id
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

