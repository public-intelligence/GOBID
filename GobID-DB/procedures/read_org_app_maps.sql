
CREATE PROCEDURE [dbo].[read_org_app_maps]
  @page_number   int           = 1,
  @page_size     int           = 50,
  @filter        nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  DECLARE
    @f_org_id int = NULL,
    @f_app_id uniqueidentifier = NULL,
    @f_active bit = NULL,
    @exclude_app2 bit = NULL,
    @f_org_app int = NULL,
    @f_search nvarchar(255) = NULL;

  IF @filter IS NOT NULL
  BEGIN
    SELECT
      @f_org_id = TRY_CAST(JSON_VALUE(@filter,'$.organization_id') AS int),
      @f_app_id = TRY_CAST(JSON_VALUE(@filter,'$.application_id') AS uniqueidentifier),
      @f_active = TRY_CAST(JSON_VALUE(@filter,'$.is_active') AS bit),
      @f_org_app = TRY_CAST(JSON_VALUE(@filter,'$.organization_application_id') AS int),
      @exclude_app2 = TRY_CAST(JSON_VALUE(@filter,'$.exclude_app2') AS bit),
      @f_search = JSON_VALUE(@filter,'$.search');
  END;

  BEGIN TRY
     ;WITH base AS (
        SELECT 
            oa.*,
            o.organization_name,
            a.created_at AS app_created_at
        FROM dbo.organization_applications oa
        INNER JOIN dbo.organizations o 
            ON o.organization_id = oa.organization_id
        INNER JOIN dbo.applications a 
            ON a.application_id  = oa.application_id
        WHERE (@f_org_id IS NULL OR oa.organization_id = @f_org_id)
          AND (@f_app_id IS NULL OR oa.application_id   = @f_app_id)
          AND (@f_active IS NULL OR oa.is_active        = @f_active)
          AND (@f_org_app IS NULL OR oa.organization_application_id = @f_org_app)
          AND (@exclude_app2 IS NULL OR a.code NOT IN ('gobIDLocal','gobId','APP3'))
          AND (
                @f_search IS NULL 
                OR o.organization_name LIKE '%' + @f_search + '%' 
                OR a.application_name LIKE '%' + @f_search + '%' 
                OR o.acronym LIKE '%' + @f_search + '%' 
                OR a.code LIKE '%' + @f_search + '%'
              )
          AND o.deleted_at IS NULL
          AND a.deleted_at IS NULL
     ),
     numbered AS (
        SELECT *,
               ROW_NUMBER() OVER (ORDER BY app_created_at DESC) AS rn
        FROM base
     ),
     tally AS (
        SELECT COUNT(*) AS total_tally FROM base
     )
     SELECT (
        SELECT 
           t.total_tally                              AS totalTally,
           @page_number                               AS currentPage,
           @page_size                                 AS pageSize,
           CEILING(t.total_tally * 1.0 / @page_size)  AS totalPages,
           (SELECT 
                organization_application_id,
                organization_id,
                organization_name, 
                application_id,
                is_active
            FROM numbered
            WHERE rn BETWEEN (@page_number-1)*@page_size+1
                         AND   @page_number*@page_size
            ORDER BY rn
            FOR JSON PATH
           ) AS data
        FROM tally t
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
     ) AS json_result;
  END TRY
  BEGIN CATCH
     DECLARE @err nvarchar(2048) = (
        SELECT ERROR_NUMBER()  AS number,
               ERROR_MESSAGE() AS message
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
     );
     THROW 51000, @err, 1;
  END CATCH
END;

GO

