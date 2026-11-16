/*--------------------------------------------------------------
Title:        read_app_accesses
Goal:         Return paged application-access rows, filterable by
             org-app, access-type or active flag.
Parameters:
  @page_number int           (default 1)
  @page_size   int           (default 50)
  @filter      nvarchar(max) (optional) – e.g.
               { "organization_application_id":12,
                 "access_type_id":3,
                 "is_active":1 }
----------------------------------------------------------------*/
CREATE   PROCEDURE [dbo].[read_app_accesses]
  @page_number int           = 1,
  @page_size   int           = 50,
  @filter      nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  BEGIN TRY
    ;WITH f AS (
      SELECT
        JSON_VALUE(@filter,'$.organization_application_id') AS org_app_id,
        JSON_VALUE(@filter,'$.access_type_id')              AS access_type_id,
        JSON_VALUE(@filter,'$.is_active')                   AS is_active
    ),
    filtered AS (
      SELECT a.*
      FROM   dbo.application_access AS a
      CROSS  JOIN f
      WHERE (@filter IS NULL OR f.org_app_id     IS NULL OR a.organization_application_id = f.org_app_id)
        AND (@filter IS NULL OR f.access_type_id IS NULL OR a.access_type_id              = f.access_type_id)
        AND (@filter IS NULL OR f.is_active      IS NULL OR a.is_active                   = f.is_active)
    ),
    numbered AS (
      SELECT *,
             ROW_NUMBER() OVER (ORDER BY application_access_id) AS rn,
             COUNT(*)   OVER ()                                  AS total_tally
      FROM   filtered
    )
	SELECT(
    SELECT
      /* meta */
      (SELECT TOP 1
              MAX(total_tally)                            AS total_tally,
              @page_number                                AS current_page,
              CEILING(MAX(total_tally)*1.0/@page_size)    AS total_pages
       FROM   numbered
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS meta,
      /* data */
      (SELECT application_access_id,
              organization_application_id,
              access_type_id,
              configuration_json,
              is_active,
              created_at,
              updated_at
       FROM   numbered
       WHERE  rn BETWEEN (@page_number-1)*@page_size+1
                   AND   @page_number*@page_size
       FOR JSON PATH)                             AS data
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

