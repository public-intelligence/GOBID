/*--------------------------------------------------------------
Title:        read_applications
Goal:         Return a paged, filterable list (or single row) of
              applications.  Uses SERIALIZABLE isolation for
              gap-free reads.
Parameters:
  @page_number int           (default 1)
  @page_size   int           (default 50)
  @filter      nvarchar(max) JSON object; any of:
                { "application_id": 3,
                  "code"          : "BILLPORT",
                  "is_active"     : 1,
                  "search"        : "billing" }

Sample output:
{
  "meta":{
      "total_tally":1,
      "current_page":1,
      "total_pages":1
  },
  "data":[
     {
       "application_id":3,
       "application_name":"Billing Portal v2",
       "code":"BILLPORT",
       "redirect_url":"https://…",
       "application_description":"Customer invoice portal (v2)",
       "is_active":1,
       "created_at":"2025-06-27T21:05:11.1300000",
       "updated_at":"2025-06-28T14:33:09.4500000"
     }
  ]
}
----------------------------------------------------------------*/
CREATE PROCEDURE [dbo].[read_applications]
  @page_number int          = 1,
  @page_size   int          = 50,
  @filter      nvarchar(max)= NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  DECLARE
    @f_id        uniqueidentifier = NULL,
    @f_code      nvarchar(50) = NULL,
    @f_active    bit          = NULL,
    @f_search    nvarchar(255)= NULL,
    @f_public    bit   =null;

  /* Pull known filter fields if present */
  IF @filter IS NOT NULL
  BEGIN
    SELECT
      @f_id     = TRY_CAST(JSON_VALUE(@filter,'$.application_id') AS uniqueidentifier),
      @f_code   = JSON_VALUE(@filter,'$.code'),
      @f_active = TRY_CAST(JSON_VALUE(@filter,'$.is_active') AS bit),
      @f_search = JSON_VALUE(@filter,'$.search'),
      @f_public = TRY_CAST(JSON_VALUE(@filter,'$.is_public') AS bit);
  END;

  BEGIN TRY
     ;WITH base AS (
        SELECT *
        FROM dbo.applications
        WHERE
              (@f_id     IS NULL OR application_id   = @f_id)
          AND (@f_code   IS NULL OR code            = @f_code)
          AND (@f_active IS NULL OR is_active       = @f_active)
          AND (@f_public IS NULL OR is_public       = @f_public)
          AND deleted_at IS NULL
          AND (@f_search IS NULL OR (
                 application_name        LIKE '%'+@f_search+'%'
              OR code                    LIKE '%'+@f_search+'%'
              OR application_description LIKE '%'+@f_search+'%'))
     ),
     numbered AS (
        SELECT *,
               ROW_NUMBER() OVER (ORDER BY created_at DESC) AS rn
        FROM base
     ),
     tally AS (
        SELECT COUNT(*) AS total_tally FROM base
     )
     SELECT (
        SELECT 
           t.total_tally                           AS totalTally,
           @page_number                            AS currentPage,
           @page_size                              AS pageSize,
           CEILING(t.total_tally * 1.0 / @page_size) AS totalPages,
           (SELECT 
                application_id,
                application_name,
                code,
                redirect_url,
                application_description,
                is_active,
                created_at,
                updated_at,
                is_public
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
        SELECT ERROR_NUMBER()   AS number,
               ERROR_MESSAGE()  AS message
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
     );
     THROW 51000, @err, 1;
  END CATCH
END;

GO

