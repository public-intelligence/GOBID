 
CREATE PROCEDURE [dbo].[read_authenticate_user]
    @payload NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    DECLARE
        @login         NVARCHAR(320)    = JSON_VALUE(@payload, '$.login'),
        @app_id        UNIQUEIDENTIFIER = JSON_VALUE(@payload, '$.application_id'),
        @ip            NVARCHAR(45)     = JSON_VALUE(@payload, '$.ip'),
        @hdrs          NVARCHAR(255)    = JSON_VALUE(@payload, '$.headers'),
        @user_id       UNIQUEIDENTIFIER,
        @org_app_id    INT,
        @success       BIT = 0,
        @is_public_app BIT = 0;

    BEGIN TRY

        -- 1. Busca el usuario con al menos una vinculacion activa
        --    users -> user_applications -> organization_applications
        --    donde organization_applications.application_id = @app_id.
        SELECT TOP (1)
            @user_id    = u.user_id,
            @org_app_id = oa.organization_application_id
        FROM dbo.users u
        INNER JOIN dbo.user_applications ua
            ON ua.user_id = u.user_id
        INNER JOIN dbo.organization_applications oa
            ON oa.organization_application_id = ua.organization_application_id
        WHERE u.email         = @login
          AND oa.application_id = @app_id
          AND u.is_active     = 1
          AND ua.is_active    = 1
          AND oa.is_active    = 1
          AND (u.discharge_date IS NULL
               OR u.discharge_date >= CAST(SYSUTCDATETIME() AS DATE));

        -- 2. Si no tiene user_applications vinculadas, verificar si la aplicacion
        --    es publica (is_public = 1). En ese caso se permite el acceso como
        --    usuario externo; los permisos por defecto se asignan mas tarde en
        --    ExchangeCodeForTokensAsync mediante los roles con set_automatic_claims.
        IF @user_id IS NULL
        BEGIN
            SELECT @is_public_app = ISNULL(a.is_public, 0)
            FROM dbo.applications a
            WHERE a.application_id = @app_id
              AND a.is_active = 1;

            IF @is_public_app = 1
            BEGIN
                -- Buscar el usuario solo por email, sin requerir user_applications.
                SELECT TOP (1)
                    @user_id = u.user_id
                FROM dbo.users u
                WHERE u.email     = @login
                  AND u.is_active = 1
                  AND (u.discharge_date IS NULL
                       OR u.discharge_date >= CAST(SYSUTCDATETIME() AS DATE));

                -- Para el log de acceso, usar cualquier org_app activo de la aplicacion.
                IF @user_id IS NOT NULL
                    SELECT TOP (1)
                        @org_app_id = oa.organization_application_id
                    FROM dbo.organization_applications oa
                    WHERE oa.application_id = @app_id
                      AND oa.is_active = 1;
            END
        END

        SET @success = CASE WHEN @user_id IS NOT NULL THEN 1 ELSE 0 END;


        -- 4. Si no se encontro usuario, devolver vacio.
        IF @success = 0
        BEGIN
            SELECT NULL WHERE 1 = 0;
            RETURN;
        END

        -- 5. Devolver datos basicos del usuario.
        --    organization_application_id, organization_id, claims y permisos
        --    se resuelven despues en ExchangeCodeForTokensAsync cuando el
        --    frontend envia el orgAppId seleccionado.
                SELECT
            user_id              = u.user_id,
            personnel_id         = u.personnel_id,
            application_id       = @app_id,
            must_change_password = u.must_change_password,
            user_password        = u.user_password,
            application_name     = a.application_name,
            user_application_id  = ua.user_application_id,
            person_position_id   = u.person_position_id
        FROM dbo.users u
        INNER JOIN dbo.applications a
            ON a.application_id = @app_id
        LEFT JOIN dbo.user_applications ua
            ON  ua.user_id                    = @user_id
            AND ua.organization_application_id = @org_app_id
            AND ua.is_active                  = 1
        WHERE u.user_id = @user_id
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
    END TRY
    BEGIN CATCH
        SELECT NULL WHERE 1 = 0;
    END CATCH
END

GO

