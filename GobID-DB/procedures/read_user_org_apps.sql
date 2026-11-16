-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[read_user_org_apps]
  @page_number int           = 1,
  @page_size   int           = 50,
  @filter      nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  /* ---- Extract dynamic filter values ------------------------------------- */
  DECLARE
    @f_user_id        uniqueidentifier = JSON_VALUE(@filter, '$.user_id'),
    @f_application_id uniqueidentifier = JSON_VALUE(@filter, '$.application_id'),
    @f_is_active      bit              = JSON_VALUE(@filter, '$.is_active');

  BEGIN TRY
    ;WITH filtered AS (
      SELECT
        oa.organization_application_id  AS OrganizationApplicationId,
        oa.organization_id              AS OrganizationId,
        o.organization_name             AS OrganizationName,
        oa.application_id               AS ApplicationId,
        a.application_name              AS ApplicationName
      FROM   dbo.user_applications              ua
      INNER JOIN dbo.organization_applications  oa ON ua.organization_application_id = oa.organization_application_id
      INNER JOIN dbo.organizations               o ON oa.organization_id             = o.organization_id
      INNER JOIN dbo.applications                a ON oa.application_id              = a.application_id
      WHERE  (@f_user_id        IS NULL OR ua.user_id        = @f_user_id)
        AND  (@f_application_id IS NULL OR oa.application_id = @f_application_id)
        AND  (@f_is_active      IS NULL OR ua.is_active      = @f_is_active)
        AND  (@f_is_active      IS NULL OR oa.is_active      = @f_is_active)
    ),
    numbered AS (
      SELECT *,
             ROW_NUMBER() OVER (ORDER BY OrganizationName ASC) AS rn,
             COUNT(*)     OVER ()                               AS total_tally
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
        ( SELECT OrganizationApplicationId,
                 OrganizationId,
                 OrganizationName,
                 ApplicationId,
                 ApplicationName
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

