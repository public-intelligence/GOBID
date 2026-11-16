CREATE PROCEDURE [dbo].[read_user_effective_claims]
  @payload nvarchar(max),
  @result nvarchar(max) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  DECLARE
    @user_id     uniqueidentifier = JSON_VALUE(@payload, '$.user_id'),
    @org_app_id  int             = JSON_VALUE(@payload, '$.organization_application_id'),
    @page_number int             = ISNULL(JSON_VALUE(@payload, '$.page_number'), 1),
    @page_size   int             = ISNULL(JSON_VALUE(@payload, '$.page_size'), 1000);

  BEGIN TRY
    ;WITH active_roles AS (
        SELECT ur.role_id
        FROM   dbo.user_applications      ua
        JOIN   dbo.user_roles             ur ON ur.user_application_id = ua.user_application_id
        JOIN   dbo.roles                  r  ON r.role_id              = ur.role_id
        WHERE  ua.user_id                     = @user_id
          AND  ua.organization_application_id = @org_app_id
          AND  ua.is_active = 1
          AND  ur.is_active = 1
          AND  r.is_active  = 1
    ),
    effective_claims AS (
        SELECT DISTINCT c.code AS claim_code
        FROM   active_roles        ar
        JOIN   dbo.role_claims     rc ON rc.role_id = ar.role_id AND rc.is_active = 1
        JOIN   dbo.claims          c  ON c.claim_id = rc.claim_id AND c.is_active = 1
    ),
    numbered AS (
        SELECT claim_code,
               ROW_NUMBER() OVER (ORDER BY claim_code) AS rn,
               COUNT(*)   OVER () AS total_tally
        FROM   effective_claims
    )

   SELECT @result = (
  SELECT
    (SELECT total_tally,
            @page_number AS current_page,
            CEILING(total_tally * 1.0 / @page_size) AS total_pages
     FROM numbered
     GROUP BY total_tally
     FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS meta,
    (SELECT claim_code
     FROM   numbered
     WHERE  rn BETWEEN (@page_number-1)*@page_size+1
                 AND   @page_number*@page_size
     FOR JSON PATH) AS claims
  FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
);
  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) = (
      SELECT ERROR_NUMBER() AS error_number,
             ERROR_MESSAGE() AS error_message
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    THROW 51000, @err, 1;
  END CATCH;
END

GO

