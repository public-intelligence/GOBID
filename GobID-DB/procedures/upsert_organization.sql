CREATE PROCEDURE [dbo].[upsert_organization]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

    DECLARE
      @organization_id          int            = TRY_CAST(JSON_VALUE(@payload,'$.organization_id') AS int),
      @organization_name        nvarchar(100)  = JSON_VALUE(@payload,'$.organization_name'),
      @acronym                  nvarchar(10)   = JSON_VALUE(@payload,'$.acronym'),
      @organization_description nvarchar(255)  = JSON_VALUE(@payload,'$.organization_description'),
      @slogan                   nvarchar(255)  = JSON_VALUE(@payload,'$.slogan'),
      @website                  nvarchar(200)  = JSON_VALUE(@payload,'$.website'),
      @email                    nvarchar(320)  = JSON_VALUE(@payload,'$.email'),
      @organization_address     nvarchar(255)  = JSON_VALUE(@payload,'$.organization_address'),
      @phone                    nvarchar(20)   = JSON_VALUE(@payload,'$.phone'),
      @is_active                bit            = ISNULL(TRY_CONVERT(bit, JSON_VALUE(@payload,'$.is_active')),1),
      @legal_entity_id          uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.legal_entity_id'));

    /* ================= VALIDACIONES ================= */

    IF EXISTS (
        SELECT 1 FROM dbo.organizations
        WHERE organization_name = @organization_name
          AND organization_id  <> ISNULL(@organization_id, 0)
          AND is_active = 1
    )
      THROW 51000, '{"message":"Ya existe una organizacion con el mismo nombre."}', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.organizations
        WHERE acronym = @acronym
          AND organization_id <> ISNULL(@organization_id, 0)
          AND is_active = 1
    )
      THROW 51000, '{"message":"Ya existe una organizacion con el mismo acronimo."}', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.organizations
        WHERE legal_entity_id = @legal_entity_id
          AND organization_id <> ISNULL(@organization_id, 0)
          AND is_active = 1
    )
      THROW 51000, '{"message":"Ya existe una organizacion con la misma entidad legal."}', 1;

    /* 🔥 BEFORE */
    DECLARE @before nvarchar(max);

    IF @organization_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT * 
         FROM dbo.organizations
         WHERE organization_id = @organization_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    /* 🔥 OUTPUT con acción */
    DECLARE @output TABLE (
      id int,
      action nvarchar(10)
    );

    MERGE dbo.organizations WITH (HOLDLOCK) AS tgt
    USING (SELECT
             @organization_id          AS organization_id,
             @organization_name        AS organization_name,
             @acronym                  AS acronym,
             @organization_description AS organization_description,
             @slogan                   AS slogan,
             @website                  AS website,
             @email                    AS email,
             @organization_address     AS organization_address,
             @phone                    AS phone,
             @is_active                AS is_active,
             @legal_entity_id          AS legal_entity_id
           ) AS src
      ON tgt.organization_id = src.organization_id

    WHEN MATCHED THEN
      UPDATE SET
        organization_name        = src.organization_name,
        acronym                  = src.acronym,
        organization_description = src.organization_description,
        slogan                   = src.slogan,
        website                  = src.website,
        email                    = src.email,
        organization_address     = src.organization_address,
        phone                    = src.phone,
        is_active                = src.is_active,
        legal_entity_id          = src.legal_entity_id,
        updated_at               = SYSUTCDATETIME()

    WHEN NOT MATCHED THEN
      INSERT (organization_name, acronym, organization_description, slogan,
              website, email, organization_address, phone, is_active, legal_entity_id, created_at)
      VALUES (src.organization_name, src.acronym, src.organization_description, src.slogan,
              src.website, src.email, src.organization_address, src.phone,
              src.is_active, src.legal_entity_id, SYSUTCDATETIME())

    OUTPUT INSERTED.organization_id, $action
    INTO @output(id, action);

    IF NOT EXISTS (SELECT 1 FROM @output)
      THROW 51000, '{"message":"No rows affected"}', 1;

    DECLARE @new_org_id int = (SELECT TOP 1 id FROM @output);
    DECLARE @action nvarchar(10) = (SELECT TOP 1 action FROM @output);

    /* 🔥 AFTER */
    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * 
       FROM dbo.organizations
       WHERE organization_id = @new_org_id
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* 🔥 LOG SOLO UPDATE */
    IF @action = 'UPDATE'
    BEGIN
      INSERT dbo.change_logs (
          log_name, entity_id, is_deleting,
          before_update_entity_json, after_update_entity_json,
          observation, did_rollback, is_active,
          created_at, created_by
      )
      VALUES (
          'update_organization',
          @organization_id,
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

    /* 🔥 TU LÓGICA ORIGINAL (solo INSERT) */
    IF @action = 'INSERT'
    BEGIN
        DECLARE @new_org_app_id INT;

        INSERT INTO dbo.organization_applications (
            organization_id, application_id, is_active
        )
        VALUES (
            @new_org_id, 'DF7FF72A-5321-F111-B7F2-6045BDB142AD', 1
        );

        SET @new_org_app_id = SCOPE_IDENTITY();

        DECLARE @new_role_id INT;

        INSERT INTO dbo.roles (
            role_name, role_description, set_automatic_claims,
            organization_application_id, is_active, created_at, is_system_role
        )
        VALUES (
            'Administrador local', 'Rol administrador local por defecto',
            1, @new_org_app_id, 1, SYSUTCDATETIME(), 0
        );

        SET @new_role_id = SCOPE_IDENTITY();

        INSERT INTO dbo.role_claims (role_id, claim_id)
        SELECT @new_role_id, c.claim_id
        FROM dbo.claims c
        WHERE c.is_automatic = 1
          AND c.application_id = 'DF7FF72A-5321-F111-B7F2-6045BDB142AD';
    END

    COMMIT;

    SELECT @new_org_id AS organization_id
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;

    DECLARE @err nvarchar(2048) = ERROR_MESSAGE();

    IF ERROR_NUMBER() = 51000
        THROW 51000, @err, 1;

    DECLARE @errJson nvarchar(2048) = (
      SELECT message = ERROR_MESSAGE()
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    THROW 51000, @errJson, 1;
  END CATCH
END;

GO

