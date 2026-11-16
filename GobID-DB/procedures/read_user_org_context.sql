-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[read_user_org_context]
  @filter nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  /* ---- Extract dynamic filter values ------------------------------------- */
  DECLARE
    @f_user_id        uniqueidentifier = JSON_VALUE(@filter, '$.user_id'),
    @f_org_app_id     int              = JSON_VALUE(@filter, '$.organization_application_id'),
    @f_application_id uniqueidentifier = JSON_VALUE(@filter, '$.application_id'),
    @f_is_active      bit              = JSON_VALUE(@filter, '$.is_active');

  BEGIN TRY
    SELECT (
      SELECT TOP 1
        oa.organization_id   AS OrganizationId,
        o.organization_name  AS OrganizationName,
        o.acronym,
        r.role_name          AS RoleName,
        r.role_id            AS RoleId
      FROM   dbo.user_applications             ua
      INNER JOIN dbo.organization_applications oa ON ua.organization_application_id = oa.organization_application_id
      INNER JOIN dbo.organizations              o ON oa.organization_id              = o.organization_id
      INNER JOIN dbo.user_roles                ur ON ua.user_application_id =      ur.user_application_id AND ur.is_active = 1
      INNER JOIN dbo.roles                     r ON ur.role_id                  = r.role_id
      WHERE  (@f_user_id        IS NULL OR ua.user_id                     = @f_user_id)
        AND  (@f_org_app_id     IS NULL OR ua.organization_application_id = @f_org_app_id)
        AND  (@f_is_active      IS NULL OR ua.is_active                   = @f_is_active)
        AND  (@f_application_id IS NULL OR oa.application_id              = @f_application_id)
        AND  (@f_is_active      IS NULL OR oa.is_active                   = @f_is_active)
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS json_result;
  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) =
      (SELECT ERROR_NUMBER() AS number,
              ERROR_MESSAGE() AS message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END

GO

