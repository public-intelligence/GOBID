CREATE PROCEDURE [dbo].[read_organizations]
  @page_number int = 1,
  @page_size   int = 50,
  @filter      nvarchar(max) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

  BEGIN TRY
    IF @page_number < 1 SET @page_number = 1;
    IF @page_size   < 1 SET @page_size   = 50;

    ;WITH filter AS (
      SELECT
        JSON_VALUE(@filter,'$.orgid')                     AS organization_id,
        JSON_VALUE(@filter,'$.search')                     AS search,
        TRY_CAST(JSON_VALUE(@filter,'$.is_active') AS bit) AS is_active
    ),
    base AS (
      SELECT   o.*
      FROM     dbo.organizations AS o
      CROSS JOIN filter
      WHERE    (filter.search IS NULL
                OR o.organization_name LIKE '%' + filter.search + '%'
                OR o.acronym           LIKE '%' + filter.search + '%')
        AND    (filter.is_active IS NULL OR o.is_active = filter.is_active)
        AND    (filter.organization_id IS NULL OR o.organization_id = filter.organization_id)
        AND    o.deleted_at IS NULL
    ),
    numbered AS (
      SELECT *,
             ROW_NUMBER() OVER (ORDER BY o.created_at DESC) AS rn
      FROM   base o
    ),
    tally AS (
      SELECT COUNT(*) AS total_tally FROM base
    )
    SELECT (
      SELECT
        t.total_tally                              AS totalTally,
        @page_number                               AS currentPage,
        @page_size                                 AS pageSize,
        CEILING(t.total_tally * 1.0 / @page_size)  AS totalPages,
        (
          SELECT 
             organization_id,
             organization_name,
             acronym,
             organization_description,
             slogan,
             website,
             email,
             organization_address,
             phone,
             is_active,
             legal_entity_id,
             created_at,
             updated_at
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
      (SELECT ERROR_NUMBER() AS number, ERROR_MESSAGE() AS message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

