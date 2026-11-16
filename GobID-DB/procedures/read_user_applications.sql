CREATE PROCEDURE [dbo].[read_user_applications]
  @page_number int           = 1,
  @page_size   int           = 50,
  @filter      nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  /* ---- Extract dynamic filter values ------------------------------------- */
  DECLARE
    @f_user_id                uniqueidentifier = JSON_VALUE(@filter, '$.user_id'),
    @f_org_app_id             int              = JSON_VALUE(@filter, '$.organization_application_id'),
    @f_is_active              bit              = JSON_VALUE(@filter, '$.is_active'),
    @f_user_application_id    uniqueidentifier = JSON_VALUE(@filter, '$.user_application_id'),
    @f_organization_id        int              = JSON_VALUE(@filter, '$.organization_id');

  BEGIN TRY
    ;WITH filtered AS (
      SELECT ua.*
      FROM   dbo.user_applications AS ua
      INNER JOIN dbo.organization_applications AS oa
             ON oa.organization_application_id = ua.organization_application_id
      WHERE  (@f_user_id             IS NULL OR ua.user_id                      = @f_user_id)
        AND  (@f_org_app_id          IS NULL OR ua.organization_application_id  = @f_org_app_id)
        AND  (@f_is_active           IS NULL OR ua.is_active                    = @f_is_active)
        AND  (@f_user_application_id IS NULL OR ua.user_application_id          = @f_user_application_id)
        AND  (@f_organization_id     IS NULL OR oa.organization_id              = @f_organization_id)
    ),
    numbered AS (
      SELECT *,
             ROW_NUMBER() OVER (ORDER BY created_at DESC) AS rn,
             COUNT(*)     OVER ()                         AS total_tally
      FROM   filtered
    )
    SELECT (
      SELECT
        ( SELECT total_tally,
                 @page_number AS current_page,
                 CEILING(total_tally * 1.0 / @page_size) AS total_pages
          FROM   numbered
          WHERE  rn = 1
          FOR JSON PATH, WITHOUT_ARRAY_WRAPPER ) AS meta,
        ( SELECT *
          FROM   numbered
          WHERE  rn BETWEEN (@page_number - 1) * @page_size + 1
                        AND  @page_number * @page_size
          FOR JSON PATH ) AS data
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

