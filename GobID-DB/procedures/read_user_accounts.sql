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
CREATE PROCEDURE [dbo].[read_user_accounts]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
  
  /*----------- 1. De-serialize JSON envelope -------------------------*/
  DECLARE
    @page_number  int = TRY_CAST(JSON_VALUE(@payload,'$.page_number') AS int),
    @page_size    int = TRY_CAST(JSON_VALUE(@payload,'$.page_size')   AS int),
    @filter_json  nvarchar(max) = JSON_QUERY(@payload,'$.filter');
  
  /* Apply sensible defaults */
  IF @page_number IS NULL OR @page_number < 1 SET @page_number = 1;
  IF @page_size   IS NULL OR @page_size   < 1 SET @page_size   = 50;
  
  /* Individual filter atoms (NULL = ignore) */
  DECLARE
    @f_user_id          uniqueidentifier = JSON_VALUE(@filter_json,'$.user_id'),
    @f_personnel_id     uniqueidentifier = JSON_VALUE(@filter_json,'$.personnel_id'),
    @f_email_like       nvarchar(320)    = JSON_VALUE(@filter_json,'$.email_like'),
    @f_is_active        bit              = JSON_VALUE(@filter_json,'$.is_active'),
    @f_organization_id  int              = JSON_VALUE(@filter_json,'$.organization_id');
  
  /*----------- 2. Query & paginate -----------------------------------*/
  BEGIN TRY
    ;WITH filtered AS (
       SELECT u.*, 
              uo.position, 
              uo.organization_id,
              o.organization_name  
       FROM   dbo.users AS u
       LEFT JOIN dbo.user_organizations AS uo
              ON u.user_id = uo.user_id
       LEFT JOIN dbo.organizations AS o 
              ON uo.organization_id = o.organization_id
       WHERE  (@f_user_id         IS NULL OR u.user_id         = @f_user_id)
         AND  (@f_personnel_id    IS NULL OR u.personnel_id    = @f_personnel_id)
         AND  (@f_is_active       IS NULL OR u.is_active       = @f_is_active)
         AND  (@f_email_like      IS NULL OR u.email LIKE '%' + @f_email_like + '%')
         AND  (@f_organization_id IS NULL OR uo.organization_id = @f_organization_id) 
         AND  u.deleted_at IS NULL
    ),
    numbered AS (
       SELECT *,
              ROW_NUMBER() OVER (ORDER BY created_at DESC) AS rn
       FROM filtered
    ),
    tally AS (
       SELECT COUNT(*) AS total_tally FROM filtered
    )
    SELECT (
      SELECT
        t.total_tally                              AS totalTally,
        @page_number                               AS currentPage,
        @page_size                                 AS pageSize,
        CEILING(t.total_tally * 1.0 / @page_size)  AS totalPages,
        (
          SELECT user_id, personnel_id, email,
                 must_change_password, admission_date, discharge_date,
                 created_at, updated_at, row_version, is_active,
                 position, organization_id, organization_name, person_position_id, phone_number
          FROM numbered
          WHERE rn BETWEEN (@page_number-1)*@page_size + 1
                     AND   @page_number*@page_size
          ORDER BY rn
          FOR JSON PATH
        ) AS data
      FROM tally t
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS json_result;
  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) =
      (SELECT
         error_number  = ERROR_NUMBER(),
         error_message = ERROR_MESSAGE()
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

