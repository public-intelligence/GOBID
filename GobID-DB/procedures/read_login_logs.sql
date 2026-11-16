/*--------------------------------------------------------------
Title:        read_login_logs
Goal:         Return paginated login-audit rows with optional
              filtering.
Parameters:
  @payload  nvarchar(max)  (required) – JSON body
    {
      "login_successful" : null,          -- true / false / null
      "organization_id"  : null,
      "from_date"        : "2025-06-01T00:00:00",
      "to_date"          : "2025-06-30T23:59:59",
      "page_number"      : 1,
      "page_size"        : 50
    }
Sample output:
  {
    "data":[ … ],
    "total_tally":143,
    "current_page":1,
    "total_pages":3
  }
----------------------------------------------------------------*/
CREATE   PROCEDURE [dbo].[read_login_logs]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  DECLARE
    @login_successful bit  = JSON_VALUE(@payload, '$.login_successful'),
    @organization_id int   = JSON_VALUE(@payload, '$.organization_id'),
    @from_date datetime2   = JSON_VALUE(@payload, '$.from_date'),
    @to_date   datetime2   = JSON_VALUE(@payload, '$.to_date'),
    @page_number int       = ISNULL(JSON_VALUE(@payload, '$.page_number'), 1),
    @page_size   int       = ISNULL(JSON_VALUE(@payload, '$.page_size'),   50);

  BEGIN TRY
    ;WITH filtered AS (
        SELECT *
        FROM   dbo.login_logs
        WHERE  (@login_successful IS NULL OR login_successful = @login_successful)
          AND  (@organization_id  IS NULL OR organization_id  = @organization_id)
          AND  (@from_date IS NULL OR created_at >= @from_date)
          AND  (@to_date   IS NULL OR created_at <= @to_date)
    ),
    numbered AS (
        SELECT *, ROW_NUMBER() OVER (ORDER BY login_log_id DESC) AS rn,
               COUNT(*) OVER() AS total_tally
        FROM   filtered
    )
    SELECT
      (SELECT total_tally,
              @page_number AS current_page,
              CEILING(total_tally * 1.0 / @page_size) AS total_pages
       FROM numbered
       GROUP BY total_tally
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS meta,
      (SELECT login_log_id, login_info, ip, header_values, login_successful,
              organization_id, organization_application_id, created_at
       FROM   numbered
       WHERE  rn BETWEEN (@page_number-1)*@page_size+1 AND @page_number*@page_size
       FOR JSON PATH)                               AS data
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) = (
      SELECT ERROR_NUMBER() AS error_number,
             ERROR_MESSAGE() AS error_message
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    THROW 51000, @err, 1;
  END CATCH;
END;

GO

