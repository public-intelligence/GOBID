/*--------------------------------------------------------------
Title:        read_user_role
Goal:         Paged / filtered list of user-role mappings.
Parameters:
  @payload nvarchar(max) – JSON envelope

Sample input
{
  "page_number": 1,
  "page_size" : 50,
  "filter": {
     "user_application_id": "7F2BEBFA-65E4-4F12-B028-85B4F8B5C8A7",
     "is_active": 1
  }
}
Sample output
{
  "data":[ { … } ],
  "total_tally":17,
  "current_page":1,
  "total_pages":1
}
----------------------------------------------------------------*/
CREATE PROCEDURE [dbo].[read_user_roles]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;   -- gap-free

  BEGIN TRY
    /*------------------------------------------------------------
      1. Unpack JSON envelope
    ------------------------------------------------------------*/
    DECLARE
      @page_number         int              = ISNULL(TRY_CAST(JSON_VALUE(@payload,'$.page_number') AS int),1),
      @page_size           int              = ISNULL(TRY_CAST(JSON_VALUE(@payload,'$.page_size')   AS int),50),

      @user_role_id        int              = TRY_CAST(JSON_VALUE(@payload,'$.user_role_id')        AS int),
      @user_application_id uniqueidentifier = TRY_CAST(JSON_VALUE(@payload,'$.user_application_id') AS uniqueidentifier),
      @role_id             int              = TRY_CAST(JSON_VALUE(@payload,'$.role_id')             AS int),
      @is_active           bit              = TRY_CAST(JSON_VALUE(@payload,'$.is_active')           AS bit);

    /*------------------------------------------------------------
      2. Apply filter + paging
    ------------------------------------------------------------*/
    ;WITH filtered AS (
      SELECT ur.*, r.role_name
      FROM   dbo.user_roles ur
      INNER JOIN dbo.roles r ON ur.role_id = r.role_id
      WHERE  (@user_role_id        IS NULL OR ur.user_role_id        = @user_role_id)
        AND  (@user_application_id IS NULL OR ur.user_application_id = @user_application_id)
        AND  (@role_id             IS NULL OR ur.role_id             = @role_id)
        AND  (@is_active           IS NULL OR ur.is_active           = @is_active)
    ),
    numbered AS (
      SELECT ur.*,
             ROW_NUMBER() OVER (ORDER BY created_at DESC) AS rn,
             COUNT(*)    OVER ()                          AS total_tally
      FROM   filtered ur
    )
	SELECT (
    SELECT
      /* ---------- meta block ---------- */
      (SELECT TOP 1
              total_tally,
              @page_number                         AS current_page,
              CEILING(total_tally * 1.0 / @page_size) AS total_pages
       FROM   numbered
       ORDER  BY total_tally DESC
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)       AS meta,

      /* ---------- data block ---------- */
      (SELECT user_role_id,
              user_application_id,
              role_id,
              role_name,
              user_role_description,
              is_active,
              created_at,
              updated_at
       FROM   numbered
       WHERE  rn BETWEEN (@page_number-1)*@page_size + 1
                     AND @page_number*@page_size
       FOR JSON PATH)                              AS data
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS json_result;
  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) =
      (SELECT ERROR_NUMBER()  AS err_no,
              ERROR_MESSAGE() AS err_msg,
              ERROR_LINE()    AS err_line
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

