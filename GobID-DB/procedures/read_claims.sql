/*--------------------------------------------------------------
Title:        read_claims
Goal:         Return paged, filterable lists of claims.
Parameters:
  @page_number  int            (optional, default 1)
  @page_size    int            (optional, default 50)
  @filter       nvarchar(max)  (optional) – JSON filter
Sample filter:
{ "application_id":5, "text":"EMP" }
Sample output:
{
  "meta": {
    "total_tally": 23,
    "current_page": 1,
    "total_pages": 3
  },
  "data": [
     { "claim_id":17, … },
     …
  ]
}
----------------------------------------------------------------*/
CREATE PROCEDURE [dbo].[read_claims]
  @page_number int          = 1,
  @page_size   int          = 50,
  @filter      nvarchar(max)= NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  BEGIN TRY
    DECLARE
      @flt_app_id  uniqueidentifier          = JSON_VALUE(@filter,'$.application_id'),
      @flt_text    nvarchar(50) = JSON_VALUE(@filter,'$.text');

    ;WITH base AS (
      SELECT c.*
      FROM   dbo.claims AS c
      WHERE  c.is_active = 1  -- ✅ Solo registros activos
        AND  (@flt_app_id IS NULL OR c.application_id = @flt_app_id)
        AND  (@flt_text   IS NULL OR (
               c.code       LIKE CONCAT('%',@flt_text,'%') OR
               c.claim_name LIKE CONCAT('%',@flt_text,'%') ))
    ),
    numbered AS (
      SELECT *,
             
ROW_NUMBER() OVER (
    ORDER BY claim_name ASC, created_at DESC
) AS rn
,
             COUNT(*)  OVER ()                     AS total_tally
      FROM   base
    )
    SELECT (
      SELECT
        /* meta */
        ( SELECT DISTINCT
                 total_tally,
                 @page_number            AS current_page,
                 CEILING(total_tally*1.0/@page_size) AS total_pages
          FROM   numbered
          FOR JSON PATH, WITHOUT_ARRAY_WRAPPER ) AS meta,
        /* paged data */
        ( SELECT claim_id, application_id, code, claim_name,
                 claim_description, is_automatic, is_active, created_at, created_by,
                 updated_at, updated_by
          FROM   numbered
          WHERE  rn BETWEEN (@page_number-1)*@page_size+1 AND @page_number*@page_size
          FOR JSON PATH ) AS data
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS json_result;

  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) =
      ( SELECT ERROR_NUMBER() AS number, ERROR_MESSAGE() AS message
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER );
    THROW 51000, @err, 1;
  END CATCH
END;

GO

