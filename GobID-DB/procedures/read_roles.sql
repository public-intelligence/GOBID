/*--------------------------------------------------------------
Title:        read_role
Goal:         Paginated fetch of roles with optional filters.
Parameters:
  @page_number int            (optional, default 1)
  @page_size   int            (optional, default 50)
  @filter      nvarchar(max)  (optional JSON)
Sample filter:
{ "organization_application_id":12, "is_active":1 }
----------------------------------------------------------------*/
CREATE PROCEDURE [dbo].[read_roles]
  @page_number int = 1,
  @page_size   int = 50,
  @filter      nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  BEGIN TRY
    ;WITH filterin AS (
       SELECT
         JSON_VALUE(@filter,'$.organization_application_id') AS org_app_id,
         JSON_VALUE(@filter,'$.is_active')                     AS is_active,
		 JSON_VALUE(@filter,'$.role_id')                     AS role_id,
         JSON_VALUE(@filter,'$.search')              AS search,
         JSON_VALUE(@filter,'$.is_system_role')    AS is_system_role,
         TRY_CAST(JSON_VALUE(@filter,'$.exclude_position_roles') AS bit) AS exclude_position_roles
    ),
    base AS (
       SELECT  
          r.*,
          -- Conteo de usuarios con este rol
          (SELECT COUNT(*) 
           FROM dbo.user_roles ur 
           WHERE ur.role_id = r.role_id AND ur.is_active = 1) AS user_count,

          -- Conteo de claims asociados a este rol
          (SELECT COUNT(*) 
           FROM dbo.role_claims rc 
           INNER JOIN dbo.claims c on rc.claim_id = c.claim_id
           WHERE rc.role_id = r.role_id AND rc.is_active = 1 AND c.is_active = 1) AS claim_count
       FROM dbo.roles AS r
       CROSS JOIN filterin f
       WHERE r.is_active = 1  
         AND (@filter IS NULL OR f.org_app_id IS NULL OR r.organization_application_id = f.org_app_id)
         AND (@filter IS NULL OR f.role_id   IS NULL OR r.role_id = f.role_id)
         AND (@filter IS NULL OR f.is_active IS NULL OR r.is_active = f.is_active)  
         AND (@filter IS NULL OR f.search IS NULL OR r.role_name LIKE '%' + f.search + '%')
         AND r.is_system_role = 0
         AND (@filter IS NULL OR ISNULL(f.exclude_position_roles, 0) = 0 OR r.position_id IS NULL)
    ),
    numbered AS (
       SELECT *, 
              ROW_NUMBER() OVER (ORDER BY created_at DESC) AS rn,
              COUNT(*) OVER()                      AS total_tally
       FROM   base
    )
	SELECT (
    SELECT
      (SELECT total_tally,
              @page_number AS current_page,
              CEILING(total_tally * 1.0 / @page_size) AS total_pages
       FROM numbered
       WHERE rn=1
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS meta,
      (SELECT role_id, role_name, role_description, set_automatic_claims,
              organization_application_id, is_active, created_at, updated_at,
              user_count, claim_count, is_system_role, position_id  
       FROM   numbered
       WHERE  rn BETWEEN (@page_number-1)*@page_size+1 AND @page_number*@page_size
       ORDER  BY rn
       FOR JSON PATH)                         AS data
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS json_result;
  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) =
      (SELECT ERROR_NUMBER() AS number, ERROR_MESSAGE() AS message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

