-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[read_summary_counts]
  @excludeOrg     bit = 0,
  @organizationId int = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

  BEGIN TRY
    -- Conteo de organizaciones
    DECLARE @orgCount int = 0;
    IF (@excludeOrg = 0)
    BEGIN
        SELECT @orgCount = COUNT(*)
        FROM dbo.organizations o
        WHERE o.deleted_at IS NULL
          AND o.is_active = 1;
    END

    -- Conteo de usuarios
    DECLARE @userCount int = 0;
    IF (@excludeOrg = 0)
    BEGIN
        -- Todos los usuarios
        SELECT @userCount = COUNT(DISTINCT u.user_id)
        FROM dbo.users u
        WHERE u.deleted_at IS NULL
          AND u.is_active = 1;
    END
    ELSE
    BEGIN
        -- Solo usuarios de la organización dada
        SELECT @userCount = COUNT(DISTINCT uo.user_id)
        FROM dbo.user_organizations uo
        INNER JOIN dbo.users u ON u.user_id = uo.user_id
        WHERE u.deleted_at IS NULL
          AND u.is_active = 1
          AND uo.organization_id = @organizationId;
    END

    -- Conteo de aplicaciones
    DECLARE @appCount int = 0;
    IF (@excludeOrg = 0)
    BEGIN
        -- Todas las aplicaciones
        SELECT @appCount = COUNT(DISTINCT a.application_id)
        FROM dbo.applications a
        WHERE a.deleted_at IS NULL
          AND a.is_active = 1;
    END
    ELSE
    BEGIN
        -- Solo aplicaciones de la organización dada
        SELECT @appCount = COUNT(DISTINCT oa.application_id)
        FROM dbo.organization_applications oa
        INNER JOIN dbo.applications a ON a.application_id = oa.application_id
        WHERE a.deleted_at IS NULL
          AND a.is_active = 1
          AND oa.organization_id = @organizationId;
    END

    -- Retorno en formato JSON
    SELECT (
      SELECT
        @orgCount  AS totalOrganizations,
        @userCount AS totalUsers,
        @appCount  AS totalApplications
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS json_result;

  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) =
      (SELECT ERROR_NUMBER() AS error_number,
              ERROR_MESSAGE() AS error_message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

