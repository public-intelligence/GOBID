/*--------------------------------------------------------------
Title:        read_role_claim
Goal:         Paginated list of claim assignments on roles.
Parameters:
  @page_number int           (optional, default 1)
  @page_size   int           (optional, default 50)
  @filter      nvarchar(max) (optional JSON)
Sample filter:
{ "role_id":7, "is_active":1 }
----------------------------------------------------------------*/
CREATE   PROCEDURE [dbo].[read_role_claims]
  @page_number int           = 1,
  @page_size   int           = 50,
  @filter      nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  BEGIN TRY
    ;WITH filterin AS (
       SELECT
         JSON_VALUE(@filter,'$.role_id')  AS role_id,
         JSON_VALUE(@filter,'$.is_active') AS is_active
    ),
    base AS (
       SELECT rc.*, c.claim_name
       FROM   dbo.role_claims AS rc
       INNER  JOIN dbo.claims AS c ON c.claim_id = rc.claim_id
       CROSS JOIN filterin f
       WHERE (@filter IS NULL OR f.role_id IS NULL OR rc.role_id = f.role_id)
         AND (@filter IS NULL OR f.is_active IS NULL OR rc.is_active = f.is_active)
    ),
    numbered AS (
       SELECT *, 
ROW_NUMBER() OVER (
    ORDER BY claim_name ASC, created_at DESC
) AS rn
,
              COUNT(*) OVER()                               AS total_tally
       FROM   base
    )
	SELECT (
    SELECT
      (SELECT total_tally,
              @page_number AS current_page,
              CEILING(total_tally * 1.0 / @page_size) AS total_pages
       FROM numbered
       WHERE rn = 1
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS meta,
      (SELECT role_claim_id, role_id, claim_id, claim_name,
              is_active, created_at, updated_at
       FROM numbered
       WHERE rn BETWEEN (@page_number-1)*@page_size+1 AND @page_number*@page_size
       ORDER BY rn
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

