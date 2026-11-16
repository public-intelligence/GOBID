/*--------------------------------------------------------------
Title:        read_access_types
Goal:         Return paged access-type rows with optional filters.
Parameters:
  @page_number int           (default 1)
  @page_size   int           (default 50)
  @filter      nvarchar(max) (optional) – { "name":"Read", "is_active":1 }
Sample output:
{
  "data":[ … ],
  "total_tally":123,
  "current_page":2,
  "total_pages":5
}
----------------------------------------------------------------*/
CREATE   PROCEDURE [dbo].[read_access_types]
  @page_number int           = 1,
  @page_size   int           = 50,
  @filter      nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  BEGIN TRY
    ;WITH filter_json AS (
      SELECT
        JSON_VALUE(@filter,'$.name')      AS name,
        JSON_VALUE(@filter,'$.is_active') AS is_active
    ),
    filtered AS (
      SELECT t.*
      FROM   dbo.access_types AS t
      CROSS  JOIN filter_json f
      WHERE  (@filter IS NULL OR f.name       IS NULL OR t.access_type_name LIKE '%' + f.name + '%')
        AND  (@filter IS NULL OR f.is_active  IS NULL OR t.is_active        =     f.is_active)
    ),
    numbered AS (
      SELECT *,
             ROW_NUMBER() OVER (ORDER BY access_type_id) AS rn,
             COUNT(*)   OVER ()                          AS total_tally
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
      (SELECT access_type_id,
              access_type_name,
              access_type_description,
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

